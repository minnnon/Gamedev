using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Інвентар гравця: список комірок "предмет + кількість".
// Вміє додавати, видаляти й використовувати предмети.
public class Inventory : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public ItemData item;
        public int count;
    }

    [SerializeField] private int capacity = 8;
    [SerializeField] private List<Slot> slots = new List<Slot>();

    private Health health;

    public IReadOnlyList<Slot> Slots => slots;
    public event Action Changed;   // на цю подію підпишеться HUD (лаба 16)

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    // Гарячі клавіші: Q — випити зілля, G — викинути зілля (Tab відкриває вікно інвентаря в HUD)
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !GameManager.IsPlaying) return;
        if (kb.qKey.wasPressedThisFrame) UseFirst(ItemType.Potion);
        if (kb.gKey.wasPressedThisFrame) DropFirst(ItemType.Potion);
    }

    public bool Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // Однакові предмети складаються в одну комірку
        Slot slot = slots.Find(s => s.item == item);
        if (slot == null)
        {
            if (slots.Count >= capacity) { Debug.Log("[Inventory] Немає місця."); return false; }
            slot = new Slot { item = item, count = 0 };
            slots.Add(slot);
        }
        slot.count += amount;
        Debug.Log($"[Inventory] + {item.displayName} x{amount} (усього {slot.count})");
        Changed?.Invoke();
        return true;
    }

    public bool Remove(ItemData item, int amount = 1)
    {
        Slot slot = slots.Find(s => s.item == item);
        if (slot == null || slot.count < amount) return false;

        slot.count -= amount;
        if (slot.count == 0) slots.Remove(slot);
        Debug.Log($"[Inventory] - {item.displayName} x{amount}");
        Changed?.Invoke();
        return true;
    }

    public int Count(ItemData item)
    {
        Slot slot = slots.Find(s => s.item == item);
        return slot != null ? slot.count : 0;
    }

    public int CountOfType(ItemType type)
    {
        int total = 0;
        foreach (var s in slots)
            if (s.item != null && s.item.type == type) total += s.count;
        return total;
    }

    public ItemData FindFirst(ItemType type)
    {
        Slot slot = slots.Find(s => s.item != null && s.item.type == type);
        return slot != null ? slot.item : null;
    }

    // Використання залежить від типу предмета
    public bool Use(ItemData item)
    {
        if (item == null || Count(item) == 0) return false;

        switch (item.type)
        {
            case ItemType.Potion:
                if (health == null || health.IsDead) return false;
                if (health.Current >= health.Max) { Debug.Log("[Inventory] Здоров'я вже повне."); return false; }
                health.Heal(item.healAmount);
                Remove(item, 1);
                Debug.Log($"[Inventory] Випито {item.displayName}: HP {health.Current}/{health.Max}");
                return true;
            default:
                Debug.Log($"[Inventory] {item.displayName} так не використовується.");
                return false;
        }
    }

    public void UseFirst(ItemType type)
    {
        ItemData item = FindFirst(type);
        if (item == null) { Debug.Log("[Inventory] Такого предмета немає."); return; }
        Use(item);
    }

    // Викинути: прибираємо з інвентаря і кладемо предмет на землю перед гравцем
    public void DropFirst(ItemType type)
    {
        ItemData item = FindFirst(type);
        if (item == null || !Remove(item, 1)) return;
        if (item.pickupPrefab != null)
            Instantiate(item.pickupPrefab, transform.position + transform.forward * 1.8f, Quaternion.identity);
    }

    public void Clear()
    {
        slots.Clear();
        Changed?.Invoke();
    }

    private void PrintToLog()
    {
        if (slots.Count == 0) { Debug.Log("[Inventory] Порожньо."); return; }
        var parts = new List<string>();
        foreach (var s in slots) parts.Add($"{s.item.displayName} x{s.count}");
        Debug.Log("[Inventory] " + string.Join(", ", parts));
    }
}
