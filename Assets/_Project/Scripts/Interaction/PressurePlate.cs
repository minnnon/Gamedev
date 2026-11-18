using UnityEngine;

// Натискна плита: поки на ній стоїть гравець або ящик — тримає двері відчиненими.
[RequireComponent(typeof(BoxCollider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Що відкриваємо")]
    [SerializeField] private Door door;

    [Header("Вигляд")]
    [SerializeField] private Transform plateVisual;
    [SerializeField] private float pressDepth = 0.04f;

    [Header("Чутливість")]
    [SerializeField] private float pressRadius = 0.9f;   // як близько до центру має бути об'єкт

    private static readonly Collider[] hits = new Collider[16];
    private BoxCollider zone;
    private Vector3 visualUpPosition;
    private bool isPressed;

    private void Awake()
    {
        zone = GetComponent<BoxCollider>();
        if (plateVisual != null) visualUpPosition = plateVisual.localPosition;
    }

    // Кожен крок фізики заново перевіряємо, чи є щось на плиті.
    // Так стан не "залипає" після телепортації гравця чи перезапуску.
    private void FixedUpdate()
    {
        bool pressed = SomethingOnPlate();
        if (pressed != isPressed) SetPressed(pressed);
    }

    private bool SomethingOnPlate()
    {
        Bounds b = zone.bounds;
        int count = Physics.OverlapBoxNonAlloc(b.center, b.extents, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            // Натиснути можуть гравець (CharacterController) і об'єкти з позначкою Pushable
            bool canPress = c is CharacterController || c.GetComponentInParent<Pushable>() != null;
            if (!canPress) continue;

            // Рахуємо тільки тих, чий центр справді над плитою, а не хто зачепив її краєм
            Vector3 offset = c.bounds.center - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= pressRadius) return true;
        }
        return false;
    }

    private void SetPressed(bool pressed)
    {
        isPressed = pressed;
        if (plateVisual != null)
            plateVisual.localPosition = visualUpPosition + (pressed ? Vector3.down * pressDepth : Vector3.zero);
        if (door != null) door.SetOpen(pressed);
        Debug.Log("[Plate] Плиту " + (pressed ? "натиснуто" : "відпущено"));
    }
}
