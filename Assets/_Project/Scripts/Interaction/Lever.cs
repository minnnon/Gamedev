using UnityEngine;

// Важіль: по клавіші E вмикає або вимикає пастки з шипами.
public class Lever : MonoBehaviour, IInteractable
{
    [Header("Вигляд")]
    [SerializeField] private Transform handle;
    [SerializeField] private float angle = 35f;

    [Header("Чим керує")]
    [SerializeField] private SpikeTrap[] traps;
    [SerializeField] private bool trapsOn = true;

    public string Prompt => trapsOn ? "Вимкнути пастки" : "Увімкнути пастки";

    private void Start()
    {
        ApplyVisual();
    }

    public void Interact(GameObject interactor)
    {
        trapsOn = !trapsOn;
        foreach (var trap in traps)
            if (trap != null) trap.SetActive(trapsOn);
        ApplyVisual();
        Debug.Log("[Lever] Пастки " + (trapsOn ? "увімкнено" : "вимкнено"));
    }

    private void ApplyVisual()
    {
        if (handle != null) handle.localRotation = Quaternion.Euler(trapsOn ? -angle : angle, 0f, 0f);
    }
}
