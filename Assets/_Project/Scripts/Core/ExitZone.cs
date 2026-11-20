using UnityEngine;

// Зона за Рунною брамою: коли лицар у неї заходить — перехід на наступний рівень.
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [SerializeField] private string nextScene = "Depths";     // порожньо — одразу перемога

    private bool used;

    // Stay, а не Enter: гравець може вже стояти в зоні, коли відкривається брама
    private void OnTriggerStay(Collider other)
    {
        if (used) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        var gate = FindFirstObjectByType<RuneGate>();
        if (gate != null && !gate.IsOpen) return;     // поки брама зачинена — вихід не працює
        used = true;
        Debug.Log("[Exit] Гравець зайшов у зону виходу");
        if (GameManager.Instance == null) return;
        if (string.IsNullOrEmpty(nextScene)) GameManager.Instance.Win();
        else GameManager.Instance.NextLevel(nextScene);
    }
}
