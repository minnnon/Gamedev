using System;
using System.Collections;
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
    [SerializeField] private Image fadeImage;     // чорний екран на весь Canvas для переходів між сценами

    private bool anyKeyClose;
    private Action onClose;
    private int openFrame = -1;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        // Прийшли з іншого рівня: починаємо з чорного екрана і плавно світлішаємо
        if (GameManager.FadeInPending && fadeImage != null)
        {
            GameManager.FadeInPending = false;
            SetFade(1f);
            StartCoroutine(Fade(1f, 0f, 0.8f, null));
        }
    }

    // Затемнення екрана, потім done (наприклад, завантаження сцени). Якщо екрана нема — done одразу.
    public static void FadeOut(float seconds, Action done)
    {
        if (Instance == null || Instance.fadeImage == null) { done?.Invoke(); return; }
        Instance.StartCoroutine(Instance.Fade(0f, 1f, seconds, done));
    }

    private IEnumerator Fade(float from, float to, float seconds, Action done)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetFade(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetFade(to);
        done?.Invoke();
    }

    private void SetFade(float a)
    {
        var c = fadeImage.color; c.a = a; fadeImage.color = c;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // onClose — що зробити, коли гравець закрив панель (наприклад, показати перемогу)
    public static bool Show(string title, string body, Action onClose = null)
    {
        if (Instance == null || LastCloseFrame == Time.frameCount) return false;
        return Instance.Open(title, body, "E / Esc — закрити", false, onClose);
    }

    private bool Open(string title, string body, string hint, bool anyKey, Action closeAction = null)
    {
        if (GameManager.Instance == null || !GameManager.Instance.StartReading()) return false;
        onClose = closeAction;
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
        var callback = onClose;
        onClose = null;
        callback?.Invoke();
    }
}
