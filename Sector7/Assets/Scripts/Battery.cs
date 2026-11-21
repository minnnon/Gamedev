using UnityEngine;

// Батарея (енергоелемент): при дотику гравця додається до лічильника і зникає.
public class Battery : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.AddBattery();
            }
            if (AudioManager.instance != null)
            {
                AudioManager.instance.Play(AudioManager.instance.pickupSound);
            }

            Destroy(gameObject);
        }
    }
}
