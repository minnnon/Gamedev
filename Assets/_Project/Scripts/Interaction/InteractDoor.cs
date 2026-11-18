using UnityEngine;

// Двері, які гравець відчиняє й зачиняє клавішею E.
[RequireComponent(typeof(Door))]
public class InteractDoor : MonoBehaviour, IInteractable
{
    private Door door;

    public string Prompt => door != null && door.IsOpen ? "Зачинити двері" : "Відчинити двері";

    private void Awake()
    {
        door = GetComponent<Door>();
    }

    public void Interact(GameObject interactor)
    {
        door.SetOpen(!door.IsOpen);
    }
}
