using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Бойова частина скелета: завдає шкоди гравцю (якщо той не блокує), реагує на удари
// заціпенінням і відкиданням, помирає і лишає здобич.
[RequireComponent(typeof(EnemyAI), typeof(Health))]
public class EnemyCombat : MonoBehaviour
{
    [Header("Удар")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitDelay = 0.6f;      // час замаху: гравець встигає відскочити чи підняти щит
    [SerializeField] private float hitRange = 2.4f;

    [Header("Реакція на удар")]
    [SerializeField] private float staggerTime = 0.6f;   // скільки скелет "очумляється" після удару
    [SerializeField] private float knockbackDistance = 0.7f;
    [SerializeField] private float poiseTime = 1.5f;     // після заціпеніння скелет деякий час стійкий до нового

    [Header("Смерть")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private float corpseTime = 4f;

    [Header("Посилання")]
    [SerializeField] private Animator animator;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private EnemyAI ai;
    private Health health;
    private NavMeshAgent agent;
    private Transform player;
    private Health playerHealth;
    private PlayerBlock playerBlock;
    private int lastHealth;
    private float noStaggerUntil;
    private bool committed;     // скелет уже замахнувся: удар не збити, можна лише ухилитися чи заблокувати

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        if (GetComponent<EnemyAudio>() == null) gameObject.AddComponent<EnemyAudio>();
    }

    private void Start()
    {
        lastHealth = health.Current;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        player = p.transform;
        playerHealth = p.GetComponent<Health>();
        playerBlock = p.GetComponent<PlayerBlock>();
    }

    private void OnEnable()
    {
        ai.Attacked += OnAttacked;
        health.Changed += OnHealthChanged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        ai.Attacked -= OnAttacked;
        health.Changed -= OnHealthChanged;
        health.Died -= OnDied;
    }

    private void OnAttacked()
    {
        StartCoroutine(HitWindow());
    }

    // Шкода проходить не в момент замаху, а трохи пізніше — якщо гравець не встиг відійти
    private IEnumerator HitWindow()
    {
        committed = true;
        yield return new WaitForSeconds(hitDelay);
        committed = false;
        if (health.IsDead || player == null || playerHealth == null) yield break;
        if (Vector3.Distance(transform.position, player.position) > hitRange) yield break;

        if (playerBlock != null && playerBlock.TryBlock(transform.position, damage))
        {
            Debug.Log($"[Enemy] {name}: удар заблоковано щитом.");
            yield break;
        }
        playerHealth.TakeDamage(damage);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current < lastHealth && current > 0)
        {
            AudioManager.PlayAt("skeleton_hit", transform.position + Vector3.up);
            ai.Alert();               // вдарили зі спини — скелет розвертається й нападає

            // Заціпеніння не спрацьовує знову, поки діє "стійкість": клацанням ЛКМ скелета не залочиш
            if (Time.time >= noStaggerUntil && !committed)
            {
                noStaggerUntil = Time.time + staggerTime + poiseTime;
                if (animator != null) animator.SetTrigger(HitHash);
                StopAllCoroutines();      // удар, що замахувався, скасовується
                StartCoroutine(Stagger());
            }
        }
        lastHealth = current;
    }

    // Коротке заціпеніння: скелет не ходить і не б'є, трохи відлітає від гравця
    private IEnumerator Stagger()
    {
        ai.enabled = false;
        bool wasStopped = agent.enabled && agent.isOnNavMesh && agent.isStopped;
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;

        Vector3 away = player != null ? transform.position - player.position : -transform.forward;
        away.y = 0f;
        away = away.sqrMagnitude > 0.001f ? away.normalized : -transform.forward;

        float t = 0f;
        while (t < staggerTime)
        {
            t += Time.deltaTime;
            if (t < 0.2f && agent.enabled && agent.isOnNavMesh)
                agent.Move(away * (knockbackDistance / 0.2f) * Time.deltaTime);
            yield return null;
        }

        if (health.IsDead) yield break;
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = wasStopped;
        ai.enabled = true;
    }

    private void OnDied()
    {
        StopAllCoroutines();
        Debug.Log($"[Enemy] {name}: знищено.");
        SaveSystem.MarkDead(transform);                       // щоб збереження знало, що цей ворог убитий
        AudioManager.PlayAt("skeleton_die", transform.position + Vector3.up);
        if (animator != null) animator.SetBool(DeadHash, true);

        // Мертвий скелет більше не думає, не ходить і не заважає пройти
        ai.enabled = false;
        if (agent != null) agent.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        if (dropPrefab != null)
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject, corpseTime);
    }
}
