#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Одноразовий інструмент лаби 16 (скрипт редактора):
// крок 1 — довідник предметів, вогнище-чекпоінт, зона виходу, Canvas з HUD і екранами в сцені Crypt;
// крок 2 — сцена MainMenu і налаштування Build Settings.
public static class Lab16Builder
{
    private const string Proj = "Assets/_Project";
    private const string CryptScene = Proj + "/Scenes/Crypt.unity";
    private const string MenuScene = Proj + "/Scenes/MainMenu.unity";

    private static readonly Color Gold = new Color(0.93f, 0.76f, 0.32f);
    private static readonly Color PanelDark = new Color(0.07f, 0.07f, 0.1f, 0.93f);
    private static readonly Color BarBack = new Color(0.12f, 0.05f, 0.05f, 1f);
    private static readonly Color HpRed = new Color(0.8f, 0.17f, 0.17f, 1f);

    private static Font font;

    // ------------------------------------------------------------------ КРОК 1

    [MenuItem("Tools/Crypt/Лаба 16/1. Інтерфейс, чекпоінт, вихід (сцена Crypt)")]
    private static void Step1()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != CryptScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(CryptScene);
        }
        if (GameObject.Find("UI") != null) { Debug.LogWarning("[Lab16] Інтерфейс уже створено — нічого не змінюю."); return; }

        var gameplay = GameObject.Find("Gameplay");
        if (gameplay == null) { Debug.LogError("[Lab16] Сцена не схожа на Crypt."); return; }

        Undo.SetCurrentGroupName("Лаба 16: інтерфейс");
        var report = new List<string>();

        BuildItemDatabase(report);
        SetupCheckpoint(gameplay.transform, report);
        SetupExit(gameplay.transform, report);
        BuildGameCanvas(report);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Lab16] Готово, сцену збережено:\n- " + string.Join("\n- ", report));
    }

    private static void BuildItemDatabase(List<string> report)
    {
        EnsureFolder(Proj + "/Resources");
        string path = Proj + "/Resources/ItemDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.items.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:ItemData"))
            db.items.Add(AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)));
        EditorUtility.SetDirty(db);
        report.Add($"Довідник предметів: {db.items.Count} шт. (Resources/ItemDatabase)");
    }

    private static void SetupCheckpoint(Transform gameplay, List<string> report)
    {
        var brazier = gameplay.Find("Checkpoint_Brazier");
        if (brazier == null) { report.Add("УВАГА: Checkpoint_Brazier не знайдено — чекпоінт не додано"); return; }
        if (brazier.GetComponent<Checkpoint>() == null) Undo.AddComponent<Checkpoint>(brazier.gameObject);
        report.Add("Вогнище Checkpoint_Brazier отримало Checkpoint (E — зберегти гру)");
    }

    private static void SetupExit(Transform gameplay, List<string> report)
    {
        if (gameplay.Find("ExitZone") != null) return;
        var go = new GameObject("ExitZone");
        Undo.RegisterCreatedObjectUndo(go, "ExitZone");
        go.transform.SetParent(gameplay, false);
        go.transform.position = new Vector3(0f, 3f, 14f);       // одразу за Рунною брамою (брама в z = 10)
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(5f, 8f, 5f);
        go.AddComponent<ExitZone>();
        report.Add("ExitZone за Рунною брамою — вхід у неї дає перемогу");
    }

    // ---------------------------------------------------------- Canvas гри

    private static void BuildGameCanvas(List<string> report)
    {
        EnsureEventSystem();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvas = MakeCanvas("UI", 10);
        var hud = canvas.gameObject.AddComponent<HUD>();
        var ui = canvas.gameObject.AddComponent<UIManager>();

        var hudRoot = NewRect("HUD", canvas.transform); Stretch(hudRoot);

        // Червоний спалах при пораненні
        var flash = Box(hudRoot, "DamageFlash", new Color(0.8f, 0.05f, 0.05f, 0f)); Stretch(flash.rectTransform);

        // Панель здоров'я й лічильників (зліва вгорі)
        var left = Box(hudRoot, "StatsPanel", new Color(0.05f, 0.05f, 0.08f, 0.75f));
        Anchor(left.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(440f, 170f));
        var barBack = Box(left.transform, "HpBack", BarBack);
        Anchor(barBack.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(310f, 30f));
        var fill = Box(barBack.transform, "HpFill", HpRed);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        var hpText = Label(left.transform, "HpText", "5/5", 26, TextAnchor.MiddleLeft, Color.white);
        Anchor(hpText.rectTransform, new Vector2(0f, 1f), new Vector2(340f, -20f), new Vector2(90f, 30f));
        var potionText = Label(left.transform, "PotionText", "Зілля: 0   (Q)", 26, TextAnchor.MiddleLeft, Color.white);
        Anchor(potionText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -66f), new Vector2(410f, 32f));
        var runeText = Label(left.transform, "RuneText", "Руни в брамі: 0/3", 26, TextAnchor.MiddleLeft, Gold);
        Anchor(runeText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -102f), new Vector2(410f, 32f));
        var coinText = Label(left.transform, "CoinText", "Монети: 0", 26, TextAnchor.MiddleLeft, Color.white);
        Anchor(coinText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -134f), new Vector2(410f, 32f));

        // Підказка "E" знизу й повідомлення зверху
        var prompt = Label(hudRoot, "Prompt", "", 36, TextAnchor.MiddleCenter, Color.white);
        Anchor(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1000f, 60f));
        AddOutline(prompt);
        var message = Label(hudRoot, "Message", "", 38, TextAnchor.MiddleCenter, Gold);
        Anchor(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1200f, 60f));
        AddOutline(message);

        // Вікно інвентаря (Tab)
        var inv = Box(hudRoot, "InventoryPanel", PanelDark);
        Anchor(inv.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 620f));
        var invTitle = Label(inv.transform, "Title", "ІНВЕНТАР", 44, TextAnchor.MiddleCenter, Gold);
        Anchor(invTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(500f, 60f));
        var icons = new Image[8]; var rows = new Text[8];
        for (int i = 0; i < 8; i++)
        {
            icons[i] = Box(inv.transform, "Icon_" + i, Color.white);
            Anchor(icons[i].rectTransform, new Vector2(0f, 1f), new Vector2(40f, -100f - i * 56f), new Vector2(36f, 36f));
            rows[i] = Label(inv.transform, "Row_" + i, "", 30, TextAnchor.MiddleLeft, Color.white);
            Anchor(rows[i].rectTransform, new Vector2(0f, 1f), new Vector2(96f, -98f - i * 56f), new Vector2(430f, 40f));
        }
        var invHint = Label(inv.transform, "Hint", "Q — випити зілля    G — викинути    Tab — закрити", 22, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f));
        Anchor(invHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(520f, 40f));

        // Пауза
        var pause = DimPanel(canvas.transform, "PausePanel", new Color(0f, 0f, 0f, 0.65f));
        var pbox = Box(pause.transform, "Box", PanelDark); Anchor(pbox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 760f));
        Title(pbox.transform, "ПАУЗА", Gold, 330f, 70);
        var bResume = MakeButton(pbox.transform, "Resume", "Продовжити", new Vector2(0f, 220f));
        var mLabel = Label(pbox.transform, "MusicLabel", "Музика", 28, TextAnchor.MiddleCenter, Color.white);
        Anchor(mLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(300f, 36f));
        var mSlider = MakeSlider(pbox.transform, "MusicSlider", new Vector2(0f, 80f));
        var sLabel = Label(pbox.transform, "SfxLabel", "Ефекти", 28, TextAnchor.MiddleCenter, Color.white);
        Anchor(sLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(300f, 36f));
        var sSlider = MakeSlider(pbox.transform, "SfxSlider", new Vector2(0f, -20f));
        var bMenu = MakeButton(pbox.transform, "ToMenu", "У головне меню", new Vector2(0f, -150f));
        var bQuit = MakeButton(pbox.transform, "Quit", "Вийти з гри", new Vector2(0f, -240f));
        var hintEsc = Label(pbox.transform, "Hint", "Esc — продовжити", 22, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f));
        Anchor(hintEsc.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(400f, 36f));
        pause.SetActive(false);

        // Смерть
        var over = DimPanel(canvas.transform, "GameOverPanel", new Color(0.18f, 0f, 0f, 0.8f));
        Title(over.transform, "ВИ ЗАГИНУЛИ", new Color(0.9f, 0.2f, 0.2f), 220f, 90);
        var bRetry = MakeButton(over.transform, "Retry", "Спробувати знову", new Vector2(0f, 50f));
        var bLoad = MakeButton(over.transform, "LoadLast", "Завантажити збереження", new Vector2(0f, -45f));
        var bMenu2 = MakeButton(over.transform, "ToMenu", "У головне меню", new Vector2(0f, -140f));
        over.SetActive(false);

        // Перемога
        var win = DimPanel(canvas.transform, "WinPanel", new Color(0f, 0.03f, 0.1f, 0.85f));
        Title(win.transform, "ПЕРЕМОГА!", Gold, 230f, 100);
        var sub = Label(win.transform, "Sub", "Лицар вибрався зі склепу.", 36, TextAnchor.MiddleCenter, Color.white);
        Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(900f, 50f));
        var stats = Label(win.transform, "Stats", "", 34, TextAnchor.MiddleCenter, Gold);
        Anchor(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(600f, 100f));
        var bMenu3 = MakeButton(win.transform, "ToMenu", "У головне меню", new Vector2(0f, -90f));
        win.SetActive(false);

        // Посилання на елементи
        Wire(hud, "hpFill", fill.rectTransform); Wire(hud, "hpText", hpText); Wire(hud, "damageFlash", flash);
        Wire(hud, "potionText", potionText); Wire(hud, "runeText", runeText); Wire(hud, "coinText", coinText);
        Wire(hud, "promptText", prompt); Wire(hud, "messageText", message); Wire(hud, "inventoryPanel", inv.gameObject);
        WireArray(hud, "rowIcons", icons); WireArray(hud, "rowTexts", rows);
        Wire(ui, "pausePanel", pause); Wire(ui, "gameOverPanel", over); Wire(ui, "winPanel", win);
        Wire(ui, "winStats", stats); Wire(ui, "loadButton", bLoad);
        Wire(ui, "musicSlider", mSlider); Wire(ui, "sfxSlider", sSlider);

        // Кнопки й повзунки -> методи UIManager
        UnityEventTools.AddPersistentListener(bResume.onClick, ui.Resume);
        UnityEventTools.AddPersistentListener(bMenu.onClick, ui.ToMenu);
        UnityEventTools.AddPersistentListener(bQuit.onClick, ui.QuitGame);
        UnityEventTools.AddPersistentListener(bRetry.onClick, ui.Retry);
        UnityEventTools.AddPersistentListener(bLoad.onClick, ui.LoadLast);
        UnityEventTools.AddPersistentListener(bMenu2.onClick, ui.ToMenu);
        UnityEventTools.AddPersistentListener(bMenu3.onClick, ui.ToMenu);
        UnityEventTools.AddPersistentListener(mSlider.onValueChanged, ui.OnMusicSlider);
        UnityEventTools.AddPersistentListener(sSlider.onValueChanged, ui.OnSfxSlider);

        report.Add("Canvas UI: HUD (HP, зілля, руни, монети, підказка E, повідомлення, інвентар Tab), пауза, смерть, перемога");
    }

    // ------------------------------------------------------------------ КРОК 2

    [MenuItem("Tools/Crypt/Лаба 16/2. Головне меню і Build Settings")]
    private static void Step2()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
        }
        EnsureEventSystem();

        var canvas = MakeCanvas("Canvas", 0);
        var menu = canvas.gameObject.AddComponent<MainMenu>();

        var bg = Box(canvas.transform, "Background", new Color(0.04f, 0.04f, 0.07f, 1f)); Stretch(bg.rectTransform);
        var title = Label(canvas.transform, "Title", "СКЛЕП ТРЬОХ РУН", 120, TextAnchor.MiddleCenter, Gold);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1500f, 150f));
        AddOutline(title);
        var sub = Label(canvas.transform, "Subtitle", "Лицар, старий склеп і три рунні ключі", 38, TextAnchor.MiddleCenter, new Color(0.75f, 0.75f, 0.82f));
        Anchor(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(1200f, 60f));

        var bNew = MakeButton(canvas.transform, "NewGame", "Нова гра", new Vector2(0f, 120f));
        var bCont = MakeButton(canvas.transform, "Continue", "Продовжити", new Vector2(0f, 30f));
        var info = Label(canvas.transform, "SaveInfo", "", 24, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f));
        Anchor(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(600f, 34f));
        var bQuit = MakeButton(canvas.transform, "Quit", "Вийти", new Vector2(0f, -110f));

        var mLabel = Label(canvas.transform, "MusicLabel", "Музика", 28, TextAnchor.MiddleCenter, Color.white);
        Anchor(mLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-260f, -250f), new Vector2(300f, 36f));
        var mSlider = MakeSlider(canvas.transform, "MusicSlider", new Vector2(-260f, -290f));
        var sLabel = Label(canvas.transform, "SfxLabel", "Ефекти", 28, TextAnchor.MiddleCenter, Color.white);
        Anchor(sLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(260f, -250f), new Vector2(300f, 36f));
        var sSlider = MakeSlider(canvas.transform, "SfxSlider", new Vector2(260f, -290f));

        var help = Label(canvas.transform, "Help", "WASD — рух   Миша — камера   ЛКМ — удар   ПКМ — щит   E — взаємодія   V — вид від першої особи   Esc — пауза", 24, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.68f));
        Anchor(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1800f, 40f));

        Wire(menu, "continueButton", bCont); Wire(menu, "saveInfo", info);
        Wire(menu, "musicSlider", mSlider); Wire(menu, "sfxSlider", sSlider);
        UnityEventTools.AddPersistentListener(bNew.onClick, menu.NewGame);
        UnityEventTools.AddPersistentListener(bCont.onClick, menu.Continue);
        UnityEventTools.AddPersistentListener(bQuit.onClick, menu.Quit);
        UnityEventTools.AddPersistentListener(mSlider.onValueChanged, menu.OnMusicSlider);
        UnityEventTools.AddPersistentListener(sSlider.onValueChanged, menu.OnSfxSlider);

        EditorSceneManager.SaveScene(scene, MenuScene);

        // Build Settings: меню — сцена 0, гра — сцена 1
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(CryptScene, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[Lab16] Готово: сцена MainMenu створена, Build Settings: 0 — MainMenu, 1 — Crypt. Відкрий MainMenu і натисни Play.");
    }

    // ------------------------------------------------------------- допоміжні

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        es.AddComponent<EventSystem>();
        var module = es.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    private static Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    // Якір і точка опори в одному куті/центрі, позиція й розмір у пікселях Canvas
    private static void Anchor(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = anchor; r.anchorMax = anchor; r.pivot = anchor;
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    private static Image Box(Transform parent, string name, Color color)
    {
        var r = NewRect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text Label(Transform parent, string name, string text, int size, TextAnchor align, Color color)
    {
        var r = NewRect(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void AddOutline(Text t)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        o.effectDistance = new Vector2(2f, -2f);
    }

    // Затемнений екран на весь Canvas (блокує кліки під собою)
    private static GameObject DimPanel(Transform parent, string name, Color color)
    {
        var img = Box(parent, name, color);
        Stretch(img.rectTransform);
        img.raycastTarget = true;
        return img.gameObject;
    }

    private static void Title(Transform parent, string text, Color color, float y, int size)
    {
        var t = Label(parent, "Title", text, size, TextAnchor.MiddleCenter, color);
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1200f, size * 1.5f));
        AddOutline(t);
    }

    private static DefaultControls.Resources UiResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = DefaultControls.CreateButton(UiResources());
        go.name = name;
        go.transform.SetParent(parent, false);
        Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, new Vector2(460f, 70f));

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.17f, 0.15f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.38f, 0.3f, 0.14f, 1f);
        colors.pressedColor = new Color(0.6f, 0.45f, 0.16f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.14f, 0.7f);
        btn.colors = colors;

        var text = go.GetComponentInChildren<Text>();
        text.font = font; text.text = label; text.fontSize = 32; text.color = Color.white;
        return btn;
    }

    private static Slider MakeSlider(Transform parent, string name, Vector2 pos)
    {
        var go = DefaultControls.CreateSlider(UiResources());
        go.name = name;
        go.transform.SetParent(parent, false);
        Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, new Vector2(360f, 28f));

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.7f;
        go.transform.Find("Background").GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);
        go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = Gold;
        return slider;
    }

    // Підключає серіалізоване поле компонента до об'єкта
    private static void Wire(Component target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Lab16] Немає поля {field} у {target.GetType().Name}"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Wire(Component target, string field, GameObject value) { Wire(target, field, (Object)value); }

    private static void WireArray(Component target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Lab16] Немає поля {field} у {target.GetType().Name}"); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
