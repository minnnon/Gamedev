using UnityEngine;

public enum ItemType { Potion, Rune, Coin }

// Опис предмета як ассет (ScriptableObject): дані лежать у проєкті, а не в сцені.
[CreateAssetMenu(fileName = "Item", menuName = "Crypt/Item")]
public class ItemData : ScriptableObject
{
    public string displayName = "Предмет";
    public ItemType type;
    [TextArea] public string description;
    public int healAmount;              // скільки HP відновлює (для зілля)
    public Color color = Color.white;   // колір значка в HUD (лаба 16)
    public GameObject pickupPrefab;     // як предмет виглядає на землі
}
