#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одноразовий Builder: переробка стелі (без щілин, з балками, ланцюгами зі свічками й сталактитами),
// шахта-спуск у Crypt добудована до верху і накрита, у Depths сходи позаду старту замінені завалом,
// камера третьої особи трохи ближче. Стелю можна перестворити повторним запуском; прибрати — 4а-скасувати.
public static class Step5CeilingBuilder
{
    private const string Proj = "Assets/_Project";
    private static readonly string[] Scenes = { Proj + "/Scenes/Crypt.unity", Proj + "/Scenes/Depths.unity" };

    private static Material stone, wood, metal;

    [MenuItem("Tools/Crypt/Сюжет/5. Стеля 2.0, шахта, завал, камера")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        stone = Mat("M_Ceiling", new Color(0.2f, 0.17f, 0.15f));
        wood = Mat("M_CeilingWood", new Color(0.22f, 0.13f, 0.07f));
        metal = Mat("M_CeilingMetal", new Color(0.1f, 0.1f, 0.11f));

        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var env = GameObject.Find("Environment");
            if (env == null) { Debug.LogWarning($"[Стеля] {scene.name}: немає Environment."); continue; }

            // Стару стелю (з кроку 4а) прибираємо — будуємо заново
            var old = GameObject.Find("Ceiling");
            if (old != null) Object.DestroyImmediate(old);

            float wallH = WallHeight();
            float ceilY = LowestWallTop(env.transform) - 0.05f;     // нижче найнижчої стіни: щілин між стіною і стелею нема
            var root = new GameObject("Ceiling");
            Undo.RegisterCreatedObjectUndo(root, "Ceiling");

            int tiles = BuildCeiling(env.transform, root.transform, ceilY);
            if (scene.name == "Crypt") BuildShaftTop(root.transform, wallH, ceilY);
            if (scene.name == "Depths") ReplaceStairsWithRubble();
            SetCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Стеля] {scene.name}: плиток {tiles}, низ стелі y={ceilY:0.00}, висота стіни {wallH:0.00}.");
        }
    }

    // ------------------------------------------------------------ Стеля

    private static int BuildCeiling(Transform env, Transform root, float ceilY)
    {
        int n = 0;
        foreach (var t in env.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("floor")) continue;
            if (t.parent != null && t.parent.name.StartsWith("floor")) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Tile(root, new Vector3(b.center.x, ceilY, b.center.z), b.size.x, b.size.z);
            n++;
        }
        return n;
    }

    // Плитка стелі + прикраси: балка, у деяких — ланцюг зі свічками або сталактити (за позицією, без випадковості)
    private static void Tile(Transform root, Vector3 bottomCenter, float sx, float sz)
    {
        var tile = Cube(root, "CeilingTile", stone, bottomCenter + Vector3.up * 0.15f, new Vector3(sx + 0.02f, 0.3f, sz + 0.02f), true);
        GameObjectUtility.SetStaticEditorFlags(tile, 0);

        int ix = Mathf.RoundToInt(bottomCenter.x / 4f), iz = Mathf.RoundToInt(bottomCenter.z / 4f);
        int hash = Mathf.Abs(ix * 73856093 ^ iz * 19349663);

        if ((ix + iz) % 2 == 0)      // балка впоперек плитки
            Cube(root, "Beam", wood, bottomCenter + Vector3.down * 0.12f, new Vector3(sx, 0.24f, 0.35f), false);

        switch (hash % 5)
        {
            case 0:     // ланцюг зі свічками
                Cube(root, "Chain", metal, bottomCenter + Vector3.down * 0.55f, new Vector3(0.04f, 0.9f, 0.04f), false);
                var plate = Cylinder(root, "Plate", metal, bottomCenter + Vector3.down * 1.02f, new Vector3(0.7f, 0.03f, 0.7f));
                var candle = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Props/candle_triple.prefab");
                if (candle != null)
                {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(candle, root);
                    c.transform.position = plate.transform.position + Vector3.up * 0.02f;
                    c.transform.localScale = Vector3.one * 0.8f;
                }
                break;
            case 1:     // два сталактити
                Spike(root, bottomCenter + new Vector3(0.9f, 0f, -0.7f), 0.7f);
                Spike(root, bottomCenter + new Vector3(-0.6f, 0f, 0.8f), 0.45f);
                break;
        }
    }

    // Сталактит: витягнутий куб, поставлений на вершину (ромб, що звужується донизу)
    private static void Spike(Transform root, Vector3 top, float length)
    {
        var go = Cube(root, "Stalactite", stone, top + Vector3.down * length * 0.35f, new Vector3(0.22f, length, 0.22f), false);
        go.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        // Звужуємо до низу: другий, коротший і тонший куб під ним
        Cube(root, "StalactiteTip", stone, top + Vector3.down * length * 0.75f, new Vector3(0.1f, length * 0.5f, 0.1f), false).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
    }

    // ------------------------------------------------------------ Шахта-спуск у Crypt

    // Бічні стіни шахти добудовуються проміжними рядами, зверху — стеля над усією довжиною сходів
    private static void BuildShaftTop(Transform root, float wallH, float ceilY)
    {
        var stairs = GameObject.Find("Exit")?.transform.Find("stairs_narrow");
        var parent = GameObject.Find("Exit")?.transform.Find("Descent");
        if (stairs == null || parent == null) { Debug.LogWarning("[Стеля] Немає шахти (спочатку 3а)."); return; }

        var old = parent.Find("Fill");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var fill = new GameObject("Fill").transform;
        fill.SetParent(parent, false);

        Bounds b = Bounds(stairs.gameObject);
        float depth = -b.min.y;
        // Проміжні ряди між y=0 і y=-depth (крайні вже стоять з кроку 3а)
        for (float y = -wallH; y > -depth + 0.05f; y -= wallH)
        {
            for (float z = b.min.z + 2f; z < b.max.z + 1.5f; z += 4f)
                foreach (float x in new[] { -2.5f, 2.5f })
                    Wall(fill, new Vector3(x, y, z), 90f);
            Wall(fill, new Vector3(0f, y, b.max.z), 0f);
        }
        // Стіни біля брами на першому рядку (z=12) уже є; нижче них — теж потрібні, якщо глибина більша за висоту стіни
        for (float z = b.min.z + 2f; z < b.max.z; z += 4f)
            Tile(root, new Vector3(0f, ceilY, z), 5.2f, 4f);
    }

    // ------------------------------------------------------------ Depths: завал замість сходів

    private static void ReplaceStairsWithRubble()
    {
        foreach (var n in new[] { "Stairs_Up", "Stairs_Shaft" })
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }

        var hallA = GameObject.Find("Hall_A");
        if (hallA == null) return;
        var wall = hallA.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith("wall")
            && !(t.parent != null && t.parent.name.StartsWith("wall"))
            && Mathf.Abs(t.position.x) < 0.1f && Mathf.Abs(t.position.z + 2f) < 0.1f);
        var cracked = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Level/wall_cracked.prefab");
        var rubble = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Level/rubble_large.prefab");
        if (wall == null || cracked == null) { Debug.LogWarning("[Стеля] Не знайдено стіну (0, -2) або wall_cracked."); return; }

        if (!wall.name.StartsWith("wall_cracked"))
        {
            Transform p = wall.parent;
            Vector3 pos = wall.position; Quaternion rot = wall.rotation;
            Object.DestroyImmediate(wall.gameObject);
            var w = (GameObject)PrefabUtility.InstantiatePrefab(cracked, p);
            w.transform.SetPositionAndRotation(pos, rot);
        }
        if (rubble != null && GameObject.Find("Rubble_Entrance") == null)
        {
            var r = (GameObject)PrefabUtility.InstantiatePrefab(rubble, hallA.transform);
            r.name = "Rubble_Entrance";
            r.transform.position = new Vector3(0f, 0.05f, -1.2f);
        }
        Debug.Log("[Стеля] Depths: прохід до сходів завалено (wall_cracked + rubble_large), сходи прибрано.");
    }

    // ------------------------------------------------------------ Камера

    private static void SetCamera()
    {
        var cam = Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (cam == null) return;
        var so = new SerializedObject(cam);
        so.FindProperty("distance").floatValue = 5f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------ Помічники

    private static float WallHeight()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Level/wall.prefab");
        if (src == null) return 4f;
        var tmp = (GameObject)PrefabUtility.InstantiatePrefab(src);
        float h = Bounds(tmp).size.y;
        Object.DestroyImmediate(tmp);
        return h > 1f ? h : 4f;
    }

    // Найнижча верхня точка серед стін (арки й стовпи вищі — вони просто пройдуть крізь стелю)
    private static float LowestWallTop(Transform env)
    {
        float low = float.MaxValue;
        foreach (var t in env.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("wall")) continue;
            if (t.parent != null && t.parent.name.StartsWith("wall")) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            float top = rs.Max(r => r.bounds.max.y);
            if (top > 1.5f && top < low) low = top;      // відкидаємо пласкі напівстінки
        }
        return low == float.MaxValue ? 4f : low;
    }

    private static void Wall(Transform parent, Vector3 pos, float yaw)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Level/wall.prefab");
        if (src == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
    }

    private static GameObject Cube(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject Cylinder(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static Material Mat(string name, Color color)
    {
        string path = $"{Proj}/Materials/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
#endif
