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
            if (GameObject.Find("Ceiling") != null) { Debug.Log($"[Стеля] {scene.name}: вже є."); continue; }
            var env = GameObject.Find("Environment");
            if (env == null) { Debug.LogWarning($"[Стеля] {scene.name}: немає Environment."); continue; }

            float wallTop = WallTop(env.transform);
            var root = new GameObject("Ceiling");
            Undo.RegisterCreatedObjectUndo(root, "Ceiling");

            int n = 0;
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.gameObject.name.StartsWith("floor")) continue;
                // Беремо весь префаб підлоги (плитка складається з одного меша; на всякий випадок ловимо верхній об'єкт)
                Bounds b = r.bounds;
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

    // Висота верху стін: найвищий об'єкт з іменем wall* (стовпи не рахуємо)
    private static float WallTop(Transform env)
    {
        float top = 0f;
        foreach (var r in env.GetComponentsInChildren<MeshRenderer>())
            if (r.gameObject.name.StartsWith("wall") && r.bounds.max.y > top) top = r.bounds.max.y;
        return top > 1f ? top : 4f;
    }

    // Відкритий прохід (wall_open_scaffold) на південній стіні зали A стає ґратами wall_gated
    private static void SwapStairsGate()
    {
        var hallA = GameObject.Find("Hall_A");
        var walls = hallA != null ? hallA.transform.Find("Walls") : null;
        if (walls == null) return;
        var open = walls.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("wall_open_scaffold")
            && Mathf.Abs(t.position.x) < 0.1f && Mathf.Abs(t.position.z + 2f) < 0.1f);
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Proj + "/Prefabs/Level/wall_gated.prefab");
        if (open == null || src == null) { Debug.LogWarning("[Стеля] Не знайдено прохід до сходів або wall_gated."); return; }

        Vector3 pos = open.position; Quaternion rot = open.rotation;
        Object.DestroyImmediate(open.gameObject);
        var gate = (GameObject)PrefabUtility.InstantiatePrefab(src, walls);
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
