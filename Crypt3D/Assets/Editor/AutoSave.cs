#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Скрипт редактора (не потрапляє у гру): автозбереження сцени
// кожні 2 хвилини і перед кожним натисканням Play — захист від вильотів Unity.
[InitializeOnLoad]
public static class AutoSave
{
    private const double IntervalSeconds = 120;
    private static double nextSaveTime;

    static AutoSave()
    {
        nextSaveTime = EditorApplication.timeSinceStartup + IntervalSeconds;
        EditorApplication.update += OnUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnUpdate()
    {
        // Під час гри зберігати сцену не можна
        if (Application.isPlaying || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.timeSinceStartup < nextSaveTime) return;

        nextSaveTime = EditorApplication.timeSinceStartup + IntervalSeconds;
        Save("за таймером");
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            Save("перед Play");
    }

    private static void Save(string reason)
    {
        if (Application.isPlaying) return;
        try
        {
            if (EditorSceneManager.SaveOpenScenes())
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[AutoSave] Сцену збережено ({reason}) о {System.DateTime.Now:HH:mm:ss}");
            }
        }
        catch (System.InvalidOperationException)
        {
            // Unity не дозволила зберегти (перехід у Play) — спробуємо наступного разу
        }
    }
}
#endif
