using System.Collections.Generic;
using UnityEngine;

// Довідник усіх предметів гри: за іменем ассета повертає ItemData.
// Потрібен для завантаження збереження (у JSON лежить лише ім'я предмета).
// Ассет лежить у папці Resources, тому завантажується з коду за іменем.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Crypt/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();

    private static ItemDatabase cached;

    public static ItemDatabase Get()
    {
        if (cached == null) cached = Resources.Load<ItemDatabase>("ItemDatabase");
        return cached;
    }

    public ItemData Find(string assetName)
    {
        return items.Find(i => i != null && i.name == assetName);
    }
}
