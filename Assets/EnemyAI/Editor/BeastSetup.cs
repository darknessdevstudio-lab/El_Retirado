using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using ElRetirado.Enemies;
using Builder = UnityEngine.AI.NavMeshBuilder;

// One-time setup works on the open scene, preserving unsaved work in a backup first.
[InitializeOnLoad]
public static class BeastSetup
{
    const string Folder = "Assets/EnemyAI";
    const string Report = Folder + "/setup-report.txt";
    static double nextCheck;
    static BeastSetup() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(Report) || SessionState.GetBool("BeastSetup.Attempted2", false)) return;
        if (EditorSceneManager.GetActiveScene().name != "casa01") return;
        SessionState.SetBool("BeastSetup.Attempted2", true);
        try { Configure(); }
        catch (Exception e) { File.WriteAllText(Report, "ERROR\n" + e); Debug.LogException(e); }
    }
    [MenuItem("El Retirado/Configurar enemigo y navegacion")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal del modo Play antes de configurar.");
        var scene = EditorSceneManager.GetActiveScene();
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        var visual = GameObject.Find("Run-bestia");
        if (!playerObject || !visual) throw new InvalidOperationException("Falta Player o Run-bestia en la escena abierta.");
        Directory.CreateDirectory(Folder + "/Backup");
        string backup = Folder + "/Backup/" + scene.name + "_antes_IA.unity";
        if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("No se pudo guardar la copia de seguridad.");
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Configurar IA de la bestia");
        int group = Undo.GetCurrentGroup();
        var enemy = visual.GetComponentInParent<BeastEnemy>();
        GameObject root;
        if (!enemy)
        {
            Bounds visualBounds = new Bounds(visual.transform.position, Vector3.zero);
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) visualBounds.Encapsulate(renderer.bounds);
            Vector3 feet = new Vector3(visualBounds.center.x, visualBounds.min.y, visualBounds.center.z);
            root = new GameObject("Bestia_Enemigo");
            Undo.RegisterCreatedObjectUndo(root, "Crear enemigo");
            root.transform.position = feet;
            root.transform.rotation = Quaternion.Euler(0, visual.transform.eulerAngles.y, 0);
            Undo.SetTransformParent(visual.transform, root.transform, "Conservar modelo de bestia");
            enemy = Undo.AddComponent<BeastEnemy>(root);
        }
        else root = enemy.gameObject;
        var agent = root.GetComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.radius = 0.28f; agent.height = 1.8f; agent.baseOffset = 0;
        agent.speed = 1.2f; agent.acceleration = 12; agent.angularSpeed = 300; agent.stoppingDistance = 0.25f;
        var settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = agent.radius; settings.agentHeight = agent.height;
        settings.agentClimb = 0.4f; settings.agentSlope = 50;
        settings.overrideVoxelSize = true; settings.voxelSize = 0.07f;
        settings.minRegionArea = 0.5f;
        agent.agentTypeID = settings.agentTypeID;
        var nav = UnityEngine.Object.FindFirstObjectByType<EnemyNavigation>();
        if (!nav)
        {
            var navObject = new GameObject("Navegacion_Bestia");
            Undo.RegisterCreatedObjectUndo(navObject, "Crear navegacion");
            nav = Undo.AddComponent<EnemyNavigation>(navObject);
        }
        var markups = new List<NavMeshBuildMarkup>
        {
            new NavMeshBuildMarkup { root = root.transform, overrideIgnore = true, ignoreFromBuild = true },
            new NavMeshBuildMarkup { root = playerObject.transform, overrideIgnore = true, ignoreFromBuild = true }
        };
        var sources = new List<NavMeshBuildSource>();
        Builder.CollectSources((Transform)null, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
        sources.RemoveAll(s => s.component && (s.component.gameObject.scene != scene || (s.component is Collider c && c.isTrigger)));
        Bounds bounds = new Bounds(root.transform.position, Vector3.one * 4);
        foreach (var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if (!collider.isTrigger && collider.gameObject.scene == scene && !collider.transform.IsChildOf(root.transform) && !collider.transform.IsChildOf(playerObject.transform)) bounds.Encapsulate(collider.bounds);
        bounds.Expand(4);
        var data = Builder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (!data) throw new InvalidOperationException("No se pudo construir la navegacion de la casa.");
        string navPath = Folder + "/CasaNavigation.asset";
        var old = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if (old) { EditorUtility.CopySerialized(data, old); UnityEngine.Object.DestroyImmediate(data); data = old; EditorUtility.SetDirty(old); }
        else AssetDatabase.CreateAsset(data, navPath);
        nav.enabled = false; nav.data = data; nav.enabled = true;
        // Add temporarily in edit mode to place the enemy and generate reachable patrol points.
        var navInstance = NavMesh.AddNavMeshData(data);
        try
        {
            if (!NavMesh.SamplePosition(root.transform.position, out var spawn, 5f, NavMesh.AllAreas)) throw new InvalidOperationException("No hay suelo navegable cerca de la bestia.");
            Undo.RecordObject(root.transform, "Colocar bestia sobre el suelo");
            root.transform.position = spawn.position;
            var patrol = GameObject.Find("Ruta_Patrulla_Bestia");
            if (!patrol) { patrol = new GameObject("Ruta_Patrulla_Bestia"); Undo.RegisterCreatedObjectUndo(patrol, "Crear ruta de patrulla"); }
            var positions = new List<Vector3>();
            var triangles = NavMesh.CalculateTriangulation();
            var candidates = triangles.vertices.Where(p => Vector3.Distance(p, spawn.position) > 2 && Vector3.Distance(p, spawn.position) < 25).OrderBy(p => p.y).ToList();
            var path = new NavMeshPath();
            // Prefer a spread of points on connected floors; no random seed or unreachable destinations.
            for (int count = 0; count < 6 && candidates.Count > 0; count++)
            {
                Vector3 best = spawn.position; float score = -1;
                foreach (var candidate in candidates)
                {
                    float separation = positions.Count == 0 ? Vector3.Distance(candidate, spawn.position) : positions.Min(p => Vector3.Distance(candidate, p));
                    if (separation <= score || separation < 2.5f) continue;
                    if (!NavMesh.SamplePosition(candidate, out var sample, 0.5f, NavMesh.AllAreas)) continue;
                    if (!NavMesh.CalculatePath(spawn.position, sample.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                    best = sample.position; score = separation;
                }
                if (score < 0) break;
                positions.Add(best);
                candidates.RemoveAll(p => Vector3.Distance(best, p) < 2.5f);
            }
            if (positions.Count < 2) throw new InvalidOperationException("La navegacion no ofrece al menos dos puntos conectados. Revisa los colliders de la casa.");
            if (patrol.transform.childCount == 0)
                foreach (var position in positions)
                {
                    var point = new GameObject("Punto_" + (patrol.transform.childCount + 1));
                    Undo.RegisterCreatedObjectUndo(point, "Crear punto");
                    point.transform.SetParent(patrol.transform); point.transform.position = position;
                }
            enemy.patrolPoints = patrol.GetComponentsInChildren<Transform>().Where(t => t != patrol.transform).ToArray();
            enemy.player = playerObject.transform;
            enemy.animator = visual.GetComponent<Animator>();
            if (!enemy.animator) enemy.animator = Undo.AddComponent<Animator>(visual);
            enemy.animator.applyRootMotion = false;
            enemy.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            enemy.animator.runtimeAnimatorController = CreateAnimator();
            enemy.eyeHeight = 1.5f;
            if (!playerObject.GetComponent<PlayerHealth>()) Undo.AddComponent<PlayerHealth>(playerObject);
            if (!playerObject.GetComponent<PlayerNoiseEmitter>()) Undo.AddComponent<PlayerNoiseEmitter>(playerObject);
            agent.enabled = true;
            EditorUtility.SetDirty(enemy); EditorUtility.SetDirty(agent); EditorUtility.SetDirty(enemy.animator); EditorUtility.SetDirty(nav);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("No se pudo guardar la escena configurada.");
            Undo.CollapseUndoOperations(group);
            string report = "OK\nScene: " + scene.path + "\nBackup: " + backup + "\nSources: " + sources.Count + "\nNavMesh vertices: " + triangles.vertices.Length + "\nEnemy spawn: " + spawn.position + "\nPlayer: " + playerObject.transform.position + "\nPatrol points: " + enemy.patrolPoints.Length;
            foreach (var point in enemy.patrolPoints) report += "\n" + point.name + ": " + point.position;
            File.WriteAllText(Report, report);
            Debug.Log("[BestiaSetup] " + report);
        }
        finally { if (navInstance.valid) navInstance.Remove(); }
    }
    static AnimatorController CreateAnimator()
    {
        string path = Folder + "/Bestia.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing) return existing;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Reposo"); idle.motion = CopyClip("Idle-bestia", "Reposo");
        var walk = machine.AddState("Caminar"); walk.motion = CopyClip("Walking-bestia", "Caminar");
        var run = machine.AddState("Correr"); run.motion = CopyClip("Run-bestia", "Correr");
        machine.defaultState = idle;
        Transition(idle, walk, AnimatorConditionMode.Greater, 0.1f);
        Transition(walk, idle, AnimatorConditionMode.Less, 0.1f);
        Transition(walk, run, AnimatorConditionMode.Greater, 2f);
        Transition(run, walk, AnimatorConditionMode.Less, 2f);
        return controller;
    }
    static AnimationClip CopyClip(string model, string name)
    {
        var source = AssetDatabase.LoadAllAssetsAtPath("Assets/Animations/" + model + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (!source) throw new InvalidOperationException("Falta la animacion " + model);
        var clip = UnityEngine.Object.Instantiate(source); clip.name = name;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true; settings.keepOriginalPositionXZ = true; settings.keepOriginalPositionY = true;
        settings.loopBlendPositionXZ = true; settings.loopBlendPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, Folder + "/" + name + ".anim");
        return clip;
    }
    static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false; transition.duration = 0.15f;
        transition.AddCondition(mode, threshold, "Speed");
    }
}
