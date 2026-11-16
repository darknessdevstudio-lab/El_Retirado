using UnityEngine;
using UnityEngine.AI;

namespace ElRetirado.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BeastEnemy : MonoBehaviour
    {
        public enum BehaviourState { Patrol, Investigate, Chase, Attack }
        public Transform player;
        public Animator animator;
        public Transform[] patrolPoints;
        [Header("Movimiento")]
        public float patrolSpeed = 1.2f, chaseSpeed = 3.6f, patrolWait = 2f;
        [Header("Vista y oido")]
        public float viewDistance = 14f;
        [Range(1, 360)] public float viewAngle = 110f;
        public float eyeHeight = 1.5f, memorySeconds = 6f, searchSeconds = 4f, hearingMultiplier = 1f;
        public LayerMask visionMask = ~0;
        [Header("Ataque")]
        public float attackRange = 1.25f, damage = 25f, attackInterval = 1.2f, attackWindup = 0.35f;
        public BehaviourState State { get; private set; }
        public Vector3 PlayerAimPoint
        {
            get
            {
                if (!player) return transform.position;
                var cc = player.GetComponent<CharacterController>();
                return cc ? player.TransformPoint(cc.center) + Vector3.up * cc.height * Mathf.Abs(player.lossyScale.y) * 0.15f : player.position + Vector3.up * 0.9f;
            }
        }
        NavMeshAgent agent;
        PlayerHealth health;
        Vector3 lastKnown;
        float lastSeen = float.NegativeInfinity, nextPath, waitUntil, nextAttack, hitAt = -1;
        int patrolIndex;
        bool waiting;
        NavMeshPath path;
        static readonly int Speed = Animator.StringToHash("Speed");
        void OnEnable() { NoiseSystem.Emitted += Hear; }
        void OnDisable() { NoiseSystem.Emitted -= Hear; }
        void Start()
        {
            path = new NavMeshPath();
            agent = GetComponent<NavMeshAgent>();
            if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform; }
            if (player) health = player.GetComponent<PlayerHealth>();
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 3f, agent.areaMask)) agent.Warp(hit.position);
            if (!agent.isOnNavMesh) { Debug.LogError("[Bestia] No hay suelo navegable bajo el enemigo.", this); enabled = false; return; }
            State = BehaviourState.Patrol;
            Debug.Log("[Bestia] IA preparada: patrulla, vision, persecucion, ataque y ruidos.", this);
        }
        void Update()
        {
            if (Time.timeScale <= 0 || !agent.isOnNavMesh) return;
            if (!player || (health && health.IsDead)) { Stop(); Animate(); return; }
            bool visible = CanSeePlayer();
            bool alerted = visible || State == BehaviourState.Chase || State == BehaviourState.Attack;
            bool canAttack = alerted && InAttackRange(State == BehaviourState.Attack ? 0.2f : 0f) && ClearLine(PlayerAimPoint);
            if (visible)
            {
                lastKnown = player.position; lastSeen = Time.time; waiting = false;
                State = InAttackRange() ? BehaviourState.Attack : BehaviourState.Chase;
            }
            // Once alerted, a nearby player can be attacked even when stepping outside the view cone.
            if (canAttack) State = BehaviourState.Attack;
            else if (State == BehaviourState.Chase || State == BehaviourState.Attack)
            {
                State = Time.time - lastSeen <= memorySeconds ? BehaviourState.Chase : BehaviourState.Investigate;
                if (State == BehaviourState.Investigate) waiting = false;
            }
            if (State != BehaviourState.Attack) hitAt = -1;
            switch (State)
            {
                case BehaviourState.Attack: Attack(); break;
                case BehaviourState.Chase: Move(lastKnown, chaseSpeed); break;
                case BehaviourState.Investigate:
                    if (!waiting)
                    {
                        if (!Move(lastKnown, patrolSpeed) || Arrived()) { Stop(); waiting = true; waitUntil = Time.time + searchSeconds; }
                    }
                    else if (Time.time >= waitUntil) { waiting = false; State = BehaviourState.Patrol; }
                    break;
                default: Patrol(); break;
            }
            Animate();
        }
        void Animate() { if (animator) animator.SetFloat(Speed, agent.velocity.magnitude, 0.15f, Time.deltaTime); }
        public bool CanSeePlayer()
        {
            if (!player) return false;
            Vector3 target = PlayerAimPoint;
            Vector3 direction = target - (transform.position + Vector3.up * eyeHeight);
            if (direction.magnitude > viewDistance) return false;
            Vector3 flat = direction; flat.y = 0;
            if (Vector3.Angle(transform.forward, flat) > viewAngle / 2) return false;
            return ClearLine(target);
        }
        bool ClearLine(Vector3 target)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 direction = target - origin;
            foreach (var hit in Physics.RaycastAll(origin, direction.normalized, direction.magnitude, visionMask, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && hit.transform != transform && hit.transform != player && !hit.transform.IsChildOf(player)) return false;
            return true;
        }
        bool InAttackRange(float tolerance = 0f)
        {
            var cc = player.GetComponent<CharacterController>();
            Vector3 center = cc ? player.TransformPoint(cc.center) : player.position;
            Vector3 difference = center - transform.position;
            float radius = cc ? cc.radius * Mathf.Max(Mathf.Abs(player.lossyScale.x), Mathf.Abs(player.lossyScale.z)) : 0;
            return Mathf.Abs(difference.y) < 1.8f && new Vector2(difference.x, difference.z).magnitude <= attackRange + radius + tolerance;
        }
        void Attack()
        {
            Stop();
            Vector3 direction = player.position - transform.position; direction.y = 0;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 360 * Time.deltaTime);
            if (hitAt < 0 && Time.time >= nextAttack) { hitAt = Time.time + attackWindup; nextAttack = Time.time + attackInterval; }
            if (hitAt >= 0 && Time.time >= hitAt)
            {
                hitAt = -1;
                if (InAttackRange(0.2f) && ClearLine(PlayerAimPoint)) health?.TakeDamage(damage);
            }
        }
        void Hear(Vector3 position, float radius)
        {
            if (!agent || !agent.isOnNavMesh || (health && health.IsDead) || State == BehaviourState.Chase || State == BehaviourState.Attack) return;
            if (Vector3.Distance(transform.position, position) > radius * hearingMultiplier) return;
            lastKnown = position; waiting = false; nextPath = 0; State = BehaviourState.Investigate;
        }
        void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) { Stop(); return; }
            if (waiting)
            {
                if (Time.time < waitUntil) return;
                waiting = false; patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
            var point = patrolPoints[patrolIndex];
            if (!point || !Move(point.position, patrolSpeed) || Arrived())
            { Stop(); waiting = true; waitUntil = Time.time + patrolWait; }
        }
        bool Move(Vector3 target, float speed)
        {
            agent.speed = speed;
            if (Time.time < nextPath) { agent.isStopped = false; return true; }
            nextPath = Time.time + 0.25f;
            if (!NavMesh.SamplePosition(target, out var sample, 1.5f, agent.areaMask) || !agent.CalculatePath(sample.position, path) || path.status != NavMeshPathStatus.PathComplete)
            { Stop(); return false; }
            agent.isStopped = false;
            agent.SetPath(path);
            return true;
        }
        bool Arrived() { return !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.15f; }
        void Stop() { if (agent && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } nextPath = 0; }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Gizmos.DrawRay(eye, Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance);
            Gizmos.DrawRay(eye, Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewDistance);
            if (patrolPoints != null) foreach (var point in patrolPoints) if (point) Gizmos.DrawWireSphere(point.position, 0.2f);
        }
    }
}
