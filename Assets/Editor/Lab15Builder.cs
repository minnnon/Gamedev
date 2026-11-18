#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Одноразовий інструмент лаби 15 (скрипт редактора): бій, предмети, інвентар,
// рунні гнізда, крипта зі Вартовим і перезапікання NavMesh.
public static class Lab15Builder
{
    private const string Proj = "Assets/_Project";
    private const string ScenePath = Proj + "/Scenes/Crypt.unity";
    private const string Dungeon = "Assets/ThirdParty/KayKit_Dungeon/";
    private const string Skel = "Assets/ThirdParty/KayKit_Skeletons/";
    private const string InputPath = Proj + "/Settings/InputSystem_Actions.inputactions";
    private const float FloorY = 0.05f;
    private const float TorchY = 2.3f;

    private static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    [MenuItem("Tools/Crypt/Лаба 15/Бій, інвентар, крипта")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }
        if (GameObject.Find("CryptWing") != null) { Debug.LogWarning("[Lab15] Лабу 15 уже налаштовано — нічого не змінюю."); return; }
        var env = GameObject.Find("Environment");
        var gameplay = GameObject.Find("Gameplay");
        if (env == null || gameplay == null) { Debug.LogError("[Lab15] Сцена не схожа на Crypt."); return; }

        Undo.SetCurrentGroupName("Лаба 15");
        var report = new List<string>();
        int enemyLayer = Mathf.Max(0, LayerMask.NameToLayer("Enemy"));

        // --- Предмети (ScriptableObject) і те, як вони лежать на землі
        var potion = MakeItem("Item_Potion", "Зілля здоров'я", ItemType.Potion, "Відновлює 2 HP.", 2, new Color(0.3f, 0.85f, 0.35f));
        var rune = MakeItem("Item_Rune", "Рунний ключ", ItemType.Rune, "Вставляється в гніздо Рунної брами.", 0, new Color(0.2f, 0.8f, 1f));
        var coin = MakeItem("Item_Coin", "Монети", ItemType.Coin, "Золото зі склепу.", 0, new Color(1f, 0.8f, 0.2f));

        var runeActive = MakeLit("M_RuneActive", new Color(0.3f, 0.9f, 1f), null, new Color(0.3f, 2.2f, 3.2f));
        var gold = MakeLit("M_Gold", Color.white, AssetDatabase.LoadAssetAtPath<Texture2D>(Dungeon + "dungeon_texture.png"), new Color(0.55f, 0.45f, 0.2f));
        gold.SetTexture("_EmissionMap", gold.GetTexture("_BaseMap"));
        gold.SetFloat("_Smoothness", 0.5f);
        EditorUtility.SetDirty(gold);

        var potionPickup = MakePotionPickup(potion);
        var runePickup = MakeRunePickup(rune, runeActive);
        var coinPickup = MakeCoinPickup(coin, gold);
        potion.pickupPrefab = potionPickup;
        rune.pickupPrefab = runePickup;
        coin.pickupPrefab = coinPickup;
        EditorUtility.SetDirty(potion);
        EditorUtility.SetDirty(rune);
        EditorUtility.SetDirty(coin);
        report.Add("Предмети: Зілля здоров'я, Рунний ключ, Монети (ассети в _Project/Items)");

        // --- Гравець: бій та інвентар
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        using (var scope = new PrefabUtility.EditPrefabContentsScope(Proj + "/Prefabs/Characters/Player.prefab"))
        {
            var root = scope.prefabContentsRoot;
            if (root.GetComponent<Inventory>() == null) root.AddComponent<Inventory>();
            var combat = root.GetComponent<PlayerCombat>();
            if (combat == null) combat = root.AddComponent<PlayerCombat>();
            var so = new SerializedObject(combat);
            so.FindProperty("animator").objectReferenceValue = root.GetComponentInChildren<Animator>();
            so.FindProperty("inputActions").objectReferenceValue = input;
            so.FindProperty("enemyMask").intValue = 1 << enemyLayer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        report.Add("Гравець: PlayerCombat (удар ЛКМ, шкода 1) та Inventory (8 комірок)");

        // --- Скелети: бойова частина і здобич
        string skeletonPath = Proj + "/Prefabs/Characters/Skeleton.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(skeletonPath))
        {
            var root = scope.prefabContentsRoot;
            var ec = root.GetComponent<EnemyCombat>();
            if (ec == null) ec = root.AddComponent<EnemyCombat>();
            var so = new SerializedObject(ec);
            so.FindProperty("animator").objectReferenceValue = root.GetComponentInChildren<Animator>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        SetDrop("Skeleton_Corridor", potionPickup);
        SetDrop("Skeleton_West", coinPickup);
        SetDrop("Skeleton_East", potionPickup);
        report.Add("Скелети: EnemyCombat (шкода 1, 3 HP), після смерті лишають зілля або монети");

        // --- Скрині, монети, руни
        SetLoot("Chest_Rune1", new[] { rune, potion }, new[] { 1, 1 });
        SetLoot("Chest_Rune2", new[] { rune }, new[] { 1 });
        int coins = MakeCoinPiles(coin, gold);
        report.Add($"Скрині: у кожній руна (у першій ще й зілля); купок монет, що підбираються: {coins}");
        SetupRuneGate(rune, runeActive, report);

        // --- Крипта і Вартовий
        var crypt = Child(env.transform, "CryptWing");
        Undo.RegisterCreatedObjectUndo(crypt.gameObject, "Крипта");
        BuildCrypt(crypt);
        var guardian = MakeGuardianPrefab(enemyLayer, runePickup);
        if (guardian != null) SpawnGuardian(guardian, gameplay.transform);
        report.Add("Крипта: коридор і зал за південною решіткою, Вартовий (8 HP, шкода 2) лишає третю руну");

        RebakeNavMesh(report);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Lab15] Готово, сцену збережено:\n- " + string.Join("\n- ", report));
    }

    // ---------------------------------------------------------------- ПРЕДМЕТИ

    private static ItemData MakeItem(string file, string title, ItemType type, string description, int heal, Color color)
    {
        string path = $"{Proj}/Items/{file}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<ItemData>();
        item.displayName = title;
        item.type = type;
        item.description = description;
        item.healAmount = heal;
        item.color = color;
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static Material MakeLit(string name, Color color, Texture tex, Color emission)
    {
        string path = $"{Proj}/Materials/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Smoothness", 0.6f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static GameObject NewPickupRoot(string name, ItemData item, int amount, float spin)
    {
        var root = new GameObject(name);
        var sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.7f;
        sc.center = new Vector3(0f, 0.5f, 0f);
        var p = root.AddComponent<Pickup>();
        var so = new SerializedObject(p);
        so.FindProperty("item").objectReferenceValue = item;
        so.FindProperty("amount").intValue = amount;
        so.FindProperty("spinSpeed").floatValue = spin;
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static void AddGlow(GameObject root, Color color, float intensity)
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = 3.5f;
        l.shadows = LightShadows.None;
    }

    private static GameObject SavePickup(GameObject root)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Proj}/Prefabs/Items/{root.name}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject LoadPickup(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{Proj}/Prefabs/Items/{name}.prefab");
    }

    private static GameObject MakePotionPickup(ItemData item)
    {
        var existing = LoadPickup("Pickup_Potion");
        if (existing != null) return existing;
        var root = NewPickupRoot("Pickup_Potion", item, 1, 90f);
        string fbx = Dungeon + "bottle_A_labeled_green.fbx";
        var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (imp != null && imp.materialImportMode != ModelImporterMaterialImportMode.None)
        {
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
        }
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (src != null)
        {
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(src);
            vis.name = "Model";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            vis.transform.localScale *= 1.6f;
            Paint(vis, AssetDatabase.LoadAssetAtPath<Material>(Proj + "/Materials/M_Dungeon.mat"));
        }
        AddGlow(root, new Color(0.4f, 1f, 0.5f), 1.5f);
        return SavePickup(root);
    }

    private static GameObject MakeRunePickup(ItemData item, Material runeMat)
    {
        var existing = LoadPickup("Pickup_Rune");
        if (existing != null) return existing;
        var root = NewPickupRoot("Pickup_Rune", item, 1, 120f);
        var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = "Crystal";
        Object.DestroyImmediate(crystal.GetComponent<Collider>());
        crystal.transform.SetParent(root.transform, false);
        crystal.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        crystal.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        crystal.transform.localScale = Vector3.one * 0.35f;
        crystal.GetComponent<Renderer>().sharedMaterial = runeMat;
        AddGlow(root, new Color(0.3f, 0.85f, 1f), 3f);
        return SavePickup(root);
    }

    private static GameObject MakeCoinPickup(ItemData item, Material gold)
    {
        var existing = LoadPickup("Pickup_Coins");
        if (existing != null) return existing;
        var root = NewPickupRoot("Pickup_Coins", item, 5, 0f);
        var src = P("coin_stack_small");
        if (src != null)
        {
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(src);
            vis.transform.SetParent(root.transform, false);
            Paint(vis, gold);
        }
        return SavePickup(root);
    }

    // Купки монет у скарбниці з декору стають предметами
    private static int MakeCoinPiles(ItemData coin, Material gold)
    {
        int n = 0;
        var room = GameObject.Find("Room_Treasure");
        if (room == null) return 0;
        foreach (Transform t in room.transform)
        {
            if (!t.name.StartsWith("coin_stack") || t.GetComponent<Pickup>() != null) continue;
            int amount = t.name.Contains("large") ? 20 : t.name.Contains("medium") ? 10 : 5;
            var sc = Undo.AddComponent<SphereCollider>(t.gameObject);
            sc.isTrigger = true;
            sc.radius = 0.8f;
            sc.center = new Vector3(0f, 0.3f, 0f);
            var p = Undo.AddComponent<Pickup>(t.gameObject);
            var so = new SerializedObject(p);
            so.FindProperty("item").objectReferenceValue = coin;
            so.FindProperty("amount").intValue = amount;
            so.FindProperty("spinSpeed").floatValue = 0f;
            so.ApplyModifiedProperties();
            Paint(t.gameObject, gold);
            n++;
        }
        return n;
    }

    private static void SetDrop(string enemyName, GameObject drop)
    {
        var go = GameObject.Find(enemyName);
        var ec = go != null ? go.GetComponent<EnemyCombat>() : null;
        if (ec == null) return;
        var so = new SerializedObject(ec);
        so.FindProperty("dropPrefab").objectReferenceValue = drop;
        so.ApplyModifiedProperties();
    }

    private static void SetLoot(string chestName, ItemData[] items, int[] amounts)
    {
        var go = GameObject.Find(chestName);
        var chest = go != null ? go.GetComponent<Chest>() : null;
        if (chest == null) { Debug.LogWarning("[Lab15] Не знайдено скриню " + chestName); return; }
        var so = new SerializedObject(chest);
        var arr = so.FindProperty("loot");
        arr.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("item").objectReferenceValue = items[i];
            el.FindPropertyRelative("amount").intValue = amounts[i];
        }
        so.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------------- РУННА БРАМА

    private static void SetupRuneGate(ItemData rune, Material active, List<string> report)
    {
        var gateGo = GameObject.Find("RuneGate");
        if (gateGo == null) { report.Add("УВАГА: не знайдено RuneGate"); return; }
        var gate = gateGo.GetComponent<RuneGate>();
        if (gate == null) gate = Undo.AddComponent<RuneGate>(gateGo);

        var sockets = new List<RuneSocket>();
        foreach (Transform t in gateGo.transform)
        {
            if (!t.name.StartsWith("RuneSocket")) continue;
            var s = t.GetComponent<RuneSocket>();
            if (s == null) s = Undo.AddComponent<RuneSocket>(t.gameObject);
            var crystal = t.Find("Crystal");
            var so = new SerializedObject(s);
            so.FindProperty("gate").objectReferenceValue = gate;
            so.FindProperty("runeItem").objectReferenceValue = rune;
            so.FindProperty("crystal").objectReferenceValue = crystal != null ? crystal.GetComponent<Renderer>() : null;
            so.FindProperty("activeMaterial").objectReferenceValue = active;
            so.ApplyModifiedProperties();
            sockets.Add(s);
        }

        var barrier = gateGo.transform.Find("RuneBarrier");
        var cryptDoorGo = GameObject.Find("Portcullis_Crypt");
        var gso = new SerializedObject(gate);
        var arr = gso.FindProperty("sockets");
        arr.arraySize = sockets.Count;
        for (int i = 0; i < sockets.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sockets[i];
        gso.FindProperty("barrier").objectReferenceValue = barrier != null ? barrier.gameObject : null;
        gso.FindProperty("cryptDoor").objectReferenceValue = cryptDoorGo != null ? cryptDoorGo.GetComponent<Door>() : null;
        gso.ApplyModifiedProperties();
        report.Add($"Рунна брама: гнізд {sockets.Count}; 2 руни відкривають крипту, 3 — знімають бар'єр");
    }

    // ---------------------------------------------------------------- КРИПТА

    private static void BuildCrypt(Transform crypt)
    {
        var corridor = Child(crypt, "Corridor");
        for (int z = -16; z <= -12; z += 4)
        {
            Put("floor_tile_large", new Vector3(0f, 0f, z), 0f, corridor);
            WallZ("wall", -2f, z, corridor);
            WallZ("wall", 2f, z, corridor);
        }
        PutFacing("TorchWall", new Vector3(-1.5f, TorchY, -14f), Vector3.right, corridor);

        var room = Child(crypt, "Room");
        var floor = Child(room, "Floor");
        var walls = Child(room, "Walls");
        for (int x = -4; x <= 4; x += 4)
            for (int z = -28; z <= -20; z += 4)
                Put("floor_tile_large", new Vector3(x, 0f, z), 0f, floor);
        for (int x = -4; x <= 4; x += 4)
        {
            Put(x == 0 ? "wall_open_scaffold" : "wall", new Vector3(x, 0f, -18f), 0f, walls);
            Put(x == 0 ? "wall_arched" : "wall", new Vector3(x, 0f, -30f), 0f, walls);
        }
        for (int z = -28; z <= -20; z += 4)
        {
            WallZ(z == -24 ? "wall_pillar" : "wall", -6f, z, walls);
            WallZ(z == -24 ? "wall_pillar" : "wall", 6f, z, walls);
        }
        foreach (int cx in new[] { -6, 6 })
            foreach (int cz in new[] { -30, -18 })
                Put("pillar", new Vector3(cx, 0f, cz), 0f, walls).transform.localScale = new Vector3(1f, 1.01f, 1f);

        PutFacing("TorchWall", new Vector3(-5.25f, TorchY, -24f), Vector3.right, room);
        PutFacing("TorchWall", new Vector3(5.25f, TorchY, -24f), Vector3.left, room);
        PutFacing("banner_red", new Vector3(-4f, 0f, -30f), Vector3.forward, room);
        PutFacing("banner_red", new Vector3(4f, 0f, -30f), Vector3.forward, room);
        Put("pillar_decorated", new Vector3(-4f, 0f, -27.5f), 0f, room);
        Put("pillar_decorated", new Vector3(4f, 0f, -27.5f), 0f, room);

        // Холодне світло в центрі зали
        var lightGo = new GameObject("CryptLight");
        lightGo.transform.SetParent(room, false);
        lightGo.transform.position = new Vector3(0f, 3.2f, -25f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.5f, 0.65f, 1f);
        l.intensity = 5f;
        l.range = 11f;
        l.shadows = LightShadows.None;
    }

    private static GameObject MakeGuardianPrefab(int enemyLayer, GameObject runePickup)
    {
        string prefabPath = Proj + "/Prefabs/Characters/Skeleton_Guardian.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        const float scale = 1.35f;
        string fbx = Skel + "Skeleton_Warrior.fbx";
        if (!PrepareModel(fbx, true)) { Debug.LogError("[Lab15] Немає моделі " + fbx); return null; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Proj + "/Materials/M_Skeleton.mat");
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Proj + "/Animations/Skeleton.controller");

        var root = new GameObject("Skeleton_Guardian");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        model.transform.SetParent(root.transform, false);
        Paint(model, mat);

        Transform foot = FindDeep(model.transform, "foot.l"), toes = FindDeep(model.transform, "toes.l");
        if (foot != null && toes != null)
        {
            Vector3 f = toes.position - foot.position;
            f.y = 0f;
            if (f.sqrMagnitude > 0.0001f)
                model.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(f.normalized, Vector3.forward, Vector3.up), Vector3.up) * model.transform.rotation;
        }
        model.transform.localScale *= scale;   // Вартовий більший за звичайних скелетів
        float height = Mathf.Clamp(WorldBounds(model).max.y, 2f, 4f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.55f;
        agent.height = height;
        agent.angularSpeed = 540f;
        agent.acceleration = 16f;
        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, height / 2f, 0f);
        col.radius = 0.6f;
        col.height = height;
        root.AddComponent<Rigidbody>().isKinematic = true;

        var hp = root.AddComponent<Health>();
        var hso = new SerializedObject(hp);
        hso.FindProperty("maxHealth").intValue = 8;
        hso.FindProperty("invulnerabilityTime").floatValue = 0.4f;
        hso.ApplyModifiedPropertiesWithoutUndo();

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;

        var ai = root.AddComponent<EnemyAI>();
        var aso = new SerializedObject(ai);
        aso.FindProperty("animator").objectReferenceValue = animator;
        aso.FindProperty("viewRadius").floatValue = 11f;
        aso.FindProperty("chaseSpeed").floatValue = 3.8f;
        aso.FindProperty("attackRange").floatValue = 2.4f;
        aso.FindProperty("attackCooldown").floatValue = 1.7f;
        aso.ApplyModifiedPropertiesWithoutUndo();

        var ec = root.AddComponent<EnemyCombat>();
        var eso = new SerializedObject(ec);
        eso.FindProperty("animator").objectReferenceValue = animator;
        eso.FindProperty("damage").intValue = 2;
        eso.FindProperty("hitRange").floatValue = 3f;
        eso.FindProperty("dropPrefab").objectReferenceValue = runePickup;
        eso.ApplyModifiedPropertiesWithoutUndo();

        AttachWeapon(model, "handslot.r", Skel + "Skeleton_Axe.fbx", "Axe", mat);
        AttachWeapon(model, "handslot.l", Skel + "Skeleton_Shield_Large_A.fbx", "Shield", mat);

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = enemyLayer;
        model.name = "Model";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static bool PrepareModel(string fbx, bool generic)
    {
        var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (imp == null) return false;
        bool dirty = imp.materialImportMode != ModelImporterMaterialImportMode.None;
        if (generic && imp.animationType != ModelImporterAnimationType.Generic) dirty = true;
        if (!dirty) return true;
        imp.materialImportMode = ModelImporterMaterialImportMode.None;
        if (generic) imp.animationType = ModelImporterAnimationType.Generic;
        imp.SaveAndReimport();
        return true;
    }

    private static void AttachWeapon(GameObject model, string bone, string fbx, string name, Material mat)
    {
        Transform hand = FindDeep(model.transform, bone);
        if (hand == null || !PrepareModel(fbx, false)) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        go.name = name;
        go.transform.SetParent(hand, false);
        Paint(go, mat);
    }

    private static void SpawnGuardian(GameObject prefab, Transform gameplay)
    {
        var enemies = gameplay.Find("Enemies");
        if (enemies == null) enemies = Child(gameplay, "Enemies");
        var route = Child(enemies, "Route_Guardian");
        Vector3[] points = { new Vector3(0f, FloorY, -26f), new Vector3(-3f, FloorY, -22.5f), new Vector3(3f, FloorY, -22.5f) };
        var wps = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            wps[i] = Child(route, "WP_" + i);
            wps[i].position = points[i];
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies);
        go.name = "Skeleton_Guardian";
        go.transform.SetPositionAndRotation(points[0], Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(go, "Вартовий");

        var so = new SerializedObject(go.GetComponent<EnemyAI>());
        var arr = so.FindProperty("waypoints");
        arr.arraySize = wps.Length;
        for (int i = 0; i < wps.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        so.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------------- NAVMESH

    private static void RebakeNavMesh(List<string> report)
    {
        var navGo = GameObject.Find("NavMesh");
        var surface = navGo != null ? navGo.GetComponent<NavMeshSurface>() : null;
        if (surface == null) { report.Add("УВАГА: об'єкт NavMesh не знайдено — навігацію для крипти не запечено"); return; }
        surface.BuildNavMesh();
        if (surface.navMeshData != null)
        {
            AssetDatabase.CreateAsset(surface.navMeshData, Proj + "/Scenes/Crypt_NavMesh.asset");
            EditorUtility.SetDirty(surface);
            report.Add("NavMesh перезапечено разом із криптою");
        }
    }

    // ---------------------------------------------------------------- ПОМІЧНИКИ

    private static void Paint(GameObject go, Material mat)
    {
        if (mat == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
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
        if (p == null) Debug.LogError("[Lab15] Немає префаба: " + name);
        cache[name] = p;
        return p;
    }

    private static GameObject Put(string prefab, Vector3 pos, float yaw, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(P(prefab), parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return go;
    }

    private static GameObject PutFacing(string prefab, Vector3 pos, Vector3 normal, Transform parent)
    {
        var go = Put(prefab, pos, 0f, parent);
        Vector3 d = WorldBounds(go).center - go.transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0004f) d = Vector3.forward;
        go.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(d.normalized, normal, Vector3.up), 0f);
        return go;
    }

    private static void WallZ(string prefab, float x, float z, Transform parent)
    {
        Put(prefab, new Vector3(x, 0f, z), 90f, parent).transform.localScale = new Vector3(1f, 1.002f, 1f);
    }
}
#endif
