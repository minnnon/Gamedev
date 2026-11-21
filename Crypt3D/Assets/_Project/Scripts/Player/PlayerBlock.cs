using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Блок щитом: поки затиснута права кнопка миші, удари спереду не завдають шкоди.
// Щит витрачає витривалість: на утримання і на кожен заблокований удар. Закінчилась — щит "ламається".
[RequireComponent(typeof(PlayerController))]
public class PlayerBlock : MonoBehaviour
{
    [Header("Блок")]
    [SerializeField, Range(30f, 360f)] private float blockAngle = 140f;   // сектор перед лицарем
    [SerializeField] private float speedMultiplier = 0.5f;

    [Header("Витривалість щита")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float holdDrain = 6f;           // за секунду утримання
    [SerializeField] private float costPerDamage = 22f;      // за кожну одиницю шкоди заблокованого удару
    [SerializeField] private float regenPerSecond = 18f;
    [SerializeField] private float regenDelay = 0.7f;        // пауза після блоку перед відновленням
    [SerializeField] private float brokenTime = 1.8f;        // скільки щит не працює після зламу
    [SerializeField] private float minToRaise = 15f;         // з меншим запасом щит не піднімеш

    [Header("Посилання")]
    [SerializeField] private Animator animator;

    private static readonly int BlockHash = Animator.StringToHash("Block");

    private PlayerController controller;
    private Health health;
    private float stamina;
    private float regenAt;
    private float brokenUntil;

    public bool IsBlocking { get; private set; }
    public bool IsBroken => Time.time < brokenUntil;
    public float Stamina01 => stamina / maxStamina;

    public event Action BlockedHit;    // удар прийнято щитом
    public event Action GuardBroken;   // щит зламано

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        stamina = maxStamina;
    }

    private void Update()
    {
        bool dead = health != null && health.IsDead;
        bool pressed = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool canRaise = IsBlocking || stamina >= minToRaise;

        bool want = !dead
                    && !IsBroken
                    && canRaise
                    && !controller.InputLocked
                    && GameManager.IsPlaying
                    && Cursor.lockState == CursorLockMode.Locked
                    && pressed;

        if (want != IsBlocking)
        {
            IsBlocking = want;
            if (animator != null) animator.SetBool(BlockHash, want);
            if (!want) regenAt = Time.time + regenDelay;
        }
        controller.SpeedMultiplier = IsBlocking ? speedMultiplier : 1f;

        if (IsBlocking)
        {
            stamina -= holdDrain * Time.deltaTime;
            if (stamina <= 0f) Break();
        }
        else if (Time.time >= regenAt && !IsBroken)
        {
            stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * Time.deltaTime);
        }
        else if (IsBroken && Time.time >= regenAt)
        {
            stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * 0.5f * Time.deltaTime);
        }
    }

    // Витривалість витрачається й на удари мечем: спамом ЛКМ вона закінчується
    public bool TrySpend(float amount)
    {
        if (stamina < amount) return false;
        stamina -= amount;
        regenAt = Time.time + regenDelay + 0.3f;
        return true;
    }

    // Чи прикриває щит від удару ворога в цій точці. Якщо так — знімає витривалість.
    // false — удар проходить (щит опущено, ворог збоку/ззаду або витривалості не вистачило).
    public bool TryBlock(Vector3 attackerPosition, int damage)
    {
        if (!IsBlocking) return false;

        Vector3 toAttacker = attackerPosition - transform.position;
        toAttacker.y = 0f;
        if (Vector3.Angle(transform.forward, toAttacker) > blockAngle * 0.5f) return false;

        float cost = costPerDamage * damage;
        if (stamina < cost)
        {
            Break();      // удар занадто сильний — щит вибито, шкода проходить
            return false;
        }
        stamina -= cost;
        BlockedHit?.Invoke();
        return true;
    }

    private void Break()
    {
        stamina = 0f;
        IsBlocking = false;
        brokenUntil = Time.time + brokenTime;
        regenAt = brokenUntil;
        if (animator != null) animator.SetBool(BlockHash, false);
        controller.SpeedMultiplier = 1f;
        Debug.Log("[Block] Щит зламано!");
        GuardBroken?.Invoke();
    }

    // Тимчасова смужка витривалості (у лабі 16 замінимо нормальним HUD)
    private void OnGUI()
    {
        if (health != null && health.IsDead) return;
        if (stamina >= maxStamina - 0.5f && !IsBlocking) return;

        const float w = 220f, h = 10f;
        float x = (Screen.width - w) * 0.5f, y = Screen.height - 50f;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), Texture2D.whiteTexture);
        GUI.color = IsBroken ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.95f, 0.75f, 0.2f);
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(Stamina01), h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
