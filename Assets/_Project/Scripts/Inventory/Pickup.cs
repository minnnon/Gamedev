using UnityEngine;

// Предмет на землі: гравець заходить у тригер — предмет потрапляє в інвентар.
[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;
    [SerializeField] private float spinSpeed = 90f;

    public ItemData Item => item;
    public int Amount => amount;

    // Для завантаження збереження: налаштувати предмет, створений із префаба
    public void Setup(ItemData newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
    }

    private void Update()
    {
        if (spinSpeed != 0f) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        var inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null) return;
        if (inventory.Add(item, amount))
        {
            AudioManager.Play("pickup");
            Destroy(gameObject);
        }
    }
}
