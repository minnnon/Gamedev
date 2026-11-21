using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// Лічильник продуктивності для тестування (лаба 17).
// F3 — показати/сховати панель (FPS, час кадру, пам'ять; в редакторі ще Batches, SetPass, Tris).
// F4 — записати замір у Console і в CSV-файл. Знімай замір, стоячи в одному й тому самому місці.
public class PerfOverlay : MonoBehaviour
{
    private const int Window = 120;     // скільки останніх кадрів усереднюємо

    private readonly float[] frames = new float[Window];
    private int index;
    private int filled;
    private bool visible;
    private string lastSnapshot = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("PerfOverlay");
        go.AddComponent<PerfOverlay>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        frames[index] = Time.unscaledDeltaTime;
        index = (index + 1) % Window;
        if (filled < Window) filled++;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f3Key.wasPressedThisFrame) visible = !visible;
        if (kb.f4Key.wasPressedThisFrame) Snapshot();
    }

    private void Stats(out float fps, out float avgMs, out float maxMs)
    {
        float sum = 0f, max = 0f;
        for (int i = 0; i < filled; i++) { sum += frames[i]; if (frames[i] > max) max = frames[i]; }
        float avg = filled > 0 ? sum / filled : 0.016f;
        fps = 1f / Mathf.Max(avg, 0.0001f);
        avgMs = avg * 1000f;
        maxMs = max * 1000f;
    }

    private string Describe()
    {
        Stats(out float fps, out float avgMs, out float maxMs);
        float managedMb = System.GC.GetTotalMemory(false) / 1048576f;
        var sb = new StringBuilder();
        sb.AppendLine($"FPS: {fps:0}");
        sb.AppendLine($"Кадр: {avgMs:0.0} мс (макс {maxMs:0.0})");
        sb.AppendLine($"Пам'ять C#: {managedMb:0.0} МБ");
#if UNITY_EDITOR
        sb.AppendLine($"Batches: {UnityEditor.UnityStats.batches}");
        sb.AppendLine($"SetPass calls: {UnityEditor.UnityStats.setPassCalls}");
        sb.AppendLine($"Triangles: {UnityEditor.UnityStats.triangles}");
        sb.AppendLine($"Static batched: {UnityEditor.UnityStats.staticBatchedDrawCalls}");
#endif
        return sb.ToString();
    }

    private void Snapshot()
    {
        Stats(out float fps, out float avgMs, out float maxMs);
        float managedMb = System.GC.GetTotalMemory(false) / 1048576f;
        int batches = 0, setPass = 0, tris = 0;
#if UNITY_EDITOR
        batches = UnityEditor.UnityStats.batches;
        setPass = UnityEditor.UnityStats.setPassCalls;
        tris = UnityEditor.UnityStats.triangles;
#endif
        string line = $"{System.DateTime.Now:HH:mm:ss};{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};{fps:0.0};{avgMs:0.00};{maxMs:0.00};{batches};{setPass};{tris};{managedMb:0.0}";
        string path = Path.Combine(Application.persistentDataPath, "perf_log.csv");
        try
        {
            if (!File.Exists(path)) File.WriteAllText(path, "time;scene;fps;frame_ms;max_ms;batches;setpass;triangles;managed_mb\n");
            File.AppendAllText(path, line + "\n");
        }
        catch { /* файл недоступний — лишається запис у Console */ }

        lastSnapshot = "Замір записано: " + line;
        Debug.Log("[Perf] " + line + "\n(файл: " + path + ")");
    }

    private void OnGUI()
    {
        if (!visible) return;
        var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 18 };
        GUI.Box(new Rect(Screen.width - 330f, 10f, 320f, 190f), Describe() + "\nF3 — сховати, F4 — замір", style);
        if (!string.IsNullOrEmpty(lastSnapshot))
            GUI.Label(new Rect(Screen.width - 330f, 205f, 320f, 24f), "Замір записано");
    }
}
