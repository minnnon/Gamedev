using UnityEngine;

// Контракт для всього, з чим гравець взаємодіє клавішею E (скриня, важіль тощо).
public interface IInteractable
{
    string Prompt { get; }                  // текст підказки для HUD (лаба 16)
    void Interact(GameObject interactor);
}
