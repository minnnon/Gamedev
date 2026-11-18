#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Одноразовий інструмент (скрипт редактора): друга порція балансу бою.
public static class Lab15BalanceBuilder
{
    private const string Proj = "Assets/_Project";

    [MenuItem("Tools/Crypt/Лаба 15/Баланс 2: HP скелета і темп удару")]
    private static void Build()
    {
        SetHealth(Proj + "/Prefabs/Characters/Skeleton.prefab", 4);

        string player = Proj + "/Prefabs/Characters/Player.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(player))
        {
            var combat = scope.prefabContentsRoot.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                var so = new SerializedObject(combat);
                so.FindProperty("cooldown").floatValue = 0.7f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Lab15+] Скелет: 4 HP; пауза між ударами лицаря 0.7 с.");
    }

    private static void SetHealth(string path, int hp)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning("[Lab15+] Немає " + path); return; }
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var health = scope.prefabContentsRoot.GetComponent<Health>();
            if (health == null) return;
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").intValue = hp;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
