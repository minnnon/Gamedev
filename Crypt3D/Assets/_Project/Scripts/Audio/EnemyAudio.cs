using UnityEngine;
using UnityEngine.AI;

// Звуки скелета: кроки кісток, бурмотіння, крик при виявленні гравця, замах.
// Звук гучніший, коли гравець ближче (просторовий звук). EnemyCombat додає цей компонент сам.
[RequireComponent(typeof(EnemyAI))]
public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private float hearDistance = 24f;

    private EnemyAI ai;
    private NavMeshAgent agent;
    private Health health;
    private Transform player;
    private EnemyAI.State lastState;
    private float idleTimer;
    private float stepTimer;
    private float pitch = 1f;

    private void Start()
    {
        ai = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        lastState = ai.Current;
        idleTimer = Random.Range(2f, 6f);
        if (name.Contains("Guardian")) pitch = 0.72f;     // великий вартовий — нижчий голос
        ai.Attacked += OnAttacked;
    }

    private void OnDestroy()
    {
        if (ai != null) ai.Attacked -= OnAttacked;
    }

    private void OnAttacked()
    {
        AudioManager.PlayAt("skeleton_attack", transform.position + Vector3.up, 0.9f, 0.05f, pitch);
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;
        if (!GameManager.IsPlaying) return;

        // Побачив гравця — крик
        if (ai.Current != lastState)
        {
            if (ai.Current == EnemyAI.State.Chase && (lastState == EnemyAI.State.Patrol || lastState == EnemyAI.State.Return))
                AudioManager.PlayAt("skeleton_alert", transform.position + Vector3.up * 1.4f, 1f, 0.04f, pitch);
            lastState = ai.Current;
        }

        // Кроки кісток
        float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
        if (speed > 0.3f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                AudioManager.PlayAt("skeleton_step_" + Random.Range(1, 3), transform.position, 0.8f, 0.08f, pitch);
                stepTimer = speed > 3f ? 0.27f : 0.5f;
            }
        }
        else stepTimer = 0.1f;

        // Бурмотіння, поки гравець неподалік
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            idleTimer = Random.Range(4f, 9f);
            if (player != null && Vector3.Distance(player.position, transform.position) < hearDistance)
                AudioManager.PlayAt("skeleton_idle_" + Random.Range(1, 3), transform.position + Vector3.up * 1.4f, 0.8f, 0.05f, pitch);
        }
    }
}
