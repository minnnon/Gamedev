using UnityEngine;

// Зона виходу за гермодверима (Is Trigger): з усіма батареями — перемога.
public class ExitZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || GameManager.instance == null)
        {
            return;
        }


        if (GameManager.instance.AllBatteriesCollected)
        {
            GameManager.instance.Win();
        }
        else
        {

            GameManager.instance.ShowMessage("Шлюз знеструмлено. Знайди всі енергоелементи!");
        }

    }
}
