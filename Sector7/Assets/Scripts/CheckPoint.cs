using UnityEngine;

// Чекпоінт: коли гравець проходить через тригер, тут стає його нова точка відродження.
public class CheckPoint : MonoBehaviour
{
    private bool activated = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SetSpawnPoint(transform.position);
                if (!activated)
                {
                    activated = true;
                    if (GameManager.instance != null)
                    {
                        GameManager.instance.ShowMessage("Чекпоінт збережено");
                    }
                }
            }
        }
    }
}
