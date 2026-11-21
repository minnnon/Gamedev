using UnityEngine;
using UnityEngine.InputSystem;

// Висить на гравці: шукає поруч найближчий IInteractable і викликає його по клавіші E.
public class Interactor : MonoBehaviour
{
    [Header("Пошук об'єктів")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private float height = 1f;

    [Header("Ввід")]
    [SerializeField] private InputActionAsset inputActions;

    private static readonly Collider[] hits = new Collider[16];
    private InputAction interactAction;

    // Поточний об'єкт поруч (для підказки в HUD)
    public IInteractable Current { get; private set; }

    private void Awake()
    {
        interactAction = inputActions.FindActionMap("Player", true).FindAction("Interact", true);
    }

    private void Update()
    {
        if (!GameManager.IsPlaying) { Current = null; return; }
        Current = FindNearest();
        if (Current != null && interactAction.WasPressedThisFrame())
            Current.Interact(gameObject);
    }

    private IInteractable FindNearest()
    {
        Vector3 center = transform.position + Vector3.up * height;
        int count = Physics.OverlapSphereNonAlloc(center, radius, hits, ~0, QueryTriggerInteraction.Collide);

        IInteractable nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var candidate = hits[i].GetComponentInParent<IInteractable>();
            if (candidate == null) continue;
            float d = (hits[i].bounds.ClosestPoint(center) - center).sqrMagnitude;   // bounds — щоб працювало і з MeshCollider
            if (d < best) { best = d; nearest = candidate; }
        }
        return nearest;
    }
}
