using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using ElRetirado.Enemies;

// Runs only on explicit request; all test changes are discarded when Play mode exits.
[InitializeOnLoad]
public static class BeastValidation
{
    const string Request = "Assets/EnemyAI/validate.request";
    const string Report = "Assets/EnemyAI/validation-report.txt";
    static BeastEnemy enemy;
    static PlayerHealth health;
    static NavMeshAgent agent;
    static int stage;
    static double until, timeout;
    static Vector3 start;
    static Dictionary<Transform, Quaternion> bones;
    static readonly List<string> results = new List<string>();
    static BeastValidation() { EditorApplication.update += Tick; }
    [MenuItem("El Retirado/Comprobar comportamiento en Play")]
    public static void RequestValidation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("BeastValidation.Active", false);
        stage = 0; results.Clear(); until = 0;
        File.WriteAllText(Request, "run");
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!SessionState.GetBool("BeastValidation.Active", false))
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            SessionState.SetBool("BeastValidation.Active", true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (EditorApplication.timeSinceStartup < until) return;
        try
        {
            if (stage > 0 && EditorApplication.timeSinceStartup > timeout) throw new Exception("Tiempo de comprobacion agotado.");
            switch (stage)
            {
                case 0:
                    enemy = UnityEngine.Object.FindFirstObjectByType<BeastEnemy>();
                    health = enemy.player.GetComponent<PlayerHealth>(); agent = enemy.GetComponent<NavMeshAgent>();
                    // Keep the real movement script, collider and HUD enabled for this regression.
                    Input.ResetInputAxes();
                    foreach (var p in UnityEngine.Object.FindObjectsByType<PauseMenu>(FindObjectsSortMode.None)) p.enabled = false;
                    Time.timeScale = 1; AudioListener.pause = false;
                    MovePlayer(new Vector3(1000, 1000, 1000));
                    start = enemy.transform.position;
                    bones = enemy.animator.GetComponentsInChildren<Transform>().ToDictionary(t => t, t => t.localRotation);
                    timeout = EditorApplication.timeSinceStartup + 60;
                    Next(3); break;
                case 1:
                    Check(agent.isOnNavMesh, "Enemigo sobre la malla de navegacion");
                    Check(enemy.State == BeastEnemy.BehaviourState.Patrol && Vector3.Distance(start, enemy.transform.position) > 0.2f, "Patrulla: desplazamiento real por la casa");
                    Check(bones.Any(p => p.Key && Quaternion.Angle(p.Key.localRotation, p.Value) > 1), "Animacion: el esqueleto cambia de postura");
                    NoiseSystem.Emit(enemy.transform.position + Vector3.right * 100, 0.1f);
                    Check(enemy.State == BeastEnemy.BehaviourState.Patrol, "Ignora ruidos fuera de alcance");
                    NoiseSystem.Emit(enemy.patrolPoints[4].position, 100f);
                    Check(enemy.State == BeastEnemy.BehaviourState.Investigate, "Investiga la posicion de un ruido audible");
                    PlacePlayer(4f);
                    Check(enemy.CanSeePlayer(), "Vision: detecta al jugador a la vista");
                    start = enemy.transform.position;
                    Next(0.5); break;
                case 2:
                    Check((enemy.State == BeastEnemy.BehaviourState.Chase || enemy.State == BeastEnemy.BehaviourState.Attack) && Vector3.Distance(start, enemy.transform.position) > 0.1f, "Persigue al jugador con movimiento real (" + enemy.State + ", metros=" + Vector3.Distance(start, enemy.transform.position).ToString("F2") + ")");
                    Vector3 origin = enemy.transform.position + Vector3.up * enemy.eyeHeight;
                    Vector3 target = enemy.player.position + Vector3.up * 1.35f;
                    var wall = new GameObject("ValidationWall");
                    wall.transform.position = (origin + target) / 2;
                    wall.transform.rotation = Quaternion.LookRotation(target - origin);
                    wall.AddComponent<BoxCollider>().size = new Vector3(3, 4, 0.2f);
                    Physics.SyncTransforms();
                    Check(!enemy.CanSeePlayer(), "Las paredes bloquean la vision");
                    UnityEngine.Object.DestroyImmediate(wall);
                    enemy.memorySeconds = 0.1f;
                    MovePlayer(new Vector3(1000, 1000, 1000));
                    Next(0.6); break;
                case 3:
                    Check(enemy.State == BeastEnemy.BehaviourState.Investigate, "Al perder al jugador busca su ultima posicion (" + enemy.State + ")");
                    enemy.memorySeconds = 6;
                    PlacePlayer(0.75f);
                    Next(0.9); break;
                case 4:
                    Check(health.CurrentHealth == health.maximumHealth - enemy.damage, "Primer golpe con collider y configuracion real: vida=" + health.CurrentHealth);
                    Check(enemy.player.GetComponent<FPSController>().enabled, "La prueba mantiene el controlador real del jugador");
                    Next(4.5); break;
                case 5:
                    Check(health.IsDead && health.CurrentHealth == 0, "Ataques repetidos reducen la vida y causan muerte");
                    Check(Time.timeScale == 0 && Cursor.lockState == CursorLockMode.None, "La muerte detiene el juego y permite reintentar");
                    var panel = health.transform.Find("HUD_Vida_y_Muerte/Pantalla_Muerte");
                    Check(panel && panel.gameObject.activeInHierarchy, "La pantalla de muerte esta visible en el Canvas superior");
                    panel.Find("Reintentar").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Next(2); break;
                case 6:
                    var restarted = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
                    Check(restarted && !restarted.IsDead && restarted.CurrentHealth == restarted.maximumHealth, "Reintentar recarga la escena y restablece la vida");
                    Finish(); break;
            }
        }
        catch (Exception e) { results.Add("ERROR: " + e); Finish(); }
    }
    static void PlacePlayer(float distance)
    {
        for (int i = 0; i < 16; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
            if (!NavMesh.SamplePosition(enemy.transform.position + offset, out var hit, 0.4f, NavMesh.AllAreas)) continue;
            if (Mathf.Abs(hit.position.y - enemy.transform.position.y) > 0.3f) continue;
            var cc = enemy.player.GetComponent<CharacterController>();
            MovePlayer(hit.position + Vector3.up * (cc.height * 0.5f + cc.skinWidth));
            Vector3 direction = hit.position - enemy.transform.position; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) continue;
            enemy.transform.rotation = Quaternion.LookRotation(direction);
            Physics.SyncTransforms();
            if (enemy.CanSeePlayer()) return;
        }
        throw new Exception("No se encontro un punto visible para la prueba.");
    }
    static void MovePlayer(Vector3 position)
    {
        var cc = enemy.player.GetComponent<CharacterController>();
        cc.enabled = false; enemy.player.position = position; cc.enabled = true; Physics.SyncTransforms();
    }
    static void Check(bool passed, string name) { results.Add((passed ? "PASS: " : "FAIL: ") + name); }
    static void Next(double delay) { stage++; until = EditorApplication.timeSinceStartup + delay; }
    static void Finish()
    {
        File.WriteAllLines(Report, results);
        Debug.Log("[BestiaValidation]\n" + string.Join("\n", results));
        SessionState.SetBool("BeastValidation.Active", false);
        Time.timeScale = 1; AudioListener.pause = false;
        EditorApplication.isPlaying = false;
    }
}
