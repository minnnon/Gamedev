using UnityEngine;

// Декор: предмет плавно гойдається вгору-вниз (батареї).
public class Hover : MonoBehaviour
{
    public float height = 0.12f;   // Амплітуда, клітинок
    public float speed = 2f;       // Швидкість коливання

    private Vector3 startPos;
    private float phase;           // Зсув, щоб батареї гойдались не синхронно

    void Start()
    {
        startPos = transform.localPosition;
        phase = Random.value * 6f;
    }

    void Update()
    {
        transform.localPosition = startPos + Vector3.up * Mathf.Sin(Time.time * speed + phase) * height;
    }
}
