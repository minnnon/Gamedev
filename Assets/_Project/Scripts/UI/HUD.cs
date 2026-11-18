using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Інтерфейс під час гри: смужка HP, лічильники, підказка "E", повідомлення, вікно інвентаря (Tab).
// Посилання на елементи Canvas виставляє одноразовий скрипт-білдер.
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Здоров'я")]
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private Text hpText;
    [SerializeField] private Image damageFlash;

    [Header("Лічильники")]
    [SerializeField] private Text potionText;
    [SerializeField] private Text runeText;
    [SerializeField] private Text coinText;

    [Header("Підказки")]
    [SerializeField] private Text promptText;
    [SerializeField] private Text messageText;

    [Header("Інвентар (Tab)")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Image[] rowIcons;
    [SerializeField] private Text[] rowTexts;

    private Health health;
    private Inventory inventory;
    private Interactor interactor;
    private RuneGate gate;
    private int lastHp;
    private float flash;
    private float messageTimer;

    private void Awake()
    {
        Instance = this;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (messageText != null) messageText.text = "";
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            health = player.GetComponent<Health>();
            inventory = player.GetComponent<Inventory>();
            interactor = player.GetComponent<Interactor>();
        }
        gate = FindFirstObjectByType<RuneGate>();

        if (health != null) { health.Changed += OnHealthChanged; lastHp = health.Current; OnHealthChanged(health.Current, health.Max); }
        if (inventory != null) inventory.Changed += Refresh;
        if (gate != null) gate.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (health != null) health.Changed -= OnHealthChanged;
        if (inventory != null) inventory.Changed -= Refresh;
        if (gate != null) gate.Changed -= Refresh;
    }

    // Повідомлення по центру зверху (зберігання, руни тощо)
    public static void ShowMessage(string text, float seconds = 2.5f)
    {
        if (Instance == null || Instance.messageText == null) return;
        Instance.messageText.text = text;
        Instance.messageTimer = seconds;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Підказка "E" для найближчого інтерактивного об'єкта
        if (promptText != null)
        {
            var target = interactor != null && GameManager.IsPlaying ? interactor.Current : null;
            string prompt = target != null ? target.Prompt : "";
            promptText.text = string.IsNullOrEmpty(prompt) ? "" : "[E]  " + prompt;
        }

        // Tab відкриває й закриває інвентар
        var kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame && GameManager.IsPlaying && inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            AudioManager.Play("ui_click", 0.6f);
            Refresh();
        }
        if (!GameManager.IsPlaying && inventoryPanel != null && inventoryPanel.activeSelf) inventoryPanel.SetActive(false);

        // Червоний спалах при пораненні й зникнення повідомлення
        if (damageFlash != null)
        {
            flash = Mathf.MoveTowards(flash, 0f, dt * 1.4f);
            var c = damageFlash.color; c.a = flash; damageFlash.color = c;
        }
        if (messageText != null && messageTimer > 0f)
        {
            messageTimer -= dt;
            if (messageTimer <= 0f) messageText.text = "";
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        if (hpFill != null) hpFill.anchorMax = new Vector2(max > 0 ? (float)current / max : 0f, 1f);
        if (hpText != null) hpText.text = current + "/" + max;
        if (current < lastHp) flash = 0.35f;
        lastHp = current;
    }

    private void Refresh()
    {
        if (inventory == null) return;

        int bagRunes = inventory.CountOfType(ItemType.Rune);
        if (potionText != null) potionText.text = "Зілля: " + inventory.CountOfType(ItemType.Potion) + "   (Q)";
        if (coinText != null) coinText.text = "Монети: " + inventory.CountOfType(ItemType.Coin);
        if (runeText != null)
        {
            string s = gate != null ? $"Руни в брамі: {gate.Inserted}/{gate.Total}" : "Руни";
            if (bagRunes > 0) s += $"   (у сумці: {bagRunes})";
            runeText.text = s;
        }

        // Рядки вікна інвентаря
        if (rowTexts == null) return;
        for (int i = 0; i < rowTexts.Length; i++)
        {
            bool has = i < inventory.Slots.Count && inventory.Slots[i].item != null;
            rowTexts[i].gameObject.SetActive(has);
            if (rowIcons != null && i < rowIcons.Length) rowIcons[i].gameObject.SetActive(has);
            if (!has) continue;
            var slot = inventory.Slots[i];
            rowTexts[i].text = $"{slot.item.displayName}  ×{slot.count}";
            if (rowIcons != null && i < rowIcons.Length) rowIcons[i].color = slot.item.color;
        }
    }
}
