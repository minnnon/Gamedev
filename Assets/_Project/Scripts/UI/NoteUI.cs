using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Панель з текстом: записки (закрити E/Esc) і вступ при новій грі (будь-яка клавіша).
// Поки панель відкрита, гра в стані Reading — гравець стоїть.
public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance { get; private set; }
    public static int LastCloseFrame = -1;      // щоб та сама клавіша не закрила і одразу не відкрила знову

    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text hintText;

    private bool anyKeyClose;
    private int openFrame = -1;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        // Вступ показуємо лише після "Нової гри" з меню
        if (GameManager.ShowIntro)
        {
            GameManager.ShowIntro = false;
            Open("", GameManager.IntroText, "Натисни будь-яку клавішу", true);
        }
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static bool Show(string title, string body)
    {
        if (Instance == null || LastCloseFrame == Time.frameCount) return false;
        return Instance.Open(title, body, "E / Esc — закрити", false);
    }

    private bool Open(string title, string body, string hint, bool anyKey)
    {
        if (GameManager.Instance == null || !GameManager.Instance.StartReading()) return false;
        titleText.text = title;
        bodyText.text = body;
        hintText.text = hint;
        anyKeyClose = anyKey;
        openFrame = Time.frameCount;
        panel.SetActive(true);
        AudioManager.Play("ui_click", 0.7f, 0f);
        return true;
    }

    private void Update()
    {
        if (!panel.activeSelf || Time.frameCount == openFrame) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        bool close = anyKeyClose
            ? kb.anyKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            : kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame;
        if (!close) return;

        panel.SetActive(false);
        LastCloseFrame = Time.frameCount;
        GameManager.Instance.StopReading();
    }
}
