#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одноразовий Builder: стеля над кожною плиткою підлоги в Crypt і Depths (на висоті верху стін)
// і ґрати замість відкритого проходу до сходів позаду старту в Depths.
// Скасувати стелю: Tools/Crypt/Сюжет/4а-скасувати.
public static class Step4CeilingBuilder
{
    private const string Proj = "Assets/_Project";
    private static readonly string[] Scenes = { Proj + "/Scenes/Crypt.unity", Proj + "/Scenes/Depths.unity" };

    [MenuItem("Tools/Crypt/Сюжет/4а. Стеля і ґрати до сходів")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var mat = GetMaterial();

        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var oldCeiling = GameObject.Find("Ceiling");
            if (oldCeiling != null && oldCeiling.transform.childCount > 0) { Debug.Log($"[Стеля] {scene.name}: вже є."); continue; }
            if (oldCeiling != null) Object.DestroyImmediate(oldCeiling);     // порожній після невдалого запуску
            var env = GameObject.Find("Environment");
            if (env == null) { Debug.LogWarning($"[Стеля] {scene.name}: немає Environment."); continue; }

            float wallTop = WallTop(env.transform);
            var root = new GameObject("Ceiling");
            Undo.RegisterCreatedObjectUndo(root, "Ceiling");

            int n = 0;
            foreach (var t in env.GetComponentsInChildren<Transform>())
            {
                if (!t.name.StartsWith("floor")) continue;
                if (t.parent != null && t.parent.name.StartsWith("floor")) continue;    // лише корінь префаба
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);

                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "CeilingTile";
                tile.transform.SetParent(root.transform, false);
                tile.transform.position = new Vector3(b.center.x, wallTop + 0.15f, b.center.z);
                tile.transform.localScale = new Vector3(b.size.x + 0.02f, 0.3f, b.size.z + 0.02f);
                tile.GetComponent<Renderer>().sharedMaterial = mat;
                GameObjectUtility.SetStaticEditorFlags(tile, 0);
                n++;
            }

            if (scene.name == "Depths") SwapStairsGate();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Стеля] {scene.name}: плиток стелі {n}, висота низу стелі y={wallTop:0.00}.");
        }
    }

    [MenuItem("Tools/Crypt/Сюжет/4а-скасувати. Прибрати стелю")]
    private static void Remove()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var go = GameObject.Find("Ceiling");
            if (go == null) continue;
            Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Стеля] {scene.name}: стелю прибрано.");
        }
    }

    // Висота верху стін: медіана по кореневих об'єктах wall* (стовпи й арки не спотворюють результат)
    private static float WallTop(Transform env)
    {
        var tops = new System.Collections.Generic.List<float>();
        foreach (var t in env.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("wall")) continue;
            if (t.parent != null && t.parent.name.StartsWith("wall")) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            float top = rs[0].bounds.max.y;
            foreach (var r in rs) top = Mathf.Max(top, r.bounds.max.y);
            tops.Add(top);
        }
        if (tops.Count == 0) return 4f;
        tops.Sort();
        return tops[tops.Count / 2];
    }

    // Прохід до сходів (будь-яка стіна на x=0, z=-2 у залі A) стає ґратами wall_gated — лише якщо сходи є
    private static void SwapStairsGate()
    {
        if (GameObject.Find("Stairs_Up") == null) { Debug.Log("[Стеля] Сходів позаду старту немає — ґрати не потрібні."); return; }
        var hallA = GameObject.Find("Hall_A");
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Level/wall_gated.prefab");
        if (hallA == null || src == null) { Debug.LogWarning("[Стеля] Немає Hall_A або wall_gated."); return; }

        var wall = hallA.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith("wall")
            && !(t.parent != null && t.parent.name.StartsWith("wall"))
            && Mathf.Abs(t.position.x) < 0.1f && Mathf.Abs(t.position.z + 2f) < 0.1f);
        if (wall == null) { Debug.LogWarning("[Стеля] Не знайдено стіну на вході в сходи (0, -2)."); return; }
        if (wall.name.StartsWith("wall_gated")) { Debug.Log("[Стеля] Ґрати вже стоять."); return; }

        Transform parent = wall.parent;
        Vector3 pos = wall.position; Quaternion rot = wall.rotation;
        Object.DestroyImmediate(wall.gameObject);
        var gate = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        gate.transform.SetPositionAndRotation(pos, rot);
        Debug.Log("[Стеля] Прохід до сходів у Depths закрито ґратами.");
    }

    private static Material GetMaterial()
    {
        string path = Proj + "/Materials/M_Ceiling.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", new Color(0.2f, 0.17f, 0.15f));
        m.SetFloat("_Smoothness", 0f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
#endif
