using UnityEngine;

namespace ElRetirado.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerNoiseEmitter : MonoBehaviour
    {
        public float walkRadius = 5f, runRadius = 12f, crouchRadius = 1.5f;
        CharacterController controller;
        Vector3 previous;
        float distance;
        void OnEnable() { controller = GetComponent<CharacterController>(); previous = transform.position; distance = 0; }
        void Update()
        {
            Vector3 delta = transform.position - previous;
            previous = transform.position;
            if (Time.timeScale <= 0 || !controller.isGrounded || GetComponent<PlayerHealth>()?.IsDead == true)
            { distance = 0; return; }
            delta.y = 0;
            if (delta.magnitude > 2f) { distance = 0; return; }
            if (delta.sqrMagnitude < 0.000001f) return;
            bool crouched = controller.height < 1.4f;
            bool running = !crouched && delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f) > 2f;
            distance += delta.magnitude;
            if (distance < (crouched ? 1f : running ? 1.6f : 1.2f)) return;
            distance = 0;
            NoiseSystem.Emit(transform.position, crouched ? crouchRadius : running ? runRadius : walkRadius);
        }
    }
}
