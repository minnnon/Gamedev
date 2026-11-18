using UnityEngine;
using UnityEngine.UI;

// Показує екрани пауза / смерть / перемога залежно від стану GameManager.
// Методи нижче підключені до кнопок і повзунків у Canvas.
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Text winStats;
    [SerializeField] private Button loadButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
        if (GameManager.Instance == null) return;
        GameManager.Instance.StateChanged += OnState;
        OnState(GameManager.Instance.State);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnState;
    }

    private void OnState(GameState state)
    {
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
        if (winPanel != null) winPanel.SetActive(state == GameState.Win);

        if (state == GameState.GameOver && loadButton != null) loadButton.interactable = SaveSystem.HasSave;
        if (state == GameState.Win && winStats != null)
        {
            float t = GameManager.Instance.PlayTime;
            var player = GameObject.FindGameObjectWithTag("Player");
            var inv = player != null ? player.GetComponent<Inventory>() : null;
            int coins = inv != null ? inv.CountOfType(ItemType.Coin) : 0;
            winStats.text = $"Час: {(int)(t / 60f):00}:{(int)(t % 60f):00}\nМонети: {coins}";
        }
    }

    // --- Кнопки
    public void Resume() { Click(); GameManager.Instance.Resume(); }
    public void Retry() { Click(); GameManager.Instance.Retry(); }
    public void LoadLast() { Click(); GameManager.Instance.ContinueGame(); }
    public void ToMenu() { Click(); GameManager.Instance.ToMenu(); }
    public void QuitGame() { Click(); GameManager.Instance.Quit(); }

    // --- Повзунки гучності
    public void OnMusicSlider(float v) { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); }
    public void OnSfxSlider(float v)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetSfxVolume(v);
        AudioManager.Play("ui_click", 0.8f, 0f);     // чути, наскільки гучно
    }

    private static void Click() { AudioManager.Play("ui_click", 0.7f, 0f); }
}
