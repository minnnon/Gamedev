#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Одноразовий Builder: панель записок/вступу в Canvas UI + записки 1–5 і табличка на рунній брамі в сцені Crypt.
public static class Step1StoryBuilder
{
    private const string CryptScene = "Assets/_Project/Scenes/Crypt.unity";

    private static readonly string[] Titles = { "Записка", "Записка", "Записка", "Записка", "Записка", "Напис на брамі" };
    private static readonly string[] Texts =
    {
        "Хто б ти не був — вертайся нагору. Я серйозно.\n\n— Гнат",
        "Осінь. Якісь розумники розкопали нижню гробницю. Щось винесли. Відтоді мерці не лежать. Чудово. Саме цього мені на старість і бракувало.",
        "Пастки ще мій дід ставив. Наступай тільки туди, де пил не зачеплений. Ну або не наступай. Мені вже однаково.",
        "Нижню браму я закрив на три руни. Розніс їх по склепу, щоб жоден дурень не поліз униз. Якщо ти їх збираєш — то ти саме той дурень.",
        "Біля брами стоїть перший сторож. Він тут стояв ще до мене. Не знаю, чи він ще пам'ятає, кого має стерегти. Але б'є він так само.",
        "Три руни — одні двері. Хто відчинить, той і відповідає."
    };

    [MenuItem("Tools/Crypt/Сюжет/1. Записки і вступ")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != CryptScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CryptScene);
        }

        if (!BuildPanel()) return;
        BuildNotes();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[Сюжет] Готово: панель NoteUI і записки створено, сцену збережено.");
    }

    // ------------------------------------------------------------ Панель

    private static bool BuildPanel()
    {
        var canvas = GameObject.Find("UI");
        if (canvas == null) { Debug.LogError("[Сюжет] Немає Canvas 'UI' у сцені."); return false; }
        var old = canvas.transform.Find("NoteUI");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("NoteUI", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "NoteUI");
        root.transform.SetParent(canvas.transform, false);
        Stretch(root.GetComponent<RectTransform>());

        // Затемнення на весь екран
        var panel = Img(root.transform, "Panel", new Color(0f, 0f, 0f, 0.8f));
        Stretch(panel.rectTransform);

        // Тло записки по центру
        var paper = Img(panel.transform, "Paper", new Color(0.16f, 0.12f, 0.08f, 0.95f));
        paper.rectTransform.anchorMin = paper.rectTransform.anchorMax = paper.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        paper.rectTransform.anchoredPosition = Vector2.zero;
        paper.rectTransform.sizeDelta = new Vector2(1000f, 560f);

        var title = Txt(paper.transform, font, "Title", 40, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.4f));
        Fixed(title.rectTransform, new Vector2(0f, 230f), new Vector2(900f, 70f));

        var body = Txt(paper.transform, font, "Body", 34, TextAnchor.MiddleLeft, new Color(0.95f, 0.9f, 0.8f));
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        Fixed(body.rectTransform, new Vector2(0f, 5f), new Vector2(880f, 360f));

        var hint = Txt(paper.transform, font, "Hint", 26, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
        Fixed(hint.rectTransform, new Vector2(0f, -235f), new Vector2(900f, 50f));

        var ui = root.AddComponent<NoteUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.transform.SetAsLastSibling();      // поверх HUD
        return true;
    }

    private static Image Img(Transform parent, string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private static Text Txt(Transform parent, Font font, string name, int size, TextAnchor align, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = align; t.color = c; t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    // Елемент фіксованого розміру відносно центру панелі
    private static void Fixed(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    // ------------------------------------------------------------ Записки

    private static void BuildNotes()
    {
        var oldRoot = GameObject.Find("Notes");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);
        var root = new GameObject("Notes");
        Undo.RegisterCreatedObjectUndo(root, "Notes");

        var start = GameObject.Find("PlayerStart");
        var gate = Object.FindFirstObjectByType<RuneGate>();
        var skeleton = GameObject.Find("Skeleton_Corridor");
        var trap = Object.FindFirstObjectByType<SpikeTrap>();
        Pickup rune = null;
        foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            if (p.Item != null && p.Item.type == ItemType.Rune) { rune = p; break; }

        Vector3 origin = start != null ? start.transform.position : Vector3.zero;
        Vector3 fwd = start != null ? start.transform.forward : Vector3.forward;

        // Кожну записку ставимо біля потрібного об'єкта, зсунувши в бік входу (щоб лежала на проході)
        PlaceNote(root, 0, origin + fwd * 2.5f, origin, 0f, "біля входу");
        PlaceNote(root, 1, skeleton != null ? skeleton.transform.position : origin, origin, 3.5f, "перед скелетом у коридорі");
        PlaceNote(root, 2, trap != null ? trap.transform.position : origin, origin, 2.5f, "біля пастки");
        PlaceNote(root, 3, rune != null ? rune.transform.position : origin, origin, 1.5f, "біля руни");
        PlaceNote(root, 4, gate != null ? gate.transform.position : origin, origin, 5f, "перед рунною брамою");
        PlaceNote(root, 5, gate != null ? gate.transform.position : origin, origin, 2f, "табличка біля брами");
    }

    private static void PlaceNote(GameObject parent, int index, Vector3 anchor, Vector3 towards, float back, string where)
    {
        Vector3 dir = towards - anchor; dir.y = 0f;
        Vector3 want = anchor + (dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.back) * back;

        // Ставимо на навмеш (прохідна підлога), потім опускаємо на землю
        if (NavMesh.SamplePosition(want, out var hit, 4f, NavMesh.AllAreas)) want = hit.position;
        if (Physics.Raycast(want + Vector3.up * 2f, Vector3.down, out var ground, 5f, ~0, QueryTriggerInteraction.Ignore))
            want = ground.point;

        bool tablet = index == 5;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Note");
        go.name = tablet ? "Tablet_Gate" : "Note_" + (index + 1);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = want + Vector3.up * (tablet ? 0.5f : 0.03f);
        go.transform.localScale = tablet ? new Vector3(0.6f, 1f, 0.12f) : new Vector3(0.35f, 0.04f, 0.25f);
        go.transform.rotation = Quaternion.Euler(0f, index * 37f, 0f);

        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;                       // не заважає ходити, але Interactor її бачить
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = tablet ? new Color(0.35f, 0.33f, 0.3f) : new Color(0.85f, 0.78f, 0.55f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var note = go.AddComponent<Note>();
        var so = new SerializedObject(note);
        so.FindProperty("title").stringValue = Titles[index];
        so.FindProperty("body").stringValue = Texts[index];
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[Сюжет] {go.name} — {where}: {go.transform.position}");
    }
}
#endif
