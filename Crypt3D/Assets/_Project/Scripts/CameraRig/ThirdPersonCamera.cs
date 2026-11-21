using UnityEngine;
using UnityEngine.InputSystem;

// Камера: від третьої особи (орбіта навколо гравця) або від першої (V чи C).
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Ціль")]
    [SerializeField] private Transform target;
    [SerializeField] private float targetHeight = 1.9f;

    [Header("Орбіта")]
    [SerializeField] private float distance = 6.5f;
    [SerializeField] private float sensitivity = 0.15f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 65f;

    [Header("Перша особа")]
    [SerializeField] private float eyeHeight = 1.7f;
    [SerializeField] private float eyeForward = 0.15f;
    [SerializeField] private float firstPersonMinPitch = -80f;
    [SerializeField] private float firstPersonMaxPitch = 80f;

    [Header("Зіткнення зі стінами")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Ввід")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction lookAction;
    private float yaw;
    private float pitch = 20f;
    private bool firstPerson;
    private bool appliedFirstPerson;
    private Renderer[] bodyRenderers;
    private PlayerController playerController;
    private Health targetHealth;

    public bool FirstPerson => firstPerson;

    private void Awake()
    {
        lookAction = inputActions.FindActionMap("Player", true).FindAction("Look", true);
    }

    private void OnEnable()
    {
        inputActions.FindActionMap("Player", true).Enable();
    }

    private void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            bodyRenderers = target.GetComponentsInChildren<Renderer>();
            playerController = target.GetComponent<PlayerController>();
            targetHealth = target.GetComponent<Health>();
        }

        // Зброя в руках для першої особи (створюється сама, нічого в сцені міняти не треба)
        if (GetComponent<FirstPersonViewModel>() == null) gameObject.AddComponent<FirstPersonViewModel>();
    }

    // LateUpdate — щоб камера рухалась після того, як гравець уже перемістився в Update
    private void LateUpdate()
    {
        if (target == null) return;

        // Курсором і паузою (Esc) керує GameManager; тут лише повертаємо захоплення кліком у вікні гри
        if (GameManager.IsPlaying && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetCursorLocked(true);

        // V або C перемикає вид
        if (Keyboard.current != null && (Keyboard.current.vKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame))
            firstPerson = !firstPerson;

        // Після смерті на кілька секунд повертаємось до третьої особи, щоб бачити анімацію
        bool useFirst = firstPerson && (targetHealth == null || !targetHealth.IsDead);
        ApplyMode(useFirst);

        float lo = useFirst ? firstPersonMinPitch : minPitch;
        float hi = useFirst ? firstPersonMaxPitch : maxPitch;
        if (GameManager.IsPlaying && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = lookAction.ReadValue<Vector2>();
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, lo, hi);
        }
        pitch = Mathf.Clamp(pitch, lo, hi);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (useFirst)
        {
            // Очі лицаря: трохи попереду голови, щоб не бачити зсередини модель
            Vector3 flatForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            transform.SetPositionAndRotation(target.position + Vector3.up * eyeHeight + flatForward * eyeForward, rotation);
            return;
        }

        Vector3 pivot = target.position + Vector3.up * targetHeight;
        Vector3 back = rotation * Vector3.back;

        // Якщо між гравцем і камерою є перешкода — підсуваємо камеру ближче
        float currentDistance = distance;
        if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            currentDistance = hit.distance;

        transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
    }

    // У першій особі тіло не малюється (лишається тільки тінь), а лицар дивиться туди ж, куди камера
    private void ApplyMode(bool useFirst)
    {
        if (playerController != null) playerController.FaceCamera = useFirst;
        if (useFirst == appliedFirstPerson) return;
        appliedFirstPerson = useFirst;

        if (bodyRenderers == null) return;
        var mode = useFirst ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
        foreach (var r in bodyRenderers)
            if (r != null) r.shadowCastingMode = mode;
    }

    // Після завантаження збереження повертаємо камеру за спину лицаря
    public void SnapYaw(float newYaw)
    {
        yaw = newYaw;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
