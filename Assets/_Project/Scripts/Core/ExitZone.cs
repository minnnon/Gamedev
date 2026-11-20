using UnityEngine;

// Зона за Рунною брамою: коли лицар у неї заходить — перехід на наступний рівень.
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [SerializeField] private string nextScene = "Depths";     // порожньо — одразу перемога

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (GameManager.Instance == null) return;
        if (string.IsNullOrEmpty(nextScene)) GameManager.Instance.Win();
        else GameManager.Instance.NextLevel(nextScene);
    }
}
