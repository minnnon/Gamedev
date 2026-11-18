using UnityEngine;

// Синхронізує анімації з діями гравця: передає в Animator швидкість,
// стан "на землі", поранення і смерть. Удар запускає PlayerCombat.
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Посилання")]
    [SerializeField] private Animator animator;

    [Header("Плавність")]
    [SerializeField] private float speedDampTime = 0.1f;

    // Хеші параметрів працюють швидше, ніж рядки
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private PlayerController controller;
    private Health health;
    private int lastHealth;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.Changed += OnHealthChanged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Changed -= OnHealthChanged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        if (health != null) lastHealth = health.Current;
    }

    private void Update()
    {
        // Швидкість керує Blend Tree (стояння - ходьба - біг), Grounded — стрибком
        animator.SetFloat(SpeedHash, controller.CurrentSpeed, speedDampTime, Time.deltaTime);
        animator.SetBool(GroundedHash, controller.IsGrounded);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current < lastHealth && current > 0) animator.SetTrigger(HitHash);
        if (current > 0) animator.SetBool(DeadHash, false);
        lastHealth = current;
    }

    private void OnDied()
    {
        animator.SetBool(DeadHash, true);
    }
}
