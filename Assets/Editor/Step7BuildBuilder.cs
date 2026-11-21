#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Збірка Windows x64 у папку Build/ (сцени беруться з Build Settings: MainMenu, Crypt, Depths).
// Старий пункт «Лаба 17/5» видалено разом з Lab17Builder, тому збірка тепер тут.
public static class Step7BuildBuilder
{
    [MenuItem("Tools/Crypt/Збірка Windows x64")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string root = Directory.GetParent(Application.dataPath).FullName;
        string dir = Path.Combine(root, "Build");
        Directory.CreateDirectory(dir);

        PlayerSettings.productName = "Crypt3D";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("[Збірка] У Build Settings немає сцен."); return; }
        Debug.Log("[Збірка] Сцени: " + string.Join(", ", scenes.Select(Path.GetFileNameWithoutExtension)));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(dir, "Crypt3D.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("[Збірка] Почалась — це кілька хвилин.");
        var summary = BuildPipeline.BuildPlayer(options).summary;

        if (summary.result == BuildResult.Succeeded)
        {
            string readme = Path.Combine(root, "README.md");
            if (File.Exists(readme)) File.Copy(readme, Path.Combine(dir, "README.md"), true);
            Debug.Log($"[Збірка] Готово: {summary.outputPath}\nРозмір: {summary.totalSize / 1048576f:0.0} МБ, час: {summary.totalTime.TotalSeconds:0} с, помилок: {summary.totalErrors}, попереджень: {summary.totalWarnings}");
            EditorUtility.RevealInFinder(summary.outputPath);
        }
        else
        {
            Debug.LogError($"[Збірка] Не вдалась: {summary.result}, помилок: {summary.totalErrors}. Дивись Console вище.");
        }
    }
}
#endif
