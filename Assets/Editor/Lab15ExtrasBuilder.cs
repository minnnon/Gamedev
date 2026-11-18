#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Одноразовий інструмент (скрипт редактора): блок щитом, баланс бою.
// Додає стан блоку в аніматор лицаря, PlayerBlock на гравця і підлаштовує значення в префабах.
public static class Lab15ExtrasBuilder
{
    private const string Proj = "Assets/_Project";
    private const string Anim = "Assets/ThirdParty/KayKit_Animations/Rig_Medium_CombatMelee.fbx";
    private const string ControllerPath = Proj + "/Animations/Knight.controller";

    [MenuItem("Tools/Crypt/Лаба 15/Доопрацювання: блок і баланс")]
    private static void Build()
    {
        var clip = ImportBlockClip();
        AddBlockState(clip);
        SetupPlayer();
        SetupEnemy(Proj + "/Prefabs/Characters/Skeleton.prefab", 0.6f, 0.6f, 2.0f);
        SetupEnemy(Proj + "/Prefabs/Characters/Skeleton_Guardian.prefab", 0.7f, 0.5f, 2.4f);
        AssetDatabase.SaveAssets();
        Debug.Log("[Lab15+] Готово: блок на ПКМ, V/C — перша особа, скелети повільніше б'ють і заціпеніють від ударів.");
    }

    private static AnimationClip ImportBlockClip()
    {
        var imp = AssetImporter.GetAtPath(Anim) as ModelImporter;
        if (imp == null) { Debug.LogError("[Lab15+] Немає файлу анімацій: " + Anim); return null; }

        var settings = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
        foreach (var c in settings)
            if (c.name == "Melee_Blocking") c.loopTime = true;
        imp.clipAnimations = settings;
        imp.SaveAndReimport();

        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Anim))
        {
            var clip = o as AnimationClip;
            if (clip != null && clip.name == "Melee_Blocking") return clip;
        }
        Debug.LogWarning("[Lab15+] Кліп Melee_Blocking не знайдено — блок працюватиме без анімації.");
        return null;
    }

    private static void AddBlockState(AnimationClip clip)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) { Debug.LogError("[Lab15+] Немає Knight.controller"); return; }

        bool hasParam = false;
        foreach (var p in ctrl.parameters) if (p.name == "Block") hasParam = true;
        if (!hasParam) ctrl.AddParameter("Block", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states)
            if (s.state.name == "Blocking") return;     // уже додано

        AnimatorState locomotion = sm.defaultState;
        var blocking = sm.AddState("Blocking", new Vector3(250, -240));
        blocking.motion = clip;

        // З будь-якого стану — у блок, поки затиснута ПКМ; відпустили — назад
        var t1 = sm.AddAnyStateTransition(blocking);
        t1.hasExitTime = false;
        t1.hasFixedDuration = true;
        t1.duration = 0.1f;
        t1.canTransitionToSelf = false;
        t1.AddCondition(AnimatorConditionMode.If, 0f, "Block");
        t1.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

        var t2 = blocking.AddTransition(locomotion);
        t2.hasExitTime = false;
        t2.hasFixedDuration = true;
        t2.duration = 0.1f;
        t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "Block");

        EditorUtility.SetDirty(ctrl);
    }

    private static void SetupPlayer()
    {
        string path = Proj + "/Prefabs/Characters/Player.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot;
            var block = root.GetComponent<PlayerBlock>();
            if (block == null) block = root.AddComponent<PlayerBlock>();

            var so = new SerializedObject(block);
            so.FindProperty("animator").objectReferenceValue = root.GetComponentInChildren<Animator>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var combat = root.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                var cso = new SerializedObject(combat);
                SetFloat(cso, "cooldown", 0.55f);
                SetFloat(cso, "hitDelay", 0.25f);
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void SetupEnemy(string path, float hitDelay, float stagger, float attackCooldown)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning("[Lab15+] Немає префаба: " + path);
            return;
        }
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot;
            var combat = root.GetComponent<EnemyCombat>();
            if (combat != null)
            {
                var so = new SerializedObject(combat);
                SetFloat(so, "hitDelay", hitDelay);
                SetFloat(so, "staggerTime", stagger);
                SetFloat(so, "knockbackDistance", 0.7f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var ai = root.GetComponent<EnemyAI>();
            if (ai != null)
            {
                var so = new SerializedObject(ai);
                SetFloat(so, "attackCooldown", attackCooldown);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void SetFloat(SerializedObject so, string name, float value)
    {
        var p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"[Lab15+] Немає поля {name} у {so.targetObject.GetType().Name}"); return; }
        p.floatValue = value;
    }
}
#endif
