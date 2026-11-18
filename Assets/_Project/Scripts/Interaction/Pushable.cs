using UnityEngine;

// Позначка: цей фізичний об'єкт гравець може штовхати.
[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    [SerializeField] private float resistance = 1f;   // що більше — то важче штовхати

    public float Resistance => Mathf.Max(0.1f, resistance);
}
