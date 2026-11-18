using UnityEngine;
using UnityEngine.InputSystem;

// Керування лицарем: рух відносно камери, біг, стрибок і гравітація.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Стрибок і гравітація")]
    [SerializeField] private float jumpHeight = 1.3f;
    [SerializeField] private float gravity = -20f;

    [Header("Посилання")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionAsset inputActions;

    private CharacterController controller;
    private PlayerBlock block;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private float verticalVelocity;

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;

    // Блокує керування (смерть, пауза), гравітація при цьому працює далі
    public bool InputLocked { get; set; }

    // Камера від першої особи вмикає це: лицар завжди дивиться туди ж, куди камера
    public bool FaceCamera { get; set; }

    // Множник швидкості (блок щитом сповільнює)
    public float SpeedMultiplier { get; set; } = 1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        block = GetComponent<PlayerBlock>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        // Дії беремо з карти "Player" стандартного набору Input System
        var map = inputActions.FindActionMap("Player", true);
        moveAction = map.FindAction("Move", true);
        jumpAction = map.FindAction("Jump", true);
        sprintAction = map.FindAction("Sprint", true);
    }

    private void OnEnable()
    {
        inputActions.FindActionMap("Player", true).Enable();
    }

    private void OnDisable()
    {
        inputActions.FindActionMap("Player", true).Disable();
    }

    private void Update()
    {
        // Напрямок руху рахуємо відносно камери, а не світових осей
        bool locked = InputLocked || !GameManager.IsPlaying;   // смерть, пауза, меню
        Vector2 input = locked ? Vector2.zero : moveAction.ReadValue<Vector2>();
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * input.y + right * input.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        bool blocking = block != null && block.IsBlocking;
        if ((FaceCamera || blocking) && !locked)
        {
            // Перша особа або блок: дивимось у бік камери
            Quaternion look = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 20f * Time.deltaTime);
        }
        else if (direction.sqrMagnitude > 0.001f)
        {
            // Плавно повертаємо лицаря в бік руху
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // На землі тримаємо невелику швидкість униз, щоб isGrounded був стабільним
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

        // Початкова швидкість стрибка з формули v = sqrt(2 * g * h)
        if (controller.isGrounded && !locked && !blocking && jumpAction.WasPressedThisFrame())
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        float speed = (sprintAction.IsPressed() && !blocking ? sprintSpeed : walkSpeed) * SpeedMultiplier;
        Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        CurrentSpeed = direction.magnitude * speed;
    }
}
