using UnityEngine;

// Гніздо для руни: по клавіші E забирає руну з інвентаря і загоряється.
public class RuneSocket : MonoBehaviour, IInteractable
{
    [SerializeField] private RuneGate gate;
    [SerializeField] private ItemData runeItem;
    [SerializeField] private Renderer crystal;
    [SerializeField] private Material activeMaterial;

    public bool IsFilled { get; private set; }
    public string Prompt => IsFilled ? "" : "Вставити руну";

    public void Interact(GameObject interactor)
    {
        if (IsFilled) return;

        var inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || !inventory.Remove(runeItem, 1))
        {
            Debug.Log("[Rune] Потрібна руна.");
            return;
        }
        Fill();
        if (gate != null) gate.OnRuneInserted();
    }

    // Для завантаження збереження: гніздо знову заповнене, брама це рахує
    public void Restore()
    {
        if (IsFilled) return;
        Fill();
        if (gate != null) gate.OnRuneInserted();
    }

    // Окремий метод — щоб завантаження збереження (лаба 16) могло відновити стан
    public void Fill()
    {
        IsFilled = true;
        if (crystal != null && activeMaterial != null) crystal.sharedMaterial = activeMaterial;
    }
}
