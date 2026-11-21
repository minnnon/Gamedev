#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одноразовий Builder: величезна темна кам'яна плита під рівнем (Crypt і Depths), щоб замість чорної порожнечі навколо
// був камінь. Скасувати можна окремим пунктом меню — він просто видаляє об'єкт Bedrock.
public static class Step3RockBuilder
{
    private const string Proj = "Assets/_Project";
    private static readonly string[] Scenes = { Proj + "/Scenes/Crypt.unity", Proj + "/Scenes/Depths.unity" };

    [MenuItem("Tools/Crypt/Сюжет/3в. Скеля навколо (Crypt і Depths)")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var mat = GetMaterial();
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            if (GameObject.Find("Bedrock") != null) { Debug.Log($"[Скеля] {scene.name}: вже є."); continue; }

            var env = GameObject.Find("Environment");
            if (env == null) { Debug.LogWarning($"[Скеля] {scene.name}: немає Environment."); continue; }
            var rs = env.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);

            // Верх плити трохи нижче найнижчої точки рівня (у Crypt після 3а це низ сходів-спуску)
            float top = Mathf.Min(0f, b.min.y) - 0.05f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Bedrock");
            go.name = "Bedrock";
            Object.DestroyImmediate(go.GetComponent<Collider>());     // тільки вигляд, фізики не треба
            go.transform.localScale = new Vector3(b.size.x + 160f, 20f, b.size.z + 160f);
            go.transform.position = new Vector3(b.center.x, top - 10f, b.center.z);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, 0);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Скеля] {scene.name}: плиту додано, верх на y={top:0.00}.");
        }
    }

    [MenuItem("Tools/Crypt/Сюжет/3в-скасувати. Прибрати скелю")]
    private static void Remove()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var go = GameObject.Find("Bedrock");
            if (go == null) continue;
            Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Скеля] {scene.name}: плиту прибрано.");
        }
    }

    private static Material GetMaterial()
    {
        string path = Proj + "/Materials/M_Bedrock.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", new Color(0.13f, 0.11f, 0.1f));
        m.SetFloat("_Smoothness", 0f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
#endif
