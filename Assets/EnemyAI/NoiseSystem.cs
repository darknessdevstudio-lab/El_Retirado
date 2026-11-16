using System;
using UnityEngine;

namespace ElRetirado.Enemies
{
    public static class NoiseSystem
    {
        public static event Action<Vector3, float> Emitted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Emitted = null; }
        // Call from doors, thrown objects or other gameplay sounds. Radius is in metres.
        public static void Emit(Vector3 position, float radius)
        {
            if (Time.timeScale > 0 && radius > 0) Emitted?.Invoke(position, radius);
        }
    }
}
