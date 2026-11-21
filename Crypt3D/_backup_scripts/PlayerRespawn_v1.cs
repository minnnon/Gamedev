using UnityEngine;

// Коли здоров'я гравця закінчується — повертає його в точку відродження з повним HP.
[RequireComponent(typeof(Health), typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private Health health;
    private CharacterController controller;

    private void Awake()
    {
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable() { health.Died += Respawn; }
    private void OnDisable() { health.Died -= Respawn; }

    // Чекпоінт (лаба 16) зможе змінювати точку відродження
    public void SetRespawnPoint(Transform point) { respawnPoint = point; }

    private void Respawn()
    {
        Debug.Log("[Player] Лицар загинув — відродження.");
        // CharacterController треба вимкнути, інакше він не дасть телепортувати об'єкт
        controller.enabled = false;
        if (respawnPoint != null)
            transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        controller.enabled = true;
        health.Restore();
    }
}
