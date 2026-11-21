using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Вступні кадри при "Новій грі": картинка + підпис, далі будь-яка клавіша. Після останнього — гра.
public class IntroUI : MonoBehaviour
{
    [SerializeField] private GameObject root;     // панель з візуалом (дочірня; сам компонент завжди активний)
    [SerializeField] private CanvasGroup slide;
    [SerializeField] private Image picture;
    [SerializeField] private Text caption;
    [SerializeField] private Text hint;
    [SerializeField] private Sprite[] sprites;
    [SerializeField, TextArea(2, 4)] private string[] captions;

    private const float FadeTime = 0.5f;
    private int index;
    private bool busy;

    private void Awake() { if (root != null) root.SetActive(false); }

    private void Start()
    {
        if (!GameManager.ShowIntro) return;
        GameManager.ShowIntro = false;
        if (GameManager.Instance == null || !GameManager.Instance.StartReading()) return;
        root.SetActive(true);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        for (index = 0; index < sprites.Length; index++)
        {
            picture.sprite = sprites[index];
            caption.text = captions[index];
            hint.text = index < sprites.Length - 1 ? "Натисни будь-яку клавішу" : "Натисни будь-яку клавішу, щоб почати";
            yield return Fade(0f, 1f);
            yield return WaitForKey();
            yield return Fade(1f, 0f);
        }
        root.SetActive(false);
        GameManager.Instance.StopReading();
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            slide.alpha = Mathf.Lerp(from, to, t / FadeTime);
            yield return null;
        }
        slide.alpha = to;
    }

    private static IEnumerator WaitForKey()
    {
        yield return null;       // щоб натискання, яким відкрили "Нову гру", не пролистало кадр
        while (true)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame)) yield break;
            yield return null;
        }
    }
}
