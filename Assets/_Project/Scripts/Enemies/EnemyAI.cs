using System;
using UnityEngine;
using UnityEngine.AI;

// ШІ скелета — скінченний автомат на NavMeshAgent:
// Patrol (обхід точок) -> Chase (погоня) -> Attack (удар) -> Return (повернення на маршрут).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Return }

    [Header("Патруль")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPoint = 1.5f;

    [Header("Виявлення гравця")]
    [SerializeField] private float viewRadius = 9f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 110f;
    [SerializeField] private float closeRadius = 2.5f;      // на такій відстані помічає і зі спини
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private LayerMask obstacleMask = 1;     // що закриває огляд (стіни)
    [SerializeField] private float loseTargetTime = 3f;

    [Header("Погоня й атака")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float attackRange = 1.9f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Посилання")]
    [SerializeField] private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private NavMeshAgent agent;
    private Transform player;
    private Health playerHealth;
    private int waypointIndex;
    private float waitTimer;
    private float lostTimer;
    private float attackTimer;

    public State Current { get; private set; }
    public event Action Attacked;   // у лабі 15 до цієї події підключиться завдання шкоди

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }
        SetState(State.Patrol);
        GoToWaypoint();
    }

    private void Update()
    {
        bool sees = CanSeePlayer();
        float distance = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        switch (Current)
        {
            case State.Patrol:
                if (sees) { SetState(State.Chase); break; }
                PatrolTick();
                break;

            case State.Chase:
                // Поки бачимо гравця — оновлюємо таймер; не бачимо довго — здаємося
                lostTimer = sees ? loseTargetTime : lostTimer - Time.deltaTime;
                if (lostTimer <= 0f) { SetState(State.Return); break; }
                agent.SetDestination(player.position);
                if (sees && distance <= attackRange) SetState(State.Attack);
                break;

            case State.Attack:
                FacePlayer();
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    attackTimer = attackCooldown;
                    if (animator != null) animator.SetTrigger(AttackHash);
                    Attacked?.Invoke();
                }
                if (!sees || distance > attackRange * 1.25f) SetState(State.Chase);
                break;

            case State.Return:
                if (sees) { SetState(State.Chase); break; }
                if (ReachedDestination()) SetState(State.Patrol);
                break;
        }

        // Анімація ходьби й бігу береться з реальної швидкості агента
        if (animator != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    // Викликається, коли скелета вдарили: він одразу кидається в погоню
    public void Alert()
    {
        if (Current == State.Patrol || Current == State.Return) SetState(State.Chase);
    }

    private void SetState(State next)
    {
        Current = next;
        switch (next)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0.2f;
                waitTimer = waitAtPoint;
                break;
            case State.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.stoppingDistance = attackRange * 0.8f;
                lostTimer = loseTargetTime;
                break;
            case State.Attack:
                agent.isStopped = true;
                attackTimer = 0.3f;
                break;
            case State.Return:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0.2f;
                GoToWaypoint();
                break;
        }
        Debug.Log($"[Enemy] {name}: стан {next}");
    }

    private void PatrolTick()
    {
        if (waypoints == null || waypoints.Length == 0 || !ReachedDestination()) return;

        // Дійшли до точки — трохи чекаємо і йдемо до наступної
        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f) return;
        waitTimer = waitAtPoint;
        waypointIndex = (waypointIndex + 1) % waypoints.Length;
        GoToWaypoint();
    }

    private void GoToWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        agent.SetDestination(waypoints[waypointIndex].position);
    }

    private bool ReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    // Бачить гравця, якщо той у радіусі, у секторі огляду і його не закриває стіна
    private bool CanSeePlayer()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead)) return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position + Vector3.up * 1.2f - eye;
        float distance = toPlayer.magnitude;
        if (distance > viewRadius) return false;

        if (distance > closeRadius)
        {
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;
        }
        return !Physics.Raycast(eye, toPlayer.normalized, distance, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    // Показує в редакторі радіус і сектор огляду вибраного ворога
    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eye, eye + left * viewRadius);
        Gizmos.DrawLine(eye, eye + right * viewRadius);
    }
}