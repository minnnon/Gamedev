using UnityEngine;
using UnityEngine.SceneManagement;

// Менеджер гри: стани гри, батареї, гермодвері, пауза, перемога/поразка, перезапуск.
public class GameManager : MonoBehaviour
{
    public enum GameState { Intro, Playing, Paused, Won, Lost }

    public static GameManager instance;       // Singleton: доступ з будь-якого скрипта
    private static bool skipIntro;            // Після "Заново" вступ не показуємо вдруге

    [Header("Налаштування рівня")]
    public int totalBatteries = 4;
    public int currentBatteries = 0;

    [Header("Посилання")]
    public DoorBarrier door;
    public UIManager ui;

    public GameState State { get; private set; } = GameState.Playing;
    public bool AllBatteriesCollected => currentBatteries >= totalBatteries;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    void Start()
    {
        Time.timeScale = 1f;
        if (ui != null)
        {
            ui.UpdateBatteries(currentBatteries, totalBatteries);
        }

        // Вступ: гра стоїть, доки гравець не натисне "Почати"
        if (ui != null && ui.introPanel != null && !skipIntro)
        {
            State = GameState.Intro;
            Time.timeScale = 0f;
            ui.ShowIntro(true);
        }
        skipIntro = false;

    }

    void Update()
    {
        // Esc: пауза під час гри, продовження під час паузи
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing)
            {
                Pause();
            }
            else if (State == GameState.Paused)
            {
                Resume();
            }

        }
    }

    public void AddBattery()
    {
        currentBatteries++;
        if (ui != null)
        {
            ui.UpdateBatteries(currentBatteries, totalBatteries);
        }


        if (AllBatteriesCollected)
        {
            ShowMessage("Усі енергоелементи зібрано! Гермодвері відкриваються.");
            if (door != null)
            {
                door.Open();
            }

        }
    }

    public void UpdateHealth(int current, int max)
    {
        if (ui != null)
        {
            ui.UpdateHealth(current, max);
        }

    }

    // Кнопка "Почати" на екрані вступу
    public void StartGame()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
        if (ui != null)
        {
            ui.ShowIntro(false);
        }
    }

    public void ShowMessage(string text, float seconds = 2f)
    {
        if (ui != null)
        {
            ui.ShowMessage(text, seconds);
        }

    }

    public void Pause()
    {
        State = GameState.Paused;
        Time.timeScale = 0f;
        if (ui != null)
        {
            ui.ShowPause(true);
        }

    }

    public void Resume()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
        if (ui != null)
        {
            ui.ShowPause(false);
        }

    }

    public void Win()
    {
        if (State != GameState.Playing)
        {
            return;
        }


        State = GameState.Won;
        Time.timeScale = 0f;
        if (ui != null)
        {
            ui.ShowWin();
        }

    }

    public void GameOver()
    {
        if (State != GameState.Playing)
        {
            return;
        }


        State = GameState.Lost;
        Time.timeScale = 0f;
        if (ui != null)
        {
            ui.ShowLose();
        }

    }

    // Кнопка "Заново": повертаємо нормальний час і перезавантажуємо сцену
    public void Restart()
    {
        Time.timeScale = 1f;
        skipIntro = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Вихід з гри");
    }

}

