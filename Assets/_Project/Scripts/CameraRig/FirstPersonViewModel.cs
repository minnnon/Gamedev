using UnityEngine;

// Меч і щит у руках від першої особи. Копії зброї з моделі лицаря чіпляються до камери,
// а рухи (замах, блок, хитання при ході) рахуються кодом. Також малює приціл, що червоніє при влученні.
public class FirstPersonViewModel : MonoBehaviour
{
    [Header("Розмір і позиції (відносно камери)")]
    [SerializeField] private float swordScale = 0.5f;
    [SerializeField] private float shieldScale = 0.7f;
    [SerializeField] private Vector3 swordRestPos = new Vector3(0.3f, -0.38f, 0.55f);
    [SerializeField] private Vector3 swordRestEuler = new Vector3(50f, 0f, 12f);
    [SerializeField] private Vector3 swordWindPos = new Vector3(0.4f, -0.1f, 0.4f);
    [SerializeField] private Vector3 swordWindEuler = new Vector3(-15f, 10f, 22f);
    [SerializeField] private Vector3 swordStrikePos = new Vector3(0.0f, -0.3f, 0.75f);
    [SerializeField] private Vector3 swordStrikeEuler = new Vector3(105f, -15f, -10f);
    [SerializeField] private Vector3 shieldRestPos = new Vector3(-0.42f, -0.32f, 0.65f);
    [SerializeField] private Vector3 shieldRestEuler = new Vector3(0f, 15f, 0f);
    [SerializeField] private Vector3 shieldBlockPos = new Vector3(-0.05f, -0.12f, 0.6f);
    [SerializeField] private Vector3 shieldBlockEuler = new Vector3(0f, 0f, 0f);
    [SerializeField] private bool flipShield;                  // якщо щит видно "зворотним боком"

    [Header("Анімація")]
    [SerializeField] private float attackDuration = 0.45f;
    [SerializeField] private float blockSmooth = 12f;

    private ThirdPersonCamera cam;
    private Camera unityCamera;
    private PlayerController controller;
    private PlayerCombat combat;
    private PlayerBlock block;
    private Health health;

    private GameObject root;
    private Transform sword, shield;
    private float attackT = -1f;
    private float blockK;
    private float bobT;
    private float kick;          // поштовх щита від заблокованого удару
    private float swordKick;     // віддача меча від влучання
    private float hitFlash;
    private float defaultNear;

    private void Start()
    {
        cam = GetComponent<ThirdPersonCamera>();
        unityCamera = GetComponent<Camera>();
        if (unityCamera != null) defaultNear = unityCamera.nearClipPlane;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) { enabled = false; return; }
        controller = p.GetComponent<PlayerController>();
        combat = p.GetComponent<PlayerCombat>();
        block = p.GetComponent<PlayerBlock>();
        health = p.GetComponent<Health>();

        root = new GameObject("ViewModel");
        root.transform.SetParent(transform, false);
        sword = Clone(p.transform, "Sword", false, swordScale);
        shield = Clone(p.transform, "Shield", true, shieldScale);
        root.SetActive(false);

        if (combat != null) { combat.Swung += OnSwung; combat.Landed += OnLanded; }
        if (block != null) block.BlockedHit += OnBlockedHit;
    }

    private void OnDestroy()
    {
        if (combat != null) { combat.Swung -= OnSwung; combat.Landed -= OnLanded; }
        if (block != null) block.BlockedHit -= OnBlockedHit;
    }

    private void OnSwung() { attackT = 0f; }
    private void OnLanded() { hitFlash = 0.2f; swordKick = 1f; }
    private void OnBlockedHit() { kick = 1f; }

    // Копіюємо зброю з моделі лицаря і вирівнюємо за розмірами: лезо вгору, лицьова сторона щита вперед
    private Transform Clone(Transform player, string name, bool flat, float scale)
    {
        Transform original = null;
        foreach (var t in player.GetComponentsInChildren<Transform>(true))
            if (t.name == name) { original = t; break; }
        if (original == null) { Debug.LogWarning("[FP] Не знайдено " + name + " на лицарі"); return null; }

        var holder = new GameObject(name + "_Holder").transform;
        holder.SetParent(root.transform, false);
        holder.localScale = Vector3.one * scale;

        var copy = Instantiate(original.gameObject, holder, false).transform;
        foreach (var c in copy.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var r in copy.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = true;
        }

        Bounds b = BoundsIn(holder, copy);
        Vector3 ext = b.extents;
        Vector3 origin = copy.localPosition;
        Vector3 offset = b.center - origin;

        if (!flat)
        {
            // Лезо — найдовша вісь; кінець леза там, де центр далі від початку моделі (руків'я)
            int axis = ext.x > ext.y ? (ext.x > ext.z ? 0 : 2) : (ext.y > ext.z ? 1 : 2);
            Vector3 dir = Vector3.zero;
            dir[axis] = offset[axis] >= 0f ? 1f : -1f;
            copy.localRotation = Quaternion.FromToRotation(dir, Vector3.up) * copy.localRotation;
            b = BoundsIn(holder, copy);

            // Плоска сторона леза дивиться вбік (товщина — вздовж X), тож лезо-ребро дивиться вперед:
            // тоді замах по X-осі рубає ребром, а не б'є плазом, як лопатою
            if (b.extents.x > b.extents.z)
                copy.localRotation = Quaternion.Euler(0f, 90f, 0f) * copy.localRotation;
            b = BoundsIn(holder, copy);
            copy.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);   // руків'я у початку координат
        }
        else
        {
            // Щит — тонка вісь це "товщина"; лицьова сторона вперед (від камери)
            int axis = ext.x < ext.y ? (ext.x < ext.z ? 0 : 2) : (ext.y < ext.z ? 1 : 2);
            Vector3 dir = Vector3.zero;
            dir[axis] = offset[axis] >= 0f ? 1f : -1f;
            copy.localRotation = Quaternion.FromToRotation(dir, Vector3.forward) * copy.localRotation;
            b = BoundsIn(holder, copy);
            if (b.extents.x > b.extents.y)
                copy.localRotation = Quaternion.Euler(0f, 0f, 90f) * copy.localRotation;
            if (flipShield) copy.localRotation = Quaternion.Euler(0f, 180f, 0f) * copy.localRotation;
            b = BoundsIn(holder, copy);
            copy.localPosition -= b.center;
        }
        return holder;
    }

    // Межі всіх мешів об'єкта в системі координат holder
    private static Bounds BoundsIn(Transform holder, Transform obj)
    {
        bool init = false;
        var result = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var mf in obj.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 p = holder.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (!init) { result = new Bounds(p, Vector3.zero); init = true; }
                else result.Encapsulate(p);
            }
        }
        return result;
    }

    private void LateUpdate()
    {
        bool active = cam != null && cam.FirstPerson && (health == null || !health.IsDead);
        if (root.activeSelf != active)
        {
            root.SetActive(active);
            if (unityCamera != null) unityCamera.nearClipPlane = active ? 0.05f : defaultNear;
        }
        if (!active) return;

        float dt = Time.deltaTime;
        bool blocking = block != null && block.IsBlocking;
        blockK = Mathf.MoveTowards(blockK, blocking ? 1f : 0f, blockSmooth * dt);
        kick = Mathf.MoveTowards(kick, 0f, 6f * dt);
        swordKick = Mathf.MoveTowards(swordKick, 0f, 5f * dt);
        hitFlash = Mathf.Max(0f, hitFlash - dt);

        // Хитання при ході
        float speed = controller != null ? controller.CurrentSpeed : 0f;
        float amp = Mathf.Clamp01(speed / 4f);
        bobT += dt * Mathf.Lerp(1.5f, 9f, Mathf.Clamp01(speed / 7f));
        Vector3 bob = new Vector3(Mathf.Cos(bobT * 0.5f) * 0.012f, Mathf.Abs(Mathf.Sin(bobT * 0.5f)) * 0.018f - 0.009f, 0f) * amp;
        bob += new Vector3(0f, Mathf.Sin(Time.time * 1.4f) * 0.004f, 0f);   // легке дихання

        // Меч: спокій -> замах -> удар -> повернення; під час блоку опускається вправо
        Vector3 sPos = swordRestPos;
        Quaternion sRot = Quaternion.Euler(swordRestEuler);
        if (attackT >= 0f)
        {
            attackT += dt / attackDuration;
            float a = attackT;
            if (a >= 1f) { attackT = -1f; }
            else if (a < 0.25f) Pose(swordRestPos, swordRestEuler, swordWindPos, swordWindEuler, Smooth(a / 0.25f), out sPos, out sRot);
            else if (a < 0.5f) Pose(swordWindPos, swordWindEuler, swordStrikePos, swordStrikeEuler, Smooth((a - 0.25f) / 0.25f), out sPos, out sRot);
            else Pose(swordStrikePos, swordStrikeEuler, swordRestPos, swordRestEuler, Smooth((a - 0.5f) / 0.5f), out sPos, out sRot);
        }
        sPos = Vector3.Lerp(sPos, swordRestPos + new Vector3(0.15f, -0.2f, -0.1f), blockK);
        sPos += new Vector3(0f, 0f, -0.08f) * swordKick;       // віддача при влучанні
        if (sword != null) sword.SetLocalPositionAndRotation(sPos + bob, sRot);

        // Щит: збоку -> перед обличчям; удар по щиту відкидає його назад
        Vector3 shPos = Vector3.Lerp(shieldRestPos, shieldBlockPos, blockK) + new Vector3(0f, 0f, -0.1f) * kick;
        Quaternion shRot = Quaternion.Slerp(Quaternion.Euler(shieldRestEuler), Quaternion.Euler(shieldBlockEuler), blockK)
                           * Quaternion.Euler(-8f * kick, 0f, 0f);
        if (shield != null) shield.SetLocalPositionAndRotation(shPos + bob, shRot);
    }

    private static float Smooth(float x) { return x * x * (3f - 2f * x); }

    private static void Pose(Vector3 p0, Vector3 e0, Vector3 p1, Vector3 e1, float k, out Vector3 p, out Quaternion r)
    {
        p = Vector3.Lerp(p0, p1, k);
        r = Quaternion.Slerp(Quaternion.Euler(e0), Quaternion.Euler(e1), k);
    }

    // Приціл у центрі; при влученні червоніє і росте — видно, що удар дійшов
    private void OnGUI()
    {
        if (root == null || !root.activeSelf) return;
        float size = hitFlash > 0f ? 12f : 4f;
        GUI.color = hitFlash > 0f ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 1f, 1f, 0.7f);
        GUI.DrawTexture(new Rect(Screen.width * 0.5f - size * 0.5f, Screen.height * 0.5f - size * 0.5f, size, size), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
