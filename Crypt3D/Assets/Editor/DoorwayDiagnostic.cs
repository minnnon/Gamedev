#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Діагностика: що стоїть у дверному прорізі кімнати зі скелетами (Crypt, wall_doorway біля x=10, z=0).
// Друкує в Console усі колайдери в радіусі 3 м від прорізу і розміри CharacterController гравця.
public static class DoorwayDiagnostic
{
    [MenuItem("Tools/Crypt/Діагностика/Проріз кімнати зі скелетами")]
    private static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Crypt.unity");

        var sb = new StringBuilder("[Діагностика] Колайдери в радіусі 3 м від прорізу (10, 1.3, 0):\n");
        Vector3 center = new Vector3(10f, 1.3f, 0f);
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(c => (c.bounds.ClosestPoint(center) - center).sqrMagnitude < 9f)
                     .OrderBy(c => c.bounds.min.y))
        {
            string kind = c is MeshCollider mc ? $"Mesh(convex={mc.convex})" : c.GetType().Name;
            sb.AppendLine($"  {PathOf(c.transform)} | {kind} | trigger={c.isTrigger} enabled={c.enabled} | y {c.bounds.min.y:0.00}..{c.bounds.max.y:0.00}, x {c.bounds.min.x:0.0}..{c.bounds.max.x:0.0}, z {c.bounds.min.z:0.0}..{c.bounds.max.z:0.0}");
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        var cc = player != null ? player.GetComponent<CharacterController>() : null;
        if (cc != null) sb.AppendLine($"[Діагностика] Гравець: висота {cc.height:0.00}, радіус {cc.radius:0.00}, skin {cc.skinWidth:0.00}, step {cc.stepOffset:0.00}");
        Debug.Log(sb.ToString());
    }

    private static string PathOf(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif
