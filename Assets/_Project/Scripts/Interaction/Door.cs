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
    private Collider[] colliders;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(openEuler);
        colliders = GetComponentsInChildren<Collider>(true);
        IsOpen = startOpen;
        UpdateColliders();
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
        UpdateColliders();
        Debug.Log($"[Door] {name}: " + (open ? "відчиняється" : "зачиняється"));
    }

    // Відчинені двері не мають заважати проходу (стулка могла перекривати проріз): колайдери вимкнені, поки двері відчинені.
    // При зачиненні вмикаються одразу — стулка виштовхує того, хто стоїть у прорізі.
    private void UpdateColliders()
    {
        if (colliders == null) return;
        foreach (var c in colliders) if (c != null) c.enabled = !IsOpen;
    }

    private void Update()
    {
        Vector3 targetPosition = IsOpen ? openPosition : closedPosition;
        Quaternion targetRotation = IsOpen ? openRotation : closedRotation;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}
