using UnityEngine;

// Двері або решітка, що зсуваються між закритим і відкритим положенням.
public class Door : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3.3f, 0f);
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private bool startOpen;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        IsOpen = startOpen;
        if (IsOpen) transform.position = openPosition;
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
        Vector3 target = IsOpen ? openPosition : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }
}
