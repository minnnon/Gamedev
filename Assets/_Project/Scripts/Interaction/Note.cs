using UnityEngine;

// Записка (або табличка): по клавіші E відкриває панель з текстом. Прочитане зберігається.
public class Note : MonoBehaviour, IInteractable
{
    [SerializeField] private string title = "Записка";
    [SerializeField, TextArea(2, 6)] private string body;

    public bool IsRead { get; private set; }
    public string Prompt => "Прочитати";

    public void Interact(GameObject interactor)
    {
        if (!NoteUI.Show(title, body)) return;
        IsRead = true;
        Debug.Log($"[Note] Прочитано: {title}");
    }

    // Для завантаження збереження
    public void Restore() { IsRead = true; }
}
