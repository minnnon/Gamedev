using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Удар мечем: анімація, а через коротку затримку — шкода всім ворогам у зоні перед лицарем.
public class PlayerCombat : MonoBehaviour
{
    [Header("Удар")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.55f;
    [SerializeField] private float hitDelay = 0.25f;     // момент, коли меч "доходить" до цілі
    [SerializeField] private float hitRadius = 1.3f;
    [SerializeField] private float hitForward = 1.2f;
    [SerializeField] private float staminaCost = 15f;   // скільки витривалості коштує один удар
    [SerializeField] private LayerMask enemyMask;

    [Header("Посилання")]
    [SerializeField] private Animator animator;
    [SerializeField] private InputActionAsset inputActions;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly Collider[] hits = new Collider[16];

    public event Action Swung;     // замах (для моделі в руках від першої особи)
    public event Action Landed;    // удар влучив у ворога

    private InputAction attackAction;
    private Health health;
    private PlayerBlock block;
    private float nextAttackTime;

    private void Awake()
    {
        health = GetComponent<Health>();
        block = GetComponent<PlayerBlock>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        attackAction = inputActions.FindActionMap("Player", true).FindAction("Attack", true);
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;
        if (!GameManager.IsPlaying) return;
        if (block != null && block.IsBlocking) return;     // зі щитом не б'ємо
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (!attackAction.WasPressedThisFrame() || Time.time < nextAttackTime) return;
        if (block != null && !block.TrySpend(staminaCost)) return;   // лицар виснажений

        nextAttackTime = Time.time + cooldown;
        animator.SetTrigger(AttackHash);
        Swung?.Invoke();
        StartCoroutine(HitWindow());
    }

    private IEnumerator HitWindow()
    {
        yield return new WaitForSeconds(hitDelay);

        // Сфера перед лицарем: усі колайдери ворогів у ній отримують шкоду
        int count = Physics.OverlapSphereNonAlloc(HitCenter(), hitRadius, hits, enemyMask, QueryTriggerInteraction.Ignore);
        bool landed = false;
        for (int i = 0; i < count; i++)
        {
            var target = hits[i].GetComponentInParent<Health>();
            if (target == null || target == health || target.IsDead) continue;
            target.TakeDamage(damage);
            landed = true;
        }
        if (landed) Landed?.Invoke();
    }

    private Vector3 HitCenter()
    {
        return transform.position + transform.forward * hitForward + Vector3.up;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitCenter(), hitRadius);
    }
}
