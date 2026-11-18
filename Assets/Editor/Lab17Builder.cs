#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Одноразові інструменти лаби 17 (скрипти редактора): оптимізація сцени і збірка гри.
// Кожен крок — окремий пункт меню, щоб після нього зняти замір і порівняти.
public static class Lab17Builder
{
    private const string Proj = "Assets/_Project";
    private const string CryptScene = Proj + "/Scenes/Crypt.unity";

    private static bool OpenCrypt()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path == CryptScene) return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(CryptScene);
        return true;
    }

    // Чи об'єкт рухається в грі: такі не можна позначати Static, інакше batching "приклеїть" їх до місця
    private static bool IsDynamic(GameObject go)
    {
        return go.GetComponentInParent<Door>() != null
            || go.GetComponentInParent<Pushable>() != null
            || go.GetComponentInParent<Chest>() != null
            || go.GetComponentInParent<Lever>() != null
            || go.GetComponentInParent<SpikeTrap>() != null
            || go.GetComponentInParent<PressurePlate>() != null
            || go.GetComponentInParent<RuneSocket>() != null
            || go.GetComponentInParent<Animator>() != null;
    }

    // ------------------------------------------------------------ 1. Static

    [MenuItem("Tools/Crypt/Лаба 17/1. Static batching і occluder/occludee")]
    private static void MakeStatic()
    {
        if (!OpenCrypt()) return;
        var env = GameObject.Find("Environment");
        if (env == null) { Debug.LogError("[Lab17] Немає Environment у сцені."); return; }

        const StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;

        int marked = 0, skipped = 0;
        foreach (var t in env.GetComponentsInChildren<Transform>(true))
        {
            var go = t.gameObject;
            bool hasMesh = go.GetComponent<MeshRenderer>() != null;
            if (IsDynamic(go))
            {
                if (hasMesh) skipped++;
                GameObjectUtility.SetStaticEditorFlags(go, 0);
                continue;
            }
            // Меш ProBuilder не має другого UV для lightmap — світло на нього не запікаємо
            var f = go.GetComponent("ProBuilderMesh") != null ? flags & ~StaticEditorFlags.ContributeGI : flags;
            Undo.RecordObject(go, "Static");
            GameObjectUtility.SetStaticEditorFlags(go, f);
            if (hasMesh) marked++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Lab17] Static: позначено {marked} об'єктів з мешем, рухомих пропущено {skipped}. Сцену збережено.");
    }

    // ------------------------------------------------------------ 2. Occlusion

    [MenuItem("Tools/Crypt/Лаба 17/2. Запекти occlusion culling")]
    private static void BakeOcclusion()
    {
        if (!OpenCrypt()) return;
        Debug.Log("[Lab17] Запікаю occlusion culling — редактор на кілька хвилин зависне, це нормально.");
        bool ok = StaticOcclusionCulling.Compute();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log(ok ? "[Lab17] Occlusion culling запечено." : "[Lab17] Occlusion не вдалося запекти — перевір, що об'єкти позначені Static (крок 1).");
    }

    // ------------------------------------------------------------ 3. Імпорт

    [MenuItem("Tools/Crypt/Лаба 17/3. Звук і текстури (налаштування імпорту)")]
    private static void ImportSettings()
    {
        int audio = 0, tex = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Proj + "/Resources/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) continue;

            var s = imp.defaultSampleSettings;
            if (Path.GetFileName(path).StartsWith("music_"))
            {
                // Музика довга: стрімінг із диска і стиснення Vorbis, щоб не тримати в пам'яті розпакованою
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
            }
            else
            {
                // Ефекти короткі: ADPCM і розпаковка при завантаженні — без затримки при відтворенні
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.SaveAndReimport();
            audio++;
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ThirdParty" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null || imp.textureType != TextureImporterType.Default) continue;

            bool changed = false;
            if (imp.maxTextureSize > 1024) { imp.maxTextureSize = 1024; changed = true; }
            if (imp.textureCompression != TextureImporterCompression.Compressed) { imp.textureCompression = TextureImporterCompression.Compressed; changed = true; }
            if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; changed = true; }
            if (changed) { imp.SaveAndReimport(); tex++; }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Lab17] Звуків налаштовано: {audio} (музика — стрімінг Vorbis, ефекти — ADPCM). Текстур змінено: {tex} (макс. 1024, стиснення, mipmaps).");
    }

    // ------------------------------------------------------------ 4. Світло

    [MenuItem("Tools/Crypt/Лаба 17/4. Запекти світло (довго, необов'язково)")]
    private static void BakeLight()
    {
        if (!OpenCrypt()) return;

        // Для lightmap моделі мають другий UV-канал. Вмикаємо його тільки для моделей, що стоять у сцені.
        var env = GameObject.Find("Environment");
        int models = 0;
        if (env != null)
        {
            var paths = env.GetComponentsInChildren<MeshFilter>(true)
                .Where(m => m.sharedMesh != null && m.gameObject.isStatic)
                .Select(m => AssetDatabase.GetAssetPath(m.sharedMesh))
                .Where(p => !string.IsNullOrEmpty(p)).Distinct();
            foreach (var p in paths)
            {
                var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                if (imp == null || imp.generateSecondaryUV) continue;
                imp.generateSecondaryUV = true;
                imp.SaveAndReimport();
                models++;
            }
        }

        // Усі світильники в режим Mixed: пряме світло лишається реальним (лицар і скелети освітлюються),
        // а відбите (непряме) освітлення запікається в lightmap
        int lights = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(l, "Light mode");
            l.lightmapBakeType = LightmapBakeType.Mixed;
            lights++;
        }

        if (!AssetDatabase.IsValidFolder(Proj + "/Settings")) AssetDatabase.CreateFolder(Proj, "Settings");
        string lsPath = Proj + "/Settings/CryptLighting.lighting";
        var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
        if (ls == null)
        {
            ls = new LightingSettings();
            AssetDatabase.CreateAsset(ls, lsPath);
        }
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;   // CPU, щоб не залежати від відеопам'яті
        ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
        ls.lightmapResolution = 6f;
        ls.lightmapMaxSize = 1024;
        ls.directSampleCount = 32;
        ls.indirectSampleCount = 128;
        ls.environmentSampleCount = 32;
        ls.maxBounces = 2;
        ls.autoGenerate = false;
        ls.compressLightmaps = true;
        ls.ao = false;
        EditorUtility.SetDirty(ls);

        Lightmapping.lightingSettings = ls;
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log($"[Lab17] Світло: моделей із UV2 — {models}, світильників у Mixed — {lights}. Запускаю запікання у фоні (смужка внизу Unity).");
        Lightmapping.BakeAsync();
    }

    // ------------------------------------------------------------ 5. Збірка

    [MenuItem("Tools/Crypt/Лаба 17/5. Збірка Windows x64")]
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
        if (scenes.Length == 0) { Debug.LogError("[Lab17] У Build Settings немає сцен."); return; }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(dir, "Crypt3D.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("[Lab17] Збірка почалась — це кілька хвилин.");
        var report = BuildPipeline.BuildPlayer(options);
        var sum = report.summary;

        if (sum.result == BuildResult.Succeeded)
        {
            string readme = Path.Combine(root, "README.md");
            if (File.Exists(readme)) File.Copy(readme, Path.Combine(dir, "README.md"), true);
            Debug.Log($"[Lab17] Збірка готова: {sum.outputPath}\nРозмір: {sum.totalSize / 1048576f:0.0} МБ, час: {sum.totalTime.TotalSeconds:0} с, помилок: {sum.totalErrors}, попереджень: {sum.totalWarnings}");
            EditorUtility.RevealInFinder(sum.outputPath);
        }
        else
        {
            Debug.LogError($"[Lab17] Збірка не вдалась: {sum.result}, помилок: {sum.totalErrors}. Дивись Console вище.");
        }
    }
}
#endif
