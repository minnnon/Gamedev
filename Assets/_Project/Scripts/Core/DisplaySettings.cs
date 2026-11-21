using UnityEngine;
using UnityEngine.InputSystem;

// Повноекранний режим у збірці: при запуску — вікно на весь екран без рамки (закриває панель завдань),
// F11 або Alt+Enter перемикає на віконний режим і назад.
public class DisplaySettings : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Application.isEditor) return;     // у редакторі вікном керує Unity
        var go = new GameObject("DisplaySettings");
        go.AddComponent<DisplaySettings>();
        DontDestroyOnLoad(go);
    }

    private void Start() { SetFullscreen(true); }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        bool alt = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
        if (kb.f11Key.wasPressedThisFrame || (alt && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)))
            SetFullscreen(Screen.fullScreenMode == FullScreenMode.Windowed);
    }

    private static void SetFullscreen(bool full)
    {
        if (full)
        {
            // Розмір поточного монітора, а не збережений у реєстрі від минулих запусків
            var d = Display.main;
            Screen.SetResolution(d.systemWidth, d.systemHeight, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
        Debug.Log("[Екран] " + (full ? "на весь екран" : "віконний режим"));
    }
}
