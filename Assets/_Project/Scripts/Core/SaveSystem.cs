using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Збереження й завантаження прогресу в один JSON-файл у Application.persistentDataPath.
public static class SaveSystem
{
    private static readonly HashSet<string> dead = new HashSet<string>();   // вбиті вороги цього проходження

    public static string FilePath => Path.Combine(Application.persistentDataPath, "crypt_save.json");
    public static bool HasSave => File.Exists(FilePath);

    // Ідентифікатор об'єкта сцени: шлях з іменами й номерами серед сусідів. Однаковий у кожному запуску.
    public static string IdOf(Transform t)
    {
        var sb = new StringBuilder();
        while (t != null)
        {
            sb.Insert(0, "/" + t.name + "[" + t.GetSiblingIndex() + "]");
            t = t.parent;
        }
        return sb.ToString();
    }

    public static void MarkDead(Transform enemy) { dead.Add(IdOf(enemy)); }
    public static void ClearRuntime() { dead.Clear(); }

    public static string ReadInfo()
    {
        if (!HasSave) return "";
        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            return $"Збереження: {data.savedAt}";
        }
        catch { return ""; }
    }

    public static bool Save()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        var data = new SaveData
        {
            savedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            playTime = GameManager.Instance != null ? GameManager.Instance.PlayTime : 0f,
            position = player.transform.position,
            yaw = player.transform.eulerAngles.y
        };

        var health = player.GetComponent<Health>();
        if (health != null) data.health = health.Current;

        var respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) { data.respawnPosition = respawn.RespawnPosition; data.respawnYaw = respawn.RespawnYaw; }

        var inventory = player.GetComponent<Inventory>();
        if (inventory != null)
            foreach (var s in inventory.Slots)
                if (s.item != null) data.items.Add(new ItemEntry { item = s.item.name, count = s.count });

        foreach (var d in UnityEngine.Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            if (d.IsOpen) data.openDoors.Add(IdOf(d.transform));
        foreach (var c in UnityEngine.Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
            if (c.IsOpen) data.openChests.Add(IdOf(c.transform));
        foreach (var s in UnityEngine.Object.FindObjectsByType<RuneSocket>(FindObjectsSortMode.None))
            if (s.IsFilled) data.filledSockets.Add(IdOf(s.transform));
        data.deadEnemies.AddRange(dead);
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            if (p.Item != null) data.pickups.Add(new PickupEntry { item = p.Item.name, amount = p.Amount, position = p.transform.position });

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log("[Save] Збережено: " + FilePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Save] Помилка запису: " + e.Message);
            return false;
        }
    }

    public static bool Load()
    {
        if (!HasSave) return false;

        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
        catch (Exception e) { Debug.LogError("[Save] Помилка читання: " + e.Message); return false; }
        if (data == null) return false;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        var db = ItemDatabase.Get();

        // Гравець: CharacterController треба вимкнути, інакше він не дасть телепортувати об'єкт
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(data.position, Quaternion.Euler(0f, data.yaw, 0f));
        if (cc != null) cc.enabled = true;

        var cam = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null) cam.SnapYaw(data.yaw);

        var health = player.GetComponent<Health>();
        if (health != null) health.SetCurrent(data.health);

        var respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.SetRespawnPosition(data.respawnPosition, data.respawnYaw);

        // Інвентар: з імені збереженого предмета знаходимо ассет у довіднику
        var inventory = player.GetComponent<Inventory>();
        if (inventory != null)
        {
            inventory.Clear();
            foreach (var e in data.items)
            {
                var item = db != null ? db.Find(e.item) : null;
                if (item != null) inventory.Add(item, e.count);
            }
        }

        // Світ
        foreach (var d in UnityEngine.Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            d.SetOpen(data.openDoors.Contains(IdOf(d.transform)));
        foreach (var c in UnityEngine.Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
            if (data.openChests.Contains(IdOf(c.transform))) c.Restore();
        foreach (var s in UnityEngine.Object.FindObjectsByType<RuneSocket>(FindObjectsSortMode.None))
            if (data.filledSockets.Contains(IdOf(s.transform))) s.Restore();

        dead.Clear();
        dead.UnionWith(data.deadEnemies);
        foreach (var en in UnityEngine.Object.FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
            if (dead.Contains(IdOf(en.transform))) UnityEngine.Object.Destroy(en.gameObject);

        // Предмети на землі: старі прибираємо, створюємо ті, що були на момент збереження
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(p.gameObject);
        foreach (var pe in data.pickups)
        {
            var item = db != null ? db.Find(pe.item) : null;
            if (item == null || item.pickupPrefab == null) continue;
            var go = UnityEngine.Object.Instantiate(item.pickupPrefab, pe.position, Quaternion.identity);
            var pickup = go.GetComponent<Pickup>();
            if (pickup != null) pickup.Setup(item, pe.amount);
        }

        if (GameManager.Instance != null) GameManager.Instance.PlayTime = data.playTime;
        Debug.Log("[Save] Збереження завантажено: " + data.savedAt);
        HUD.ShowMessage("Прогрес завантажено");
        return true;
    }
}
