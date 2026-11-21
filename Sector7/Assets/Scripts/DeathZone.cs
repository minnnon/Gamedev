using UnityEngine;

// Невидима зона внизу шахти (Is Trigger): падіння забирає 1 HP.
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.FallIntoPit();
            }
        }
    }
}
