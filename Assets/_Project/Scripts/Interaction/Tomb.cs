using UnityEngine;

// Гробниця: якщо в гравця є перстень — кладе його на місце, і прокидається Хазяїн гробниці.
public class Tomb : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData ringItem;
    [SerializeField] private GameObject boss;     // у сцені вимкнений, з'являється після кладення персня

    public bool IsUsed { get; private set; }
    public string Prompt => IsUsed ? "" : "Покласти перстень";

    public void Interact(GameObject interactor)
    {
        if (IsUsed) return;

        var inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || !inventory.Remove(ringItem, 1))
        {
            HUD.ShowMessage("Гробниця порожня. Бракує персня.");
            return;
        }
        IsUsed = true;
        if (boss != null) boss.SetActive(true);
        HUD.ShowMessage("Хазяїн гробниці прокинувся!", 3.5f);
        AudioManager.Play("chest");
        Debug.Log("[Tomb] Перстень покладено, бос прокинувся.");
    }

    // Для завантаження збереження: гробниця вже використана, бос уже ходить
    public void Restore()
    {
        IsUsed = true;
        if (boss != null) boss.SetActive(true);
    }
}
