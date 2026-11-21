using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Інтерфейс: HUD (енергія, сегменти HP, повідомлення) і панелі паузи, перемоги, поразки.
// Сам нічого не вирішує — лише показує те, що каже GameManager.
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text batteryText;
    public TMP_Text healthText;
    public Image[] hpSegments;                 // Сегменти смужки HP (якщо порожньо — HP пишеться текстом)
    public Color hpFullColor = new Color(0.95f, 0.27f, 0.33f);
    public Color hpEmptyColor = new Color(0.18f, 0.2f, 0.26f);

    [Header("Повідомлення")]
    public TMP_Text messageText;
    public GameObject messageBox;              // Фон повідомлення (необов'язково)

    [Header("Панелі")]
    public GameObject pausePanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject introPanel;              // Екран вступу з сюжетом

    private Coroutine messageRoutine;

    void Awake()
    {
        // На старті всі панелі та повідомлення сховані
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }


        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }


        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }


        SetMessageVisible(false);
    }

    public void UpdateBatteries(int current, int total)
    {
        if (batteryText != null)
        {
            batteryText.text = $"ЕНЕРГІЯ  {current}/{total}";
        }

    }

    public void UpdateHealth(int current, int max)
    {
        if (hpSegments != null && hpSegments.Length > 0)
        {
            // Зафарбовуємо стільки сегментів, скільки є HP
            for (int i = 0; i < hpSegments.Length; i++)
            {

                if (hpSegments[i] != null)
                {
                    hpSegments[i].color = i < current ? hpFullColor : hpEmptyColor;
                }
            }

            if (healthText != null)
            {
                healthText.text = "HP";
            }

        }
        else if (healthText != null)
        {
            healthText.text = $"HP: {current}/{max}";
        }
    }

    public void ShowPause(bool show)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(show);
        }

    }

    public void ShowIntro(bool show)
    {
        if (introPanel != null)
        {
            introPanel.SetActive(show);
        }
    }

    public void ShowWin()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

    }

    public void ShowLose()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }

    }

    // Повідомлення на 2 секунди вгорі екрана
    public void ShowMessage(string text, float seconds = 2f)
    {
        if (messageText == null)
        {
            return;
        }


        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }


        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    private IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.text = text;
        SetMessageVisible(true);
        yield return new WaitForSecondsRealtime(seconds);   // Realtime — працює навіть на паузі
        SetMessageVisible(false);
    }

    private void SetMessageVisible(bool visible)
    {
        if (messageBox != null)
        {
            messageBox.SetActive(visible);
        }

        else if (messageText != null)
        {
            messageText.gameObject.SetActive(visible);
        }

    }
}
