using System;
using UnityEngine;

// Скриня: по клавіші E піднімає кришку і віддає здобич в інвентар гравця.
public class Chest : MonoBehaviour, IInteractable
{
    [Serializable]
    public class Loot
    {
        public ItemData item;
        public int amount = 1;
    }

    [Header("Кришка")]
    [SerializeField] private Transform lid;
    [SerializeField] private Vector3 openAxis = Vector3.right;
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float openSpeed = 200f;

    [Header("Здобич")]
    [SerializeField] private Loot[] loot;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    public bool IsOpen { get; private set; }
    public string Prompt => IsOpen ? "" : "Відкрити скриню";

    private void Awake()
    {
        if (lid == null) return;
        closedRotation = lid.localRotation;
        openRotation = Quaternion.AngleAxis(openAngle, openAxis) * closedRotation;
    }

    public void Interact(GameObject interactor)
    {
        if (IsOpen) return;
        IsOpen = true;
        AudioManager.Play("chest");
        Debug.Log($"[Chest] {name}: скриню відкрито.");

        var inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || loot == null) return;
        foreach (var l in loot)
            if (l != null && l.item != null) inventory.Add(l.item, l.amount);
    }

    // Для завантаження збереження: скриня одразу відкрита, без здобичі (її вже забрано)
    public void Restore()
    {
        IsOpen = true;
        if (lid != null) lid.localRotation = openRotation;
    }

    private void Update()
    {
        if (lid == null) return;
        Quaternion target = IsOpen ? openRotation : closedRotation;
        lid.localRotation = Quaternion.RotateTowards(lid.localRotation, target, openSpeed * Time.deltaTime);
    }
}
