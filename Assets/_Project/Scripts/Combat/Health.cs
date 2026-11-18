using System;
using UnityEngine;

// Здоров'я об'єкта: шкода, коротка невразливість після удару, події для інших скриптів.
public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invulnerabilityTime = 1f;

    private float invulnerableUntil;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action<int, int> Changed;   // (поточне, максимальне)
    public event Action Died;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || Time.time < invulnerableUntil) return;

        Current = Mathf.Max(0, Current - amount);
        invulnerableUntil = Time.time + invulnerabilityTime;
        Debug.Log($"[Health] {name}: -{amount} HP, залишилось {Current}/{maxHealth}");
        Changed?.Invoke(Current, maxHealth);
        if (Current == 0) Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    // Для завантаження збереження: виставляє HP напряму
    public void SetCurrent(int value)
    {
        Current = Mathf.Clamp(value, 1, maxHealth);
        Changed?.Invoke(Current, maxHealth);
    }

    public void Restore()
    {
        Current = maxHealth;
        invulnerableUntil = Time.time + invulnerabilityTime;
        Changed?.Invoke(Current, maxHealth);
    }
}
