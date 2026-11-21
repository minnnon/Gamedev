#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Одноразовий Builder кроку 2г: гробниця, бос «Хазяїн гробниці» (префаб + вимкнений екземпляр у залі C),
// записка 7 і фінальний текст у сцені Depths.
public static class Step2dBossBuilder
{
    private const string Proj = "Assets/_Project";
    private const string Skel = "Assets/ThirdParty/KayKit_Skeletons/";
    private const string DepthsScene = Proj + "/Scenes/Depths.unity";
    private const float FloorY = 0.05f;

    private const string NoteText = "Якщо перстень повернути — він ляже назад. Мабуть. Сам я перевіряти не буду.\n\n— Гнат";
    private const string FinalText = "Перстень повернувся на місце, і склеп затих. Гната я так і не знайшов. Тільки його остання записка лежала біля самої брами — наче він знав, що хтось прийде.";

    [MenuItem("Tools/Crypt/Сюжет/2г. Гробниця і бос")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != DepthsScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(DepthsScene);
        }
        if (GameObject.Find("Tomb") != null) { Debug.LogWarning("[Depths] Гробниця вже є — нічого не змінюю."); return; }

        var ring = AssetDatabase.LoadAssetAtPath<ItemData>(Proj + "/Items/Item_Ring.asset");
        var enemies = GameObject.Find("Enemies");
        var gameplay = GameObject.Find("Gameplay");
        if (ring == null || enemies == null || gameplay == null)
        {
            Debug.LogError("[Depths] Спочатку запусти 2б і 2в (немає персня або Enemies).");
            return;
        }

        var bossPrefab = MakeBossPrefab();
        if (bossPrefab == null) return;

        var boss = SpawnBoss(bossPrefab, enemies.transform);
        BuildTomb(gameplay.transform, ring, boss);
        MakeNote(gameplay.transform, new Vector3(-2.4f, 0.08f, 41.6f));

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Depths] Готово: гробниця, бос (вимкнений до персня), записка 7. Сцену збережено.");
    }

    // ------------------------------------------------------------ Бос

    // Усі 5 моделей пакета вже використані, тому бос — модель Warrior, але більший, з іншим кольором і мечем
    private static GameObject MakeBossPrefab()
    {
        string prefabPath = Proj + "/Prefabs/Characters/Skeleton_Master.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        const float scale = 1.7f;
        string fbx = Skel + "Skeleton_Warrior.fbx";
        if (!PrepareModel(fbx, true)) { Debug.LogError("[Depths] Немає моделі " + fbx); return null; }
        var mat = MakeBossMaterial();
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Proj + "/Animations/Skeleton.controller");
        int enemyLayer = Mathf.Max(0, LayerMask.NameToLayer("Enemy"));

        var root = new GameObject("Skeleton_Master");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        model.transform.SetParent(root.transform, false);
        Paint(model, mat);

        // Повертаємо модель обличчям уздовж Z (так само, як у Вартового)
        Transform foot = FindDeep(model.transform, "foot.l"), toes = FindDeep(model.transform, "toes.l");
        if (foot != null && toes != null)
        {
            Vector3 f = toes.position - foot.position;
            f.y = 0f;
            if (f.sqrMagnitude > 0.0001f)
                model.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(f.normalized, Vector3.forward, Vector3.up), Vector3.up) * model.transform.rotation;
        }
        model.transform.localScale *= scale;
        float height = Mathf.Clamp(WorldBounds(model).max.y, 2.5f, 4.5f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.6f;
        agent.height = height;
        agent.angularSpeed = 480f;
        agent.acceleration = 14f;
        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, height / 2f, 0f);
        col.radius = 0.7f;
        col.height = height;
        root.AddComponent<Rigidbody>().isKinematic = true;

        // Баланс боса: 14 HP (у Вартового 8), шкода 2, повільніший за гравця, довгий замах — його удар можна прочитати
        var hp = root.AddComponent<Health>();
        var hso = new SerializedObject(hp);
        hso.FindProperty("maxHealth").intValue = 14;
        hso.FindProperty("invulnerabilityTime").floatValue = 0.3f;
        hso.ApplyModifiedPropertiesWithoutUndo();

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;

        var ai = root.AddComponent<EnemyAI>();
        var aso = new SerializedObject(ai);
        aso.FindProperty("animator").objectReferenceValue = animator;
        aso.FindProperty("viewRadius").floatValue = 25f;       // прокинувся — бачить усю залу
        aso.FindProperty("viewAngle").floatValue = 360f;
        aso.FindProperty("closeRadius").floatValue = 6f;
        aso.FindProperty("patrolSpeed").floatValue = 1.5f;
        aso.FindProperty("chaseSpeed").floatValue = 3.6f;
        aso.FindProperty("attackRange").floatValue = 2.8f;
        aso.FindProperty("attackCooldown").floatValue = 1.6f;
        aso.ApplyModifiedPropertiesWithoutUndo();

        var ec = root.AddComponent<EnemyCombat>();
        var eso = new SerializedObject(ec);
        eso.FindProperty("animator").objectReferenceValue = animator;
        eso.FindProperty("damage").intValue = 2;
        eso.FindProperty("hitDelay").floatValue = 0.7f;
        eso.FindProperty("hitRange").floatValue = 3.4f;
        eso.FindProperty("staggerTime").floatValue = 0.3f;
        eso.FindProperty("knockbackDistance").floatValue = 0.3f;
        eso.FindProperty("poiseTime").floatValue = 3f;
        eso.ApplyModifiedPropertiesWithoutUndo();

        var win = root.AddComponent<BossWin>();
        var wso = new SerializedObject(win);
        wso.FindProperty("finalText").stringValue = FinalText;
        wso.ApplyModifiedPropertiesWithoutUndo();

        AttachWeapon(model, "handslot.r", Skel + "Skeleton_Blade.fbx", "Blade", mat);

        // Тьмяне фіолетове світіння навколо боса
        var glow = new GameObject("Aura");
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0f, height * 0.6f, 0f);
        var l = glow.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.7f, 0.4f, 1f);
        l.intensity = 3f;
        l.range = 6f;
        l.shadows = LightShadows.None;

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = enemyLayer;
        model.name = "Model";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Material MakeBossMaterial()
    {
        string path = Proj + "/Materials/M_SkeletonBoss.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        var src = AssetDatabase.LoadAssetAtPath<Material>(Proj + "/Materials/M_Skeleton.mat");
        m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", new Color(0.6f, 0.45f, 0.85f));     // фіолетовий відтінок поверх текстури
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.12f, 0.05f, 0.2f));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static GameObject SpawnBoss(GameObject prefab, Transform enemies)
    {
        var route = new GameObject("Route_Skeleton_Master").transform;
        route.SetParent(enemies, false);
        Vector3[] points = { new Vector3(-2f, FloorY, 38.5f), new Vector3(2f, FloorY, 38.5f) };
        var wps = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var wp = new GameObject("WP_" + i).transform;
            wp.SetParent(route, false);
            wp.position = points[i];
            wps[i] = wp;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies);
        go.name = "Skeleton_Master";
        go.transform.SetPositionAndRotation(points[0], Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(go, "Бос");

        var so = new SerializedObject(go.GetComponent<EnemyAI>());
        var arr = so.FindProperty("waypoints");
        arr.arraySize = wps.Length;
        for (int i = 0; i < wps.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);        // прокинеться, коли гравець покладе перстень
        return go;
    }

    // ------------------------------------------------------------ Гробниця і записка

    private static void BuildTomb(Transform gameplay, ItemData ring, GameObject boss)
    {
        var root = new GameObject("Tomb");
        Undo.RegisterCreatedObjectUndo(root, "Гробниця");
        root.transform.SetParent(gameplay, false);
        root.transform.position = new Vector3(0f, 0f, 43.4f);

        var stone = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.45f, 0.45f, 0.5f) };
        var baseBox = Box(root.transform, "Base", new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.8f, 1.3f), stone);
        Object.DestroyImmediate(baseBox.GetComponent<Collider>());
        Box(root.transform, "Lid", new Vector3(0f, 0.9f, 0f), new Vector3(2.8f, 0.2f, 1.5f), stone);
        var slot = Box(root.transform, "RingSlot", new Vector3(0f, 1.02f, 0f), new Vector3(0.3f, 0.04f, 0.3f),
            new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.2f, 0.15f, 0.05f) });
        Object.DestroyImmediate(slot.GetComponent<Collider>());

        // Одна колайдер-коробка на весь саркофаг: через нього не пройти, а Interactor його бачить
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.5f, 0f);
        bc.size = new Vector3(2.8f, 1f, 1.5f);

        var tomb = root.AddComponent<Tomb>();
        var so = new SerializedObject(tomb);
        so.FindProperty("ringItem").objectReferenceValue = ring;
        so.FindProperty("boss").objectReferenceValue = boss;
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var cx in new[] { -1.2f, 1.2f })
        {
            var candle = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Props/candle_triple.prefab");
            if (candle == null) continue;
            var c = (GameObject)PrefabUtility.InstantiatePrefab(candle, root.transform);
            c.transform.localPosition = new Vector3(cx, 1.0f, 0f);
        }
    }

    private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static void MakeNote(Transform parent, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Записка 7");
        go.name = "Note_7";
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.35f, 0.04f, 0.25f);
        go.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
        go.GetComponent<BoxCollider>().isTrigger = true;
        go.GetComponent<MeshRenderer>().sharedMaterial =
            new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.85f, 0.78f, 0.55f) };

        var note = go.AddComponent<Note>();
        var so = new SerializedObject(note);
        so.FindProperty("title").stringValue = "Записка";
        so.FindProperty("body").stringValue = NoteText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------ Помічники (з Lab15Builder)

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
}
#endif
