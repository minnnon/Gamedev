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
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private float verticalVelocity;

    // Знадобляться аніматору в лабі 13
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * input.y + right * input.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // Плавно повертаємо лицаря в бік руху
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // На землі тримаємо невелику швидкість униз, щоб isGrounded був стабільним
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

        // Початкова швидкість стрибка з формули v = sqrt(2 * g * h)
        if (controller.isGrounded && jumpAction.WasPressedThisFrame())
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        CurrentSpeed = direction.magnitude * speed;
    }
}
