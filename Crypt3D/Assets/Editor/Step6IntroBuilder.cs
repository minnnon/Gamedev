#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Одноразовий Builder кроку 6: три вступні кадри (малюються кодом), панель IntroUI у Crypt
// і покращений екран перемоги (епілог, час, спроби, монети, «Нова гра») у Crypt і Depths.
public static class Step6IntroBuilder
{
    private const string Proj = "Assets/_Project";
    private const string ArtDir = Proj + "/Art/Intro";
    private const int W = 960, H = 540;

    private static readonly string[] Captions =
    {
        "Під нашим селом є старий склеп.",
        "Тиждень тому зник сторож Гнат, а ночами біля цвинтаря бачать кістяків.",
        "Староста заплатив, щоб я спустився і розібрався. Гроші вперед. Назад — як вийде."
    };

    [MenuItem("Tools/Crypt/Сюжет/6. Вступні кадри і екран перемоги")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var sprites = MakeArt();

        var crypt = EditorSceneManager.OpenScene(Proj + "/Scenes/Crypt.unity");
        BuildIntroPanel(sprites);
        UpgradeWinPanel();
        EditorSceneManager.MarkSceneDirty(crypt);
        EditorSceneManager.SaveScene(crypt);

        var depths = EditorSceneManager.OpenScene(Proj + "/Scenes/Depths.unity");
        UpgradeWinPanel();
        EditorSceneManager.MarkSceneDirty(depths);
        EditorSceneManager.SaveScene(depths);

        Debug.Log("[Сюжет] Готово: вступ (3 кадри) у Crypt, екран перемоги оновлено в Crypt і Depths.");
    }

    // ------------------------------------------------------------ Панель вступу

    private static void BuildIntroPanel(Sprite[] sprites)
    {
        var canvas = GameObject.Find("UI");
        if (canvas == null) { Debug.LogError("[Сюжет] Немає Canvas 'UI'."); return; }
        var old = canvas.transform.Find("IntroUI");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Хост (завжди активний, тримає скрипт) і дочірня панель із візуалом, яку скрипт вмикає й вимикає
        var host = new GameObject("IntroUI", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(host, "IntroUI");
        host.transform.SetParent(canvas.transform, false);
        Stretch(host.GetComponent<RectTransform>());

        var root = new GameObject("Panel", typeof(RectTransform));
        root.transform.SetParent(host.transform, false);
        Stretch(root.GetComponent<RectTransform>());
        var bg = root.AddComponent<Image>();
        bg.color = Color.black;                 // чорний фон, блокує кліки

        var slide = new GameObject("Slide", typeof(RectTransform), typeof(CanvasGroup));
        slide.transform.SetParent(root.transform, false);
        Stretch(slide.GetComponent<RectTransform>());

        var pic = new GameObject("Picture", typeof(RectTransform), typeof(Image));
        pic.transform.SetParent(slide.transform, false);
        var pr = pic.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0f, 0.18f); pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
        var img = pic.GetComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;

        var cap = MakeText(slide.transform, font, "Caption", 38, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f));
        cap.horizontalOverflow = HorizontalWrapMode.Wrap;
        Anchor(cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1500f, 120f));

        var hint = MakeText(slide.transform, font, "Hint", 26, TextAnchor.MiddleCenter, new Color(0.65f, 0.65f, 0.65f));
        Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1200f, 50f));

        var ui = host.AddComponent<IntroUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("root").objectReferenceValue = root;
        so.FindProperty("slide").objectReferenceValue = slide.GetComponent<CanvasGroup>();
        so.FindProperty("picture").objectReferenceValue = img;
        so.FindProperty("caption").objectReferenceValue = cap;
        so.FindProperty("hint").objectReferenceValue = hint;
        var sp = so.FindProperty("sprites"); sp.arraySize = sprites.Length;
        var cp = so.FindProperty("captions"); cp.arraySize = Captions.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            sp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            cp.GetArrayElementAtIndex(i).stringValue = Captions[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // Поверх HUD і записок, але під затемненням переходу (воно в NoteUI, що вище)
        host.transform.SetAsLastSibling();
        var note = canvas.transform.Find("NoteUI");
        if (note != null) note.SetAsLastSibling();
    }

    // ------------------------------------------------------------ Екран перемоги

    private static void UpgradeWinPanel()
    {
        var ui = Object.FindFirstObjectByType<UIManager>();
        if (ui == null) { Debug.LogError("[Сюжет] Немає UIManager."); return; }
        var so = new SerializedObject(ui);
        var panel = so.FindProperty("winPanel").objectReferenceValue as GameObject;
        var stats = so.FindProperty("winStats").objectReferenceValue as Text;
        if (panel == null || stats == null) { Debug.LogError("[Сюжет] У UIManager не задано winPanel/winStats."); return; }

        var texts = panel.GetComponentsInChildren<Text>(true).Where(t => t != stats && t.GetComponentInParent<Button>() == null).ToList();
        var title = texts.OrderByDescending(t => t.fontSize).FirstOrDefault();
        var subtitle = texts.Where(t => t != title).FirstOrDefault();
        if (subtitle != null) subtitle.text = "Хазяїн гробниці переможений. Склеп затих.";

        var buttons = panel.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0) { Debug.LogError("[Сюжет] У панелі перемоги немає кнопки."); return; }
        if (buttons.Length >= 2) { Debug.Log("[Сюжет] Екран перемоги вже оновлено."); return; }

        var menuBtn = buttons[0];
        var rt = (RectTransform)menuBtn.transform;
        var clone = Object.Instantiate(menuBtn.gameObject, menuBtn.transform.parent);
        clone.name = "NewGameButton";
        var crt = (RectTransform)clone.transform;

        // Дві кнопки поруч, трохи нижче (у статистиці тепер три рядки)
        float y = rt.anchoredPosition.y - 40f;
        rt.sizeDelta = new Vector2(Mathf.Min(rt.sizeDelta.x, 400f), rt.sizeDelta.y);
        crt.sizeDelta = rt.sizeDelta;
        rt.anchoredPosition = new Vector2(-(rt.sizeDelta.x / 2f + 15f), y);
        crt.anchoredPosition = new Vector2(rt.sizeDelta.x / 2f + 15f, y);
        stats.rectTransform.anchoredPosition += new Vector2(0f, -15f);

        var label = clone.GetComponentInChildren<Text>();
        if (label != null) label.text = "Нова гра";
        var btn = clone.GetComponent<Button>();
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(btn.onClick, i);
        UnityEventTools.AddPersistentListener(btn.onClick, ui.NewGame);

        Debug.Log("[Сюжет] Екран перемоги: епілог, 3 рядки статистики, кнопки «Нова гра» і «У головне меню».");
    }

    // ------------------------------------------------------------ Малювання кадрів

    private static Sprite[] MakeArt()
    {
        Directory.CreateDirectory(ArtDir);
        string[] files = { "intro_1_village.png", "intro_2_graveyard.png", "intro_3_crypt.png" };
        var bufs = new[] { PaintVillage(), PaintGraveyard(), PaintCrypt() };
        var result = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            string path = $"{ArtDir}/{files[i]}";
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(bufs[i].Finish());
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return result;
    }

    // Невеликий "малярський" клас: усе в пікселях, початок координат — лівий нижній кут
    private class Painter
    {
        private readonly Color[] px = new Color[W * H];
        private readonly System.Random rnd;
        public Painter(int seed) { rnd = new System.Random(seed); }
        public float R() { return (float)rnd.NextDouble(); }

        public void Gradient(Color top, Color bottom)
        {
            for (int y = 0; y < H; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(H - 1));
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
        }

        public void Rect(float x0, float y0, float x1, float y1, Color c)
        {
            for (int y = Mathf.Max(0, (int)y0); y < Mathf.Min(H, (int)y1); y++)
                for (int x = Mathf.Max(0, (int)x0); x < Mathf.Min(W, (int)x1); x++) Blend(x, y, c, 1f);
        }

        public void Disc(float cx, float cy, float r, Color c)
        {
            for (int y = Mathf.Max(0, (int)(cy - r - 1)); y < Mathf.Min(H, (int)(cy + r + 2)); y++)
                for (int x = Mathf.Max(0, (int)(cx - r - 1)); x < Mathf.Min(W, (int)(cx + r + 2)); x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (a > 0f) Blend(x, y, c, a);
                }
        }

        public void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int x0 = Mathf.Max(0, (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.Min(W - 1, (int)Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int y0 = Mathf.Max(0, (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y))), y1 = Mathf.Min(H - 1, (int)Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos)) Blend(x, y, col, 1f);
                }
        }

        // М'яке світіння (додається до кольору)
        public void Glow(float cx, float cy, float r, Color c, float k)
        {
            for (int y = Mathf.Max(0, (int)(cy - r)); y < Mathf.Min(H, (int)(cy + r)); y++)
                for (int x = Mathf.Max(0, (int)(cx - r)); x < Mathf.Min(W, (int)(cx + r)); x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d >= 1f) continue;
                    float a = (1f - d) * (1f - d) * k;
                    var p = px[y * W + x];
                    px[y * W + x] = new Color(p.r + c.r * a, p.g + c.g * a, p.b + c.b * a, 1f);
                }
        }

        // Туман: горизонтальна смуга, що розмивається вгору і вниз
        public void Fog(float yCenter, float height, Color c, float k)
        {
            for (int y = Mathf.Max(0, (int)(yCenter - height)); y < Mathf.Min(H, (int)(yCenter + height)); y++)
            {
                float a = (1f - Mathf.Abs(y - yCenter) / height) * k;
                for (int x = 0; x < W; x++)
                {
                    float wob = 0.75f + 0.25f * Mathf.Sin(x * 0.011f + yCenter) * Mathf.Sin(x * 0.027f + 1.7f);
                    Blend(x, y, c, a * wob);
                }
            }
        }

        // Хвиляста лінія горизонту: усе нижче заливається кольором
        public void Hills(float baseY, float amp, float freq, float phase, Color c)
        {
            for (int x = 0; x < W; x++)
            {
                float h = baseY + amp * (Mathf.Sin(x * freq + phase) * 0.6f + Mathf.Sin(x * freq * 2.3f + phase * 1.7f) * 0.4f);
                for (int y = 0; y < Mathf.Min(H, (int)h); y++) px[y * W + x] = c;
            }
        }

        public void Stars(int count, float yMin)
        {
            for (int i = 0; i < count; i++)
            {
                int x = (int)(R() * W), y = (int)(yMin + R() * (H - yMin));
                float b = 0.4f + R() * 0.6f;
                Blend(x, y, new Color(b, b, b * 0.9f), 1f);
                if (R() > 0.85f) { Blend(x + 1, y, new Color(b, b, b), 0.5f); Blend(x, y + 1, new Color(b, b, b), 0.5f); }
            }
        }

        public Color[] Finish()
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - W / 2f) / (W / 2f), dy = (y - H / 2f) / (H / 2f);
                    float v = 1f - Mathf.Clamp01((dx * dx + dy * dy - 0.35f) * 0.6f);     // віньєтка
                    float n = 1f + (R() - 0.5f) * 0.05f;                                     // легке зерно
                    var p = px[y * W + x];
                    px[y * W + x] = new Color(Mathf.Clamp01(p.r * v * n), Mathf.Clamp01(p.g * v * n), Mathf.Clamp01(p.b * v * n), 1f);
                }
            return px;
        }

        private void Blend(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            var p = px[y * W + x];
            px[y * W + x] = new Color(Mathf.Lerp(p.r, c.r, a), Mathf.Lerp(p.g, c.g, a), Mathf.Lerp(p.b, c.b, a), 1f);
        }

        private static float Sign(Vector2 p, Vector2 a, Vector2 b) { return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y); }
    }

    // Кадр 1: нічне село під горою, вікна світяться
    private static Painter PaintVillage()
    {
        var p = new Painter(1);
        p.Gradient(new Color(0.03f, 0.04f, 0.12f), new Color(0.16f, 0.14f, 0.28f));
        p.Stars(260, 200);
        p.Glow(740, 430, 170, new Color(0.5f, 0.55f, 0.8f), 0.55f);
        p.Disc(740, 430, 42, new Color(0.93f, 0.93f, 0.85f));
        p.Hills(190, 40, 0.006f, 0.5f, new Color(0.08f, 0.09f, 0.18f));
        p.Hills(130, 30, 0.009f, 2.0f, new Color(0.05f, 0.06f, 0.12f));

        // Хати: стіна, дах, вікно зі світлом
        float[] xs = { 90, 230, 400, 560, 700 };
        for (int i = 0; i < xs.Length; i++)
        {
            float x = xs[i], w = 80 + (i % 3) * 18, h = 55 + (i % 2) * 20, y = 85 + (i % 3) * 8;
            p.Rect(x, y, x + w, y + h, new Color(0.07f, 0.06f, 0.1f));
            p.Tri(new Vector2(x - 10, y + h), new Vector2(x + w + 10, y + h), new Vector2(x + w / 2f, y + h + 42), new Color(0.04f, 0.03f, 0.07f));
            float wx = x + w * 0.3f, wy = y + h * 0.35f;
            p.Glow(wx + 8, wy + 8, 55, new Color(1f, 0.7f, 0.25f), 0.7f);
            p.Rect(wx, wy, wx + 16, wy + 16, new Color(1f, 0.82f, 0.4f));
        }
        // Ялинки
        for (int i = 0; i < 14; i++)
        {
            float x = 20 + i * 70 + p.R() * 30, y = 40 + p.R() * 50, s = 40 + p.R() * 35;
            var dark = new Color(0.02f, 0.03f, 0.05f);
            p.Tri(new Vector2(x - s * 0.3f, y), new Vector2(x + s * 0.3f, y), new Vector2(x, y + s * 1.1f), dark);
            p.Tri(new Vector2(x - s * 0.4f, y + s * 0.3f), new Vector2(x + s * 0.4f, y + s * 0.3f), new Vector2(x, y + s * 1.5f), dark);
        }
        p.Fog(120, 45, new Color(0.35f, 0.38f, 0.55f), 0.25f);
        return p;
    }

    // Кадр 2: цвинтар, у тумані — силуети кістяків із жовтими очима
    private static Painter PaintGraveyard()
    {
        var p = new Painter(2);
        p.Gradient(new Color(0.04f, 0.07f, 0.09f), new Color(0.2f, 0.26f, 0.27f));
        p.Stars(120, 300);
        p.Glow(250, 420, 200, new Color(0.45f, 0.6f, 0.6f), 0.5f);
        p.Disc(250, 420, 36, new Color(0.85f, 0.9f, 0.85f));
        p.Hills(150, 25, 0.007f, 1.0f, new Color(0.04f, 0.06f, 0.07f));

        // Надгробки двома рядами
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < 10; i++)
            {
                float x = 40 + i * 95 + row * 45 + p.R() * 25, y = 70 + row * 45 + p.R() * 10;
                float w = 34 + p.R() * 14, h = 50 + p.R() * 30;
                var c = row == 0 ? new Color(0.02f, 0.03f, 0.04f) : new Color(0.06f, 0.08f, 0.09f);
                p.Rect(x, y, x + w, y + h, c);
                p.Disc(x + w / 2f, y + h, w / 2f, c);
            }
        // Силуети кістяків
        float[] sx = { 380, 560, 700 };
        for (int i = 0; i < sx.Length; i++)
        {
            float x = sx[i], y = 100 + i * 12, s = 1f - i * 0.12f;
            var bone = new Color(0.03f, 0.04f, 0.05f);
            p.Rect(x - 4 * s, y + 20 * s, x + 4 * s, y + 90 * s, bone);                       // хребет
            for (int r = 0; r < 4; r++) p.Rect(x - 22 * s, y + (45 + r * 11) * s, x + 22 * s, y + (49 + r * 11) * s, bone);   // ребра
            p.Rect(x - 12 * s, y, x - 7 * s, y + 25 * s, bone); p.Rect(x + 7 * s, y, x + 12 * s, y + 25 * s, bone);          // ноги
            p.Rect(x - 36 * s, y + 70 * s, x - 22 * s, y + 76 * s, bone); p.Rect(x + 22 * s, y + 70 * s, x + 36 * s, y + 76 * s, bone); // руки
            p.Disc(x, y + 108 * s, 17 * s, bone);                                             // череп
            var eye = new Color(1f, 0.8f, 0.25f);
            p.Glow(x - 6 * s, y + 110 * s, 22 * s, eye, 0.9f); p.Glow(x + 6 * s, y + 110 * s, 22 * s, eye, 0.9f);
            p.Disc(x - 6 * s, y + 110 * s, 2.4f * s, eye); p.Disc(x + 6 * s, y + 110 * s, 2.4f * s, eye);
        }
        p.Fog(120, 70, new Color(0.45f, 0.55f, 0.55f), 0.45f);
        p.Fog(70, 40, new Color(0.35f, 0.45f, 0.45f), 0.4f);
        return p;
    }

    // Кадр 3: вхід у склеп — арка, сходи вниз, смолоскипи і маленька постать із ліхтарем
    private static Painter PaintCrypt()
    {
        var p = new Painter(3);
        p.Gradient(new Color(0.07f, 0.06f, 0.07f), new Color(0.15f, 0.12f, 0.12f));
        // Кам'яна стіна: цеглини
        for (int row = 0; row < 14; row++)
            for (int i = 0; i < 16; i++)
            {
                float x = i * 64 - (row % 2) * 32 + p.R() * 2, y = row * 40 + 5;
                float g = 0.1f + p.R() * 0.07f;
                p.Rect(x + 2, y + 2, x + 62, y + 38, new Color(g * 1.05f, g, g * 0.95f));
            }
        // Арка: темний отвір із півколом зверху
        float cx = 480, bottom = 60, aw = 140, ah = 230;
        var inner = new Color(0.005f, 0.005f, 0.01f);
        var frame = new Color(0.2f, 0.18f, 0.17f);
        p.Rect(cx - aw - 18, bottom, cx + aw + 18, bottom + ah, frame);
        p.Disc(cx, bottom + ah, aw + 18, frame);
        p.Rect(cx - aw, bottom, cx + aw, bottom + ah, inner);
        p.Disc(cx, bottom + ah, aw, inner);
        // Сходи вниз у темряву
        for (int s = 0; s < 6; s++)
        {
            float y = bottom + s * 9, w = aw - s * 6;
            float g = 0.12f - s * 0.015f;
            p.Rect(cx - w, y, cx + w, y + 6, new Color(g, g * 0.9f, g * 0.85f));
        }
        p.Glow(cx, bottom + 120, 200, new Color(0.05f, 0.1f, 0.2f), 0.5f);
        // Смолоскипи
        foreach (float tx in new[] { 250f, 710f })
        {
            p.Rect(tx - 3, 190, tx + 3, 270, new Color(0.08f, 0.05f, 0.03f));
            p.Glow(tx, 285, 190, new Color(1f, 0.55f, 0.15f), 0.9f);
            p.Disc(tx, 285, 11, new Color(1f, 0.75f, 0.3f));
            p.Disc(tx, 292, 6, new Color(1f, 0.95f, 0.7f));
        }
        // Марко: силует спиною до глядача, плащ, шолом, ліхтар
        float mx = 480, my = 30;
        var dark = new Color(0.01f, 0.01f, 0.02f);
        p.Tri(new Vector2(mx - 28, my), new Vector2(mx + 28, my), new Vector2(mx, my + 85), dark);       // плащ
        p.Rect(mx - 14, my + 60, mx + 14, my + 95, dark);                                                 // плечі
        p.Disc(mx, my + 108, 14, dark);                                                                   // шолом
        p.Tri(new Vector2(mx - 6, my + 118), new Vector2(mx + 6, my + 118), new Vector2(mx, my + 132), dark);
        p.Rect(mx + 20, my + 52, mx + 24, my + 74, dark);                                                 // рука
        p.Glow(mx + 22, my + 45, 110, new Color(1f, 0.7f, 0.3f), 0.6f);                                   // ліхтар
        p.Disc(mx + 22, my + 45, 7, new Color(1f, 0.85f, 0.45f));
        p.Fog(50, 40, new Color(0.3f, 0.3f, 0.33f), 0.3f);
        return p;
    }

    // ------------------------------------------------------------ UI-помічники

    private static Text MakeText(Transform parent, Font font, string name, int size, TextAnchor align, Color c)
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

    private static void Anchor(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }
}
#endif
