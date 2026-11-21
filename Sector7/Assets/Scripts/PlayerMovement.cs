using UnityEngine;

// Рух робота: біг, стрибок, перевірка землі, анімації.
public class PlayerMovement : MonoBehaviour
{
    [Header("Рух")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;

    [Header("Перевірка землі")]
    public LayerMask groundLayer;             // Шар, який вважається землею (Ground)
    public float groundCheckDistance = 0.1f;  // Наскільки глибоко під ногами шукаємо землю

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;
    private float horizontalInput;
    private bool isGrounded;
    private float knockbackTimer;             // Поки > 0, керування вимкнене (робота відкинуло)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        // На паузі та після кінця гри не реагуємо на клавіші
        if (Time.timeScale == 0f)
        {
            return;
        }


        horizontalInput = Input.GetAxisRaw("Horizontal");
        isGrounded = CheckGround();

        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
            anim.SetBool("IsGrounded", isGrounded);
        }
        
        // Розвертаємо спрайт у бік руху
        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }


        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
             if (AudioManager.instance != null)
        {
            AudioManager.instance.Play(AudioManager.instance.jumpSound, volume: 0.3f);
        }
        }
       
    }

    void FixedUpdate()
    {
        // Під час відкидання не перезаписуємо швидкість
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    // Відкидання після удару (викликає PlayerHealth)
    public void Knockback(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        knockbackTimer = 0.25f;
    }

    // BoxCast: вузька "коробка" з-під ніг вниз. Стіна збоку в неї не потрапляє.
    private bool CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y);
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.05f);

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    // Налагодження: зона перевірки у вікні Scene (зелена — на землі, червона — у повітрі)
    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null)
        {
            return;
        }


        Bounds b = c.bounds;
        Vector3 center = new Vector3(b.center.x, b.min.y - groundCheckDistance / 2f, 0f);
        Vector3 size = new Vector3(b.size.x * 0.9f, groundCheckDistance + 0.05f, 0f);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
