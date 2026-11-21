using UnityEngine;

// Рухома платформа: їздить між стартовою точкою і start + moveOffset, везе гравця.
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public Vector2 moveOffset = new Vector2(3f, 0f);  // Куди і наскільки їхати від старту
    public float speed = 2f;                           // Клітинок за секунду
    public float waitTime = 0.6f;                      // Пауза на кінцях шляху

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 startPos, endPos, target;
    private float waitTimer;
    private Rigidbody2D rider;                         // Гравець, що стоїть зверху

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        col = GetComponent<Collider2D>();

        startPos = rb.position;
        endPos = startPos + moveOffset;
        target = endPos;
    }

    void FixedUpdate()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        Vector2 delta = newPos - rb.position;
        rb.MovePosition(newPos);

        // Зсуваємо гравця на стільки ж, на скільки зсунулася платформа
        if (rider != null)
        {
            rider.position += delta;
        }

        // Доїхали — пауза і розворот

        if ((newPos - target).sqrMagnitude < 0.0001f)
        {
            target = (target == endPos) ? startPos : endPos;
            waitTimer = waitTime;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // Пасажир — лише якщо стоїть ЗВЕРХУ платформи

        bool onTop = collision.collider.bounds.min.y >= col.bounds.max.y - 0.15f;
        rider = onTop ? collision.rigidbody : null;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            rider = null;
        }

    }
}
