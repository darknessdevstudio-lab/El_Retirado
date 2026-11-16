using UnityEngine;
using UnityEngine.AI;

namespace ElRetirado.Enemies
{
    [DefaultExecutionOrder(-1000)]
    public class EnemyNavigation : MonoBehaviour
    {
        public NavMeshData data;
        NavMeshDataInstance instance;
        void OnEnable() { if (data != null) instance = NavMesh.AddNavMeshData(data); }
        void OnDisable() { if (instance.valid) instance.Remove(); }
    }
}
