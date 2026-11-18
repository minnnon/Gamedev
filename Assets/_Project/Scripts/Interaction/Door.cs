using UnityEngine;

// Двері або решітка: між закритим і відкритим положенням можуть і зсуватися, і повертатися.
public class Door : MonoBehaviour
{
    [Header("Зсув (решітка)")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3.3f, 0f);
    [SerializeField] private float speed = 2.5f;

    [Header("Поворот (двері на петлях)")]
    [SerializeField] private Vector3 openEuler = Vector3.zero;
    [SerializeField] private float turnSpeed = 180f;

    [Header("Стан")]
    [SerializeField] private bool startOpen;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(openEuler);
        IsOpen = startOpen;
        if (IsOpen)
        {
            transform.position = openPosition;
            transform.localRotation = openRotation;
        }
    }

    public void Open() { SetOpen(true); }
    public void Close() { SetOpen(false); }

    public void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        Debug.Log($"[Door] {name}: " + (open ? "відчиняється" : "зачиняється"));
    }

    private void Update()
    {
        Vector3 targetPosition = IsOpen ? openPosition : closedPosition;
        Quaternion targetRotation = IsOpen ? openRotation : closedRotation;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}
