#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одноразовий Builder кроку 3: сходи, що мають сенс. У Crypt — спуск вниз за брамою (замість сходів «вгору в нікуди»),
// у Depths — сходи позаду старту, ніби гравець спустився з Crypt.
public static class Step3StairsBuilder
{
    private const string Proj = "Assets/_Project";
    private const string CryptScene = Proj + "/Scenes/Crypt.unity";
    private const string DepthsScene = Proj + "/Scenes/Depths.unity";

    // ------------------------------------------------------------ Crypt: спуск

    [MenuItem("Tools/Crypt/Сюжет/3а. Спуск у Crypt")]
    private static void BuildCryptDescent()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(CryptScene);

        var exit = GameObject.Find("Exit");
        var stairs = exit != null ? exit.transform.Find("stairs_narrow") : null;
        var zone = GameObject.Find("ExitZone");
        var box = zone != null ? zone.GetComponent<BoxCollider>() : null;
        if (stairs == null || box == null) { Debug.LogError("[Сюжет] Не знайдено Exit/stairs_narrow або ExitZone."); return; }
        if (exit.transform.Find("Descent") != null) { Debug.LogWarning("[Сюжет] Спуск уже зроблено."); return; }

        Bounds b = WorldBounds(stairs.gameObject);
        float h = b.size.y;
        Undo.RecordObject(stairs, "Спуск");
        stairs.RotateAround(b.center, Vector3.up, 180f);     // тепер високий кінець біля брами
        stairs.position += Vector3.down * h;                 // верхня сходинка на рівні підлоги

        b = WorldBounds(stairs.gameObject);
        var parent = new GameObject("Descent").transform;
        Undo.RegisterCreatedObjectUndo(parent.gameObject, "Descent");
        parent.SetParent(exit.transform, false);
        BuildShaft(parent, b, h);

        // Зона виходу — у самій шахті, коли гравець уже спустився
        Vector3 worldCenter = new Vector3(0f, (-h - 1f + 2f) / 2f, (b.min.z + 1f + b.max.z) / 2f);
        Undo.RecordObject(box, "ExitZone");
        box.center = zone.transform.InverseTransformPoint(worldCenter);
        box.size = new Vector3(4f, h + 3f, Mathf.Max(1f, b.size.z - 1f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Сюжет] Спуск у Crypt готовий: висота сходів {h:0.00}, зона виходу в шахті. Сцену збережено.");
    }

    // ------------------------------------------------------------ Depths: сходи позаду старту

    [MenuItem("Tools/Crypt/Сюжет/3б. Сходи в Depths")]
    private static void BuildDepthsStairs()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(DepthsScene);
        var hallA = GameObject.Find("Hall_A");
        if (hallA == null) { Debug.LogError("[Сюжет] Немає Hall_A у Depths."); return; }
        if (GameObject.Find("Stairs_Up") != null) { Debug.LogWarning("[Сюжет] Сходи вже є."); return; }

        // Південна стіна зали A (x=0, z=-2) стає відкритим проходом
        var walls = hallA.transform.Find("Walls");
        Transform oldWall = walls.Cast<Transform>().FirstOrDefault(t => Mathf.Abs(t.position.x) < 0.1f && Mathf.Abs(t.position.z + 2f) < 0.1f);
        if (oldWall != null) Object.DestroyImmediate(oldWall.gameObject);
        var open = Spawn("wall_open_scaffold", new Vector3(0f, 0f, -2f), 0f, walls);
        open.name = "wall_open_scaffold";

        var stairs = Spawn("stairs_narrow", Vector3.zero, 0f, null);
        stairs.name = "Stairs_Up";
        Bounds b = WorldBounds(stairs);
        // Сходи піднімаються на південь (-z) від проходу: ближній до зали кінець — біля z=-2
        stairs.transform.position += new Vector3(-b.center.x, 0f, -2f - b.max.z);
        b = WorldBounds(stairs);
        stairs.transform.SetParent(hallA.transform.parent, true);

        var shaft = new GameObject("Stairs_Shaft").transform;
        Undo.RegisterCreatedObjectUndo(shaft.gameObject, "Stairs");
        shaft.SetParent(hallA.transform.parent, false);
        BuildShaft(shaft, b, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Сюжет] Сходи в Depths додано позаду старту. Сцену збережено.");
    }

    // ------------------------------------------------------------ Спільне

    // Бокові стіни вздовж сходів і торцева стіна. dropY — на скільки сходи опущені (стіни ставимо і на землі, і нижче)
    private static void BuildShaft(Transform parent, Bounds b, float dropY)
    {
        bool lowered = dropY > 0.01f;
        int rows = lowered ? 2 : 1;
        for (int r = 0; r < rows; r++)
        {
            float y = -dropY * r;
            // Бокові стіни: від початку сходів до кінця (існуючі біля брами вже стоять на першому рядку)
            for (float z = b.min.z + 2f; z < b.max.z + 1.5f; z += 4f)
                foreach (float x in new[] { -2.5f, 2.5f })
                {
                    if (r == 0 && lowered && Mathf.Abs(z - 12f) < 0.5f) continue;
                    Spawn("wall", new Vector3(x, y, z), 90f, parent);
                }
            Spawn("wall", new Vector3(0f, y, b.max.z), 0f, parent);     // торець
        }
    }

    private static GameObject Spawn(string prefabName, Vector3 pos, float yaw, Transform parent)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Proj}/Prefabs/Level/{prefabName}.prefab");
        if (src == null) { Debug.LogError("[Сюжет] Немає префаба " + prefabName); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        Undo.RegisterCreatedObjectUndo(go, prefabName);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return go;
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
