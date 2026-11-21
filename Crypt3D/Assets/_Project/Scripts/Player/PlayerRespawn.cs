using System.Collections;
using UnityEngine;

// Відродження лицаря: пам'ятає точку (вогнище або старт). Після лаби 16 відродженням керує екран смерті,
// тому автоматичне відродження вимикається (AutoRespawn).
[RequireComponent(typeof(Health), typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 2.5f;   // час на анімацію смерті (лише для автоматичного режиму)

    private Health health;
    private CharacterController controller;
    private PlayerController movement;
    private Vector3 position;
    private Quaternion rotation = Quaternion.identity;

    public bool AutoRespawn { get; set; } = true;
    public Vector3 RespawnPosition => position;
    public float RespawnYaw => rotation.eulerAngles.y;

    private void Awake()
    {
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerController>();
        position = transform.position;
        rotation = transform.rotation;
        if (respawnPoint != null) SetRespawnPoint(respawnPoint);
    }

    private void OnEnable() { health.Died += OnDied; }
    private void OnDisable() { health.Died -= OnDied; }

    public void SetRespawnPoint(Transform point)
    {
        position = point.position;
        rotation = point.rotation;
    }

    public void SetRespawnPosition(Vector3 newPosition, float yaw)
    {
        position = newPosition;
        rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void OnDied()
    {
        if (movement != null) movement.InputLocked = true;
        if (AutoRespawn) StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        Debug.Log("[Player] Лицар загинув — відродження за кілька секунд.");
        yield return new WaitForSeconds(respawnDelay);
        RespawnNow();
    }

    // Телепорт у точку відродження з повним HP
    public void RespawnNow()
    {
        // CharacterController треба вимкнути, інакше він не дасть телепортувати об'єкт
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;

        health.Restore();
        if (movement != null) movement.InputLocked = false;
    }
}
