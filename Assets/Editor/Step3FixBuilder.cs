#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Одноразовий Builder: виправлення сходів кроку 3 і чорний екран (fade) для переходу між рівнями.
//  - Crypt: зона виходу тільки на дні шахти (а не з першої сходинки);
//  - Depths: торцева стіна сходів на дальньому кінці (вона закривала вхід у сходи);
//  - в обох сценах: Image «Fade» у NoteUI.
public static class Step3FixBuilder
{
    private const string Proj = "Assets/_Project";

    [MenuItem("Tools/Crypt/Сюжет/3г. Виправити сходи і додати затемнення")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var crypt = EditorSceneManager.OpenScene(Proj + "/Scenes/Crypt.unity");
        FixCryptZone();
        AddFade();
        EditorSceneManager.MarkSceneDirty(crypt);
        EditorSceneManager.SaveScene(crypt);

        var depths = EditorSceneManager.OpenScene(Proj + "/Scenes/Depths.unity");
        FixDepthsEndWall();
        AddFade();
        EditorSceneManager.MarkSceneDirty(depths);
        EditorSceneManager.SaveScene(depths);

        Debug.Log("[Сюжет] Готово: сходи виправлено, затемнення додано в Crypt і Depths.");
    }

    // Зона виходу — нижня третина сходів-спуску: гравець справді спускається, а не зникає на першій сходинці
    private static void FixCryptZone()
    {
        var stairs = GameObject.Find("Exit")?.transform.Find("stairs_narrow");
        var zone = GameObject.Find("ExitZone");
        var box = zone != null ? zone.GetComponent<BoxCollider>() : null;
        if (stairs == null || box == null) { Debug.LogError("[Сюжет] Немає Exit/stairs_narrow або ExitZone."); return; }

        Bounds b = WorldBounds(stairs.gameObject);
        float len = b.size.z * 0.35f;
        var center = new Vector3(0f, b.min.y + 1f, b.max.z - len / 2f);
        Undo.RecordObject(box, "ExitZone");
        box.center = zone.transform.InverseTransformPoint(center);
        box.size = new Vector3(4f, 4f, len);
        Debug.Log($"[Сюжет] Зона виходу Crypt: низ сходів y={b.min.y:0.0}, довжина зони {len:0.0}.");
    }

    // Торцева стіна, що перекривала вхід у сходи на z≈-2, переїжджає на дальній (південний) кінець сходів
    private static void FixDepthsEndWall()
    {
        var stairs = GameObject.Find("Stairs_Up");
        var shaft = GameObject.Find("Stairs_Shaft");
        if (stairs == null || shaft == null) { Debug.LogWarning("[Сюжет] Немає Stairs_Up або Stairs_Shaft у Depths."); return; }

        Bounds b = WorldBounds(stairs);
        var blocking = shaft.transform.Cast<Transform>().FirstOrDefault(t =>
            Mathf.Abs(t.position.x) < 0.1f && Mathf.Abs(t.position.z - b.max.z) < 0.6f && Mathf.Abs(t.eulerAngles.y) < 1f);
        if (blocking != null)
        {
            Undo.RecordObject(blocking, "Стіна сходів");
            blocking.position = new Vector3(0f, 0f, b.min.z);
            Debug.Log("[Сюжет] Стіну, що закривала вхід у сходи, перенесено на дальній кінець.");
        }
        else Debug.Log("[Сюжет] Стіни на вході в сходи не знайдено (можливо, вже виправлено).");
    }

    private static void AddFade()
    {
        var ui = Object.FindFirstObjectByType<NoteUI>(FindObjectsInactive.Include);
        if (ui == null) { Debug.LogError("[Сюжет] Немає NoteUI у сцені."); return; }
        var existing = ui.transform.Find("Fade");
        if (existing != null) { Debug.Log("[Сюжет] Fade вже є."); return; }

        var go = new GameObject("Fade", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Fade");
        go.transform.SetParent(ui.transform, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        go.transform.SetAsLastSibling();

        var so = new SerializedObject(ui);
        so.FindProperty("fadeImage").objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();
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
