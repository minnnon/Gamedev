using UnityEngine;

// Зона за Рунною брамою: коли лицар у неї заходить — перемога.
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (GameManager.Instance != null) GameManager.Instance.Win();
    }
}
