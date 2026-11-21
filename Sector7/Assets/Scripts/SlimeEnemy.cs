using UnityEngine;

// Слиз: патрулює платформу, розвертається біля краю чи стіни, б'є гравця, гине від стрибка зверху.
public class SlimeEnemy : MonoBehaviour
{
    [Header("Рух")]
    public float speed = 1.5f;
    public LayerMask groundLayer;            // Шар землі (Ground)

    [Header("Бій")]
    public int damage = 1;
    public float stompBounce = 8f;           // Швидкість відскоку робота після стрибка на слиз

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private int direction = 1;               // 1 — праворуч, -1 — ліворуч
    private bool isDead;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        // Рухаємо через швидкість Rigidbody, а не через transform

        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        if (ShouldTurn())
        {
            direction = -direction;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }
    }

    // Raycast: чи є попереду обрив або стіна
    private bool ShouldTurn()
    {
        Bounds b = col.bounds;
        Vector2 frontFoot = new Vector2(direction > 0 ? b.max.x + 0.05f : b.min.x - 0.05f, b.min.y + 0.05f);

        bool groundAhead = Physics2D.Raycast(frontFoot, Vector2.down, 0.3f, groundLayer);
        bool wallAhead = Physics2D.Raycast(new Vector2(b.center.x, b.center.y), Vector2.right * direction, b.extents.x + 0.1f, groundLayer);

        return !groundAhead || wallAhead;
    }

    // Stay — щокадру, поки є дотик: після невразливості гравець знову отримає шкоду
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }


        Rigidbody2D playerRb = collision.rigidbody;
        Bounds playerBounds = collision.collider.bounds;

        // Гравець падає зверху і його ноги вище середини слизу — стрибок на голову
        bool stomp = playerRb.linearVelocity.y <= 0.1f && playerBounds.min.y > col.bounds.center.y;

        if (stomp)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounce);
            Die();
        }
        else
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }

        }
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger("Die");                 // Тригер переходу Any State -> Slime_Death
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.Play(AudioManager.instance.stompSound);
        }


        Destroy(gameObject, 0.8f);                  // Видаляємо після анімації смерті
    }

    // Налагодження: промені у вікні Scene (жовті лінії)
    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null)
        {
            return;
        }


        Bounds b = c.bounds;
        Vector3 frontFoot = new Vector3(direction > 0 ? b.max.x + 0.05f : b.min.x - 0.05f, b.min.y + 0.05f, 0);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(frontFoot, frontFoot + Vector3.down * 0.3f);
        Gizmos.DrawLine(b.center, b.center + Vector3.right * direction * (b.extents.x + 0.1f));
    }
}
