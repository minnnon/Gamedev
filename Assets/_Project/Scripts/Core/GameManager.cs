using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, Paused, GameOver, Win }

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

    public GameState State { get; private set; } = GameState.Menu;
    public float PlayTime { get; set; }
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
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
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
    }

    // Чекаємо кадр, щоб усі Start() у сцені встигли відпрацювати, і лише тоді накладаємо збереження
    private IEnumerator ApplyLoadNextFrame()
    {
        yield return null;
        SaveSystem.Load();
    }

    private void OnPlayerDied()
    {
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
        SceneManager.LoadScene(GameScene);
    }

    public void ContinueGame()
    {
        if (!SaveSystem.HasSave) return;
        LoadRequested = true;
        SceneManager.LoadScene(GameScene);
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
