using System;
using UnityEngine;

// Рунна брама: рахує вставлені руни. Дві руни відкривають крипту, три — вихід.
public class RuneGate : MonoBehaviour
{
    [SerializeField] private RuneSocket[] sockets;
    [SerializeField] private GameObject barrier;
    [SerializeField] private Door cryptDoor;
    [SerializeField] private int runesForCrypt = 2;

    public int Inserted { get; private set; }
    public int Total => sockets != null ? sockets.Length : 0;
    public bool IsOpen { get; private set; }

    public event Action Changed;
    public event Action Opened;   // на цю подію в лабі 16 підпишеться перемога

    public void OnRuneInserted()
    {
        Inserted++;
        Debug.Log($"[Gate] Руну вставлено: {Inserted}/{Total}");
        Changed?.Invoke();

        if (Inserted >= runesForCrypt && cryptDoor != null) cryptDoor.Open();
        if (Inserted >= Total && !IsOpen)
        {
            IsOpen = true;
            if (barrier != null) barrier.SetActive(false);
            Debug.Log("[Gate] Рунну браму відкрито!");
            Opened?.Invoke();
        }
    }
}
