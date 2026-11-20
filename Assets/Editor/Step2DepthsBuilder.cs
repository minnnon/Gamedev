#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одноразовий Builder кроку 2б: сцена Depths («Нижня гробниця») — 3 зали, 2 коридори, чекпоінт, скелети, NavMesh.
// Сцена створюється копією Crypt (гравець, камера, UI, світло лишаються), рівень 1 з неї прибирається.
public static class Step2DepthsBuilder
{
    private const string Proj = "Assets/_Project";
    private const string CryptScene = Proj + "/Scenes/Crypt.unity";
    private const string DepthsScene = Proj + "/Scenes/Depths.unity";
    private const float FloorY = 0.05f;
    private const float TorchY = 2.3f;

    private static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    [MenuItem("Tools/Crypt/Сюжет/2б. Сцена Depths")]
    private static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DepthsScene) != null)
        {
            Debug.LogWarning("[Depths] Сцена Depths уже існує — нічого не змінюю. Щоб перебудувати, видали Depths.unity і Depths_NavMesh.asset.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. Копія Crypt → Depths
        if (!AssetDatabase.CopyAsset(CryptScene, DepthsScene)) { Debug.LogError("[Depths] Не вдалося скопіювати сцену."); return; }
        var scene = EditorSceneManager.OpenScene(DepthsScene);
        Lightmapping.lightingDataAsset = null;      // запечене світло Crypt до Depths не підходить
        cache.Clear();

        var gameplay = GameObject.Find("Gameplay");
        var player = GameObject.FindGameObjectWithTag("Player");
        var brazier = GameObject.Find("Checkpoint_Brazier");
        if (gameplay == null || player == null || brazier == null) { Debug.LogError("[Depths] Сцена не схожа на Crypt."); return; }

        // 2. Прибираємо рівень 1: усе в Gameplay, крім чекпоінта; Notes; Environment
        // Лишаємо чекпоінт і набір головоломки (ящик, плита, решітка) — їх перенесемо в зал A і коридор
        var crate = GameObject.Find("Crate_Pushable");
        var plate = GameObject.Find("PressurePlate");
        var portcullis = GameObject.Find("Portcullis_Crypt");
        var keep = new HashSet<GameObject> { brazier, crate, plate, portcullis };
        foreach (var t in gameplay.transform.Cast<Transform>().ToList())
            if (!keep.Contains(t.gameObject)) Object.DestroyImmediate(t.gameObject);
        DestroyIfExists("Notes");
        DestroyIfExists("Environment");

        // 3. Нова геометрія
        var env = new GameObject("Environment").transform;
        BuildGeometry(env);

        // 4. Старт, чекпоінт, скелети
        var start = new GameObject("PlayerStart").transform;
        start.SetParent(gameplay.transform, false);
        start.SetPositionAndRotation(new Vector3(0f, FloorY, 0f), Quaternion.identity);
        player.transform.SetPositionAndRotation(start.position, start.rotation);

        brazier.transform.SetPositionAndRotation(new Vector3(-4.6f, 0f, 4.6f), Quaternion.identity);
        SetupPuzzle(crate, plate, portcullis);
        SpawnEnemies(gameplay.transform);

        // 5. NavMesh
        RebakeNavMesh();

        // 6. Build Settings: MainMenu, Crypt, Depths
        AddToBuild(DepthsScene);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Depths] Готово: сцена Depths створена й додана в Build Settings (індекс 2).");
    }

    // Підсуває тригер виходу в Crypt до самої брами (z 8..16, працює лише коли брама відкрита), щоб не залежати від підйому сходами
    [MenuItem("Tools/Crypt/Сюжет/2б-fix. Тригер виходу в Crypt")]
    private static void FixExitZone()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(CryptScene);
        var zone = GameObject.Find("ExitZone");
        var box = zone != null ? zone.GetComponent<BoxCollider>() : null;
        if (box == null) { Debug.LogError("[Depths] Не знайдено ExitZone у Crypt."); return; }
        Undo.RecordObject(box, "ExitZone");
        box.center = new Vector3(0f, 0f, -2f);
        box.size = new Vector3(5f, 8f, 8f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Depths] Тригер виходу розширено до брами, сцену Crypt збережено.");
    }

    // ---------------------------------------------------------------- ГЕОМЕТРІЯ

    // Тайл підлоги 4x4. Зали: A (старт) z -2..6, коридор 6..14, B z 14..26, коридор 26..34, C (гробниця) z 34..46.
    private static void BuildGeometry(Transform env)
    {
        BuildRoom(Child(env, "Hall_A"), 0f, 2, false, true, new Color(1f, 0.7f, 0.4f));
        BuildCorridor(Child(env, "Corridor_AB"), 8f, 2);
        BuildRoom(Child(env, "Hall_B"), 16f, 3, true, true, new Color(1f, 0.65f, 0.35f));
        BuildCorridor(Child(env, "Corridor_BC"), 28f, 2);
        BuildRoom(Child(env, "Hall_C"), 36f, 3, true, false, new Color(0.55f, 0.7f, 1f));
    }

    // zFirst — центр першого ряду плиток; рядів rows; doorS/doorN — прохід у південній/північній стіні
    private static void BuildRoom(Transform room, float zFirst, int rows, bool doorS, bool doorN, Color lightColor)
    {
        var floor = Child(room, "Floor");
        var walls = Child(room, "Walls");
        float south = zFirst - 2f;
        float north = zFirst + 4f * (rows - 1) + 2f;
        float mid = (south + north) / 2f;

        for (int r = 0; r < rows; r++)
            for (int x = -4; x <= 4; x += 4)
                Put("floor_tile_large", new Vector3(x, 0f, zFirst + 4f * r), 0f, floor);

        for (int x = -4; x <= 4; x += 4)
        {
            Put(doorS && x == 0 ? "wall_open_scaffold" : "wall", new Vector3(x, 0f, south), 0f, walls);
            Put(doorN && x == 0 ? "wall_open_scaffold" : (!doorN && x == 0 ? "wall_arched" : "wall"), new Vector3(x, 0f, north), 0f, walls);
        }
        for (int r = 0; r < rows; r++)
        {
            float z = zFirst + 4f * r;
            string type = rows >= 3 && r == rows / 2 ? "wall_pillar" : "wall";
            WallZ(type, -6f, z, walls);
            WallZ(type, 6f, z, walls);
        }
        foreach (float cx in new[] { -6f, 6f })
            foreach (float cz in new[] { south, north })
                Put("pillar", new Vector3(cx, 0f, cz), 0f, walls).transform.localScale = new Vector3(1f, 1.01f, 1f);

        PutFacing("TorchWall", new Vector3(-5.25f, TorchY, mid), Vector3.right, room);
        PutFacing("TorchWall", new Vector3(5.25f, TorchY, mid), Vector3.left, room);

        var lightGo = new GameObject("RoomLight");
        lightGo.transform.SetParent(room, false);
        lightGo.transform.position = new Vector3(0f, 3.2f, mid);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.intensity = 5f;
        l.range = 11f;
        l.shadows = LightShadows.None;
    }

    // Вузький коридор шириною в один тайл; zCenter — центр першого тайла, tiles — скільки їх
    private static void BuildCorridor(Transform corridor, float zFirst, int tiles)
    {
        for (int i = 0; i < tiles; i++)
        {
            float z = zFirst + 4f * i;
            Put("floor_tile_large", new Vector3(0f, 0f, z), 0f, corridor);
            WallZ("wall", -2f, z, corridor);
            WallZ("wall", 2f, z, corridor);
        }
        PutFacing("TorchWall", new Vector3(-1.5f, TorchY, zFirst + 2f), Vector3.right, corridor);
    }

    // Головоломка: ящик з зали A треба штовхнути на плиту — решітка в коридорі до зали B відчиниться
    private static void SetupPuzzle(GameObject crate, GameObject plate, GameObject portcullis)
    {
        if (crate == null || plate == null || portcullis == null) { Debug.LogWarning("[Depths] Не знайдено ящик, плиту або решітку."); return; }
        portcullis.name = "Portcullis_AB";
        portcullis.transform.position = new Vector3(0f, 0f, 10f);
        plate.transform.position = new Vector3(-3f, FloorY, 2.5f);
        crate.transform.position = new Vector3(3f, FloorY, 2.5f);

        var door = portcullis.GetComponent<Door>();
        var pp = plate.GetComponent<PressurePlate>();
        if (door == null || pp == null) { Debug.LogWarning("[Depths] На решітці немає Door або на плиті PressurePlate."); return; }
        var so = new SerializedObject(pp);
        so.FindProperty("door").objectReferenceValue = door;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- СКЕЛЕТИ

    private static void SpawnEnemies(Transform gameplay)
    {
        var enemies = Child(gameplay, "Enemies");
        var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Characters/Skeleton.prefab");
        var potion = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Items/Pickup_Potion.prefab");
        var coins = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Items/Pickup_Coins.prefab");
        if (skeleton == null) { Debug.LogError("[Depths] Немає Skeleton.prefab."); return; }

        // Зал B — двоє, кожен патрулює свою половину; зал C — один біля гробниці
        SpawnSkeleton(enemies, skeleton, "Skeleton_B_West", coins,
            new Vector3(-3f, FloorY, 17.5f), new Vector3(-3f, FloorY, 22.5f));
        SpawnSkeleton(enemies, skeleton, "Skeleton_B_East", potion,
            new Vector3(3f, FloorY, 22.5f), new Vector3(3f, FloorY, 17.5f));
        SpawnSkeleton(enemies, skeleton, "Skeleton_C", potion,
            new Vector3(-3f, FloorY, 40f), new Vector3(3f, FloorY, 40f), new Vector3(3f, FloorY, 37f));
    }

    private static void SpawnSkeleton(Transform enemies, GameObject prefab, string name, GameObject drop, params Vector3[] points)
    {
        var route = Child(enemies, "Route_" + name);
        var wps = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            wps[i] = Child(route, "WP_" + i);
            wps[i].position = points[i];
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies);
        go.name = name;
        go.transform.SetPositionAndRotation(points[0], Quaternion.identity);

        var so = new SerializedObject(go.GetComponent<EnemyAI>());
        var arr = so.FindProperty("waypoints");
        arr.arraySize = wps.Length;
        for (int i = 0; i < wps.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        var ec = go.GetComponent<EnemyCombat>();
        if (ec != null && drop != null)
        {
            var eso = new SerializedObject(ec);
            eso.FindProperty("dropPrefab").objectReferenceValue = drop;
            eso.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------------------------------------------------------------- NAVMESH І BUILD SETTINGS

    private static void RebakeNavMesh()
    {
        var navGo = GameObject.Find("NavMesh");
        var surface = navGo != null ? navGo.GetComponent<NavMeshSurface>() : null;
        if (surface == null) { Debug.LogError("[Depths] Об'єкт NavMesh не знайдено — навігацію не запечено."); return; }
        surface.navMeshData = null;         // не чіпаємо навмеш Crypt: у Depths буде свій ассет
        surface.BuildNavMesh();
        if (surface.navMeshData != null)
        {
            AssetDatabase.CreateAsset(surface.navMeshData, Proj + "/Scenes/Depths_NavMesh.asset");
            EditorUtility.SetDirty(surface);
            Debug.Log("[Depths] NavMesh запечено.");
        }
    }

    private static void AddToBuild(string path)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ---------------------------------------------------------------- ПОМІЧНИКИ

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static Transform Child(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    private static GameObject P(string name)
    {
        if (cache.TryGetValue(name, out var cached) && cached != null) return cached;
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Proj}/Prefabs/Level/{name}.prefab");
        if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Proj}/Prefabs/Props/{name}.prefab");
        if (p == null) Debug.LogError("[Depths] Немає префаба: " + name);
        cache[name] = p;
        return p;
    }

    private static GameObject Put(string prefab, Vector3 pos, float yaw, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(P(prefab), parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return go;
    }

    // Ставить предмет так, щоб його "лице" дивилось уздовж normal (для смолоскипів на стінах)
    private static GameObject PutFacing(string prefab, Vector3 pos, Vector3 normal, Transform parent)
    {
        var go = Put(prefab, pos, 0f, parent);
        Vector3 d = WorldBounds(go).center - go.transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0004f) d = Vector3.forward;
        go.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(d.normalized, normal, Vector3.up), 0f);
        return go;
    }

    // Стіна вздовж осі Z (бічна)
    private static void WallZ(string prefab, float x, float z, Transform parent)
    {
        Put(prefab, new Vector3(x, 0f, z), 90f, parent).transform.localScale = new Vector3(1f, 1.002f, 1f);
    }
}
#endif
