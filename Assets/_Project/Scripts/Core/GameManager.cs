using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, Paused, GameOver, Win, Reading }

// Керує станами гри (меню, гра, пауза, програш, перемога), паузою через Time.timeScale,
// курсором і завантаженням сцен. Один екземпляр на всю програму (Singleton + DontDestroyOnLoad).
public class GameManager : MonoBehaviour
{
    public const string MenuScene = "MainMenu";
    public const string GameScene = "Crypt";

    public static GameManager Instance { get; private set; }

    // Скрипти гравця перевіряють це перед обробкою вводу (на паузі й у меню — false)
    public static bool IsPlaying => Instance == null || Instance.State == GameState.Playing;

    // Меню виставляє true для "Продовжити": після завантаження сцени застосується збереження
    public static bool LoadRequested;

    // "Нова гра" виставляє true: у сцені гри спершу показується вступ
    public static bool ShowIntro;

    // Після переходу між рівнями нова сцена починається з чорного екрана
    public static bool FadeInPending;

    public GameState State { get; private set; } = GameState.Menu;
    public float PlayTime { get; set; }
    public int Deaths { get; set; }     // скільки разів гравець помирав за проходження (для екрана перемоги)
    public event Action<GameState> StateChanged;

    private PlayerRespawn respawn;
    private Health playerHealth;

    // Створюємо менеджер автоматично до завантаження першої сцени — у меню і в грі він один і той самий
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void Update()
    {
        if (State == GameState.Playing) PlayTime += Time.deltaTime;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && NoteUI.LastCloseFrame != Time.frameCount)
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        playerHealth = null;
        respawn = null;
        StopAllCoroutines();

        if (scene.name == MenuScene) { SetState(GameState.Menu); return; }

        SaveSystem.ClearRuntime();
        PlayTime = 0f;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null) respawn.AutoRespawn = false;     // відродження тепер за кнопкою на екрані смерті
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            if (player.GetComponent<AudioHooks>() == null) player.AddComponent<AudioHooks>();
        }
        SetState(GameState.Playing);

        if (LoadRequested)
        {
            LoadRequested = false;
            StartCoroutine(ApplyLoadNextFrame());
        }
        else StartCoroutine(ApplyStashNextFrame());
    }

    // Чекаємо кадр, щоб усі Start() у сцені встигли відпрацювати, і лише тоді накладаємо збереження
    private IEnumerator ApplyLoadNextFrame()
    {
        yield return null;
        SaveSystem.Load();
    }

    private IEnumerator ApplyStashNextFrame()
    {
        yield return null;
        SaveSystem.ApplyStash();
    }

    private void OnPlayerDied()
    {
        Deaths++;
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(2f);     // дати програтися анімації смерті
        if (State == GameState.Playing) SetState(GameState.GameOver);
    }

    public void SetState(GameState next)
    {
        State = next;
        switch (next)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                SetCursor(false);
                break;
            case GameState.Reading:     // читаємо записку: час стоїть, курсор лишається захопленим
                Time.timeScale = 0f;
                SetCursor(false);
                break;
            case GameState.Paused:
            case GameState.Win:
                Time.timeScale = 0f;
                SetCursor(true);
                break;
            default:     // Menu, GameOver
                Time.timeScale = 1f;
                SetCursor(true);
                break;
        }
        if (next == GameState.Win) AudioManager.Play("win");
        if (next == GameState.GameOver) AudioManager.Play("lose");
        Debug.Log($"[Game] Стан: {next}");
        StateChanged?.Invoke(next);
    }

    private static void SetCursor(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    // --- Дії, які викликають кнопки й інші скрипти

    public void Pause() { if (State == GameState.Playing) SetState(GameState.Paused); }
    public void Resume() { if (State == GameState.Paused) SetState(GameState.Playing); }

    public bool StartReading()
    {
        if (State != GameState.Playing) return false;
        SetState(GameState.Reading);
        return true;
    }

    public void StopReading() { if (State == GameState.Reading) SetState(GameState.Playing); }

    public void Win() { if (State == GameState.Playing) SetState(GameState.Win); }

    // "Спробувати знову": відродження біля останнього вогнища, світ лишається як є
    public void Retry()
    {
        if (State != GameState.GameOver) return;
        if (respawn != null) respawn.RespawnNow();
        SetState(GameState.Playing);
    }

    public void NewGame()
    {
        LoadRequested = false;
        ShowIntro = true;
        Deaths = 0;
        SceneManager.LoadScene(GameScene);
    }

    public void ContinueGame()
    {
        if (!SaveSystem.HasSave) return;
        LoadRequested = true;
        SceneManager.LoadScene(SaveSystem.ReadScene());     // Crypt або Depths — де збереглись
    }

    // Перехід на наступний рівень: гравець іде з тим, що в нього є
    public void NextLevel(string sceneName)
    {
        if (State != GameState.Playing) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[Game] Сцени {sceneName} немає в Build Settings — перемога.");
            Win();
            return;
        }
        if (!StartReading()) return;     // гравець завмирає на час затемнення
        NoteUI.FadeOut(0.8f, () =>
        {
            SaveSystem.StashPlayer();
            LoadRequested = false;
            FadeInPending = true;
            SceneManager.LoadScene(sceneName);
        });
    }

    public void ToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
