using UnityEngine;

// Натискна плита: поки на ній стоїть гравець або ящик — тримає двері відчиненими.
[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Що відкриваємо")]
    [SerializeField] private Door door;

    [Header("Вигляд")]
    [SerializeField] private Transform plateVisual;
    [SerializeField] private float pressDepth = 0.04f;

    private int bodiesOnPlate;
    private Vector3 visualUpPosition;

    private void Start()
    {
        if (plateVisual != null) visualUpPosition = plateVisual.localPosition;
    }

    // Тригер не зупиняє об'єкти, а лише повідомляє про вхід і вихід
    private void OnTriggerEnter(Collider other)
    {
        if (!CanPress(other)) return;
        bodiesOnPlate++;
        if (bodiesOnPlate == 1) SetPressed(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!CanPress(other)) return;
        bodiesOnPlate = Mathf.Max(0, bodiesOnPlate - 1);
        if (bodiesOnPlate == 0) SetPressed(false);
    }

    // Натиснути можуть гравець (CharacterController) і фізичні тіла (Rigidbody)
    private static bool CanPress(Collider other)
    {
        return other is CharacterController || other.attachedRigidbody != null;
    }

    private void SetPressed(bool pressed)
    {
        if (plateVisual != null)
            plateVisual.localPosition = visualUpPosition + (pressed ? Vector3.down * pressDepth : Vector3.zero);
        if (door != null) door.SetOpen(pressed);
        Debug.Log("[Plate] Плиту " + (pressed ? "натиснуто" : "відпущено"));
    }
}
