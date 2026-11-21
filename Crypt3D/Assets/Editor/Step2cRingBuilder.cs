#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одноразовий Builder кроку 2в: предмет «Перстень», кістки грабіжника і записка 6 у залі B сцени Depths.
public static class Step2cRingBuilder
{
    private const string Proj = "Assets/_Project";
    private const string DepthsScene = Proj + "/Scenes/Depths.unity";

    private const string NoteText = "Взяв перстень. Тільки перстень, решта — мотлох. Хлопці побігли нагору, а я загубив дорогу. Чую кроки. Це не хлопці.";

    [MenuItem("Tools/Crypt/Сюжет/2в. Перстень, кістки, записка 6")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != DepthsScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(DepthsScene);
        }
        if (GameObject.Find("RobberBones") != null) { Debug.LogWarning("[Depths] Кістки вже є — нічого не змінюю."); return; }

        // 1. Предмет, значок на землі, запис у довіднику
        var ring = MakeItem();
        var gold = AssetDatabase.LoadAssetAtPath<Material>(Proj + "/Materials/M_Gold.mat");
        var ringPickup = MakeRingPickup(ring, gold);
        ring.pickupPrefab = ringPickup;
        EditorUtility.SetDirty(ring);
        AddToDatabase(ring);

        // 2. Кістки, перстень і записка в західному кутку зали B (там ходить Skeleton_B_West)
        var bones = new GameObject("RobberBones");
        Undo.RegisterCreatedObjectUndo(bones, "Кістки");
        bones.transform.position = new Vector3(-4.2f, 0.05f, 23.2f);
        BuildBones(bones.transform);

        var pickup = (GameObject)PrefabUtility.InstantiatePrefab(ringPickup);
        Undo.RegisterCreatedObjectUndo(pickup, "Перстень");
        pickup.name = "Ring_Pickup";
        pickup.transform.SetParent(bones.transform, false);
        pickup.transform.localPosition = new Vector3(0.7f, 0f, -0.4f);

        MakeNote(bones.transform, new Vector3(-0.9f, 0f, 0.5f));

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Depths] Готово: кістки, перстень і записка 6 у залі B, сцену збережено.");
    }

    // ------------------------------------------------------------ Предмет

    private static ItemData MakeItem()
    {
        string path = Proj + "/Items/Item_Ring.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<ItemData>();
        item.displayName = "Перстень";
        item.type = ItemType.Relic;
        item.description = "Золотий перстень із гробниці. Його варто повернути.";
        item.color = new Color(1f, 0.82f, 0.25f);
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static GameObject MakeRingPickup(ItemData item, Material gold)
    {
        string path = Proj + "/Prefabs/Items/Pickup_Ring.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("Pickup_Ring");
        var sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.7f;
        sc.center = new Vector3(0f, 0.5f, 0f);
        var p = root.AddComponent<Pickup>();
        var so = new SerializedObject(p);
        so.FindProperty("item").objectReferenceValue = item;
        so.FindProperty("amount").intValue = 1;
        so.FindProperty("spinSpeed").floatValue = 90f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Перстень: тор з циліндрів нема, тому — товстий золотий диск на ребрі
        var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.name = "Ring";
        Object.DestroyImmediate(disk.GetComponent<Collider>());
        disk.transform.SetParent(root.transform, false);
        disk.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disk.transform.localScale = new Vector3(0.28f, 0.03f, 0.28f);
        if (gold != null) disk.GetComponent<Renderer>().sharedMaterial = gold;

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        var l = glow.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.8f, 0.3f);
        l.intensity = 2f;
        l.range = 3.5f;
        l.shadows = LightShadows.None;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AddToDatabase(ItemData item)
    {
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(Proj + "/Resources/ItemDatabase.asset");
        if (db == null) { Debug.LogError("[Depths] Немає ItemDatabase."); return; }
        if (db.items.Contains(item)) return;
        Undo.RecordObject(db, "ItemDatabase");
        db.items.Add(item);
        EditorUtility.SetDirty(db);
    }

    // ------------------------------------------------------------ Кістки і записка

    private static void BuildBones(Transform parent)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.82f, 0.78f, 0.68f) };
        // Кілька "кісток" (капсули) і череп (сфера) купкою
        Bone(parent, mat, PrimitiveType.Capsule, new Vector3(0f, 0.08f, 0f), new Vector3(90f, 20f, 0f), new Vector3(0.1f, 0.4f, 0.1f));
        Bone(parent, mat, PrimitiveType.Capsule, new Vector3(0.25f, 0.08f, 0.15f), new Vector3(90f, 110f, 0f), new Vector3(0.09f, 0.35f, 0.09f));
        Bone(parent, mat, PrimitiveType.Capsule, new Vector3(-0.3f, 0.08f, -0.1f), new Vector3(90f, 70f, 0f), new Vector3(0.09f, 0.3f, 0.09f));
        Bone(parent, mat, PrimitiveType.Capsule, new Vector3(0.1f, 0.08f, -0.35f), new Vector3(90f, 160f, 0f), new Vector3(0.08f, 0.3f, 0.08f));
        Bone(parent, mat, PrimitiveType.Sphere, new Vector3(-0.15f, 0.15f, 0.35f), Vector3.zero, new Vector3(0.28f, 0.3f, 0.3f));
    }

    private static void Bone(Transform parent, Material mat, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = type == PrimitiveType.Sphere ? "Skull" : "Bone";
        Object.DestroyImmediate(go.GetComponent<Collider>());      // кістки не заважають ходити
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void MakeNote(Transform parent, Vector3 localPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Записка 6");
        go.name = "Note_6";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos + Vector3.up * 0.03f;
        go.transform.localScale = new Vector3(0.35f, 0.04f, 0.25f);
        go.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.85f, 0.78f, 0.55f) };
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var note = go.AddComponent<Note>();
        var so = new SerializedObject(note);
        so.FindProperty("title").stringValue = "Записка";
        so.FindProperty("body").stringValue = NoteText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
