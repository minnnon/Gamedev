using System;
using System.Collections.Generic;
using UnityEngine;

// Усе, що потрапляє в JSON-файл збереження. Тільки прості поля й списки — їх вміє JsonUtility.

[Serializable]
public class ItemEntry
{
    public string item;      // ім'я ассета ItemData
    public int count;
}

[Serializable]
public class PickupEntry
{
    public string item;
    public int amount;
    public Vector3 position;
}

[Serializable]
public class SaveData
{
    public int version = 1;
    public string savedAt;
    public float playTime;

    // Гравець
    public Vector3 position;
    public float yaw;
    public int health;
    public Vector3 respawnPosition;
    public float respawnYaw;
    public List<ItemEntry> items = new List<ItemEntry>();

    // Світ (ідентифікатор об'єкта — шлях в ієрархії сцени)
    public List<string> openDoors = new List<string>();
    public List<string> openChests = new List<string>();
    public List<string> filledSockets = new List<string>();
    public List<string> readNotes = new List<string>();
    public List<string> deadEnemies = new List<string>();
    public List<PickupEntry> pickups = new List<PickupEntry>();
}
