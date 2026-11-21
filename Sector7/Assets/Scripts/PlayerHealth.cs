using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Здоров'я робота: шкода, невразливість після удару, падіння в яму, точка відродження.
public class PlayerHealth : MonoBehaviour
{
    [Header("Здоров'я")]
    public int maxHealth = 3;
    public float invulnerabilityTime = 1f;   // Скільки секунд робот невразливий після удару

    [Header("Відкидання при ударі")]
    public Vector2 knockbackForce = new Vector2(6f, 6f);

    private int currentHealth;
    private bool isInvulnerable;
    private Vector3 spawnPoint;               // Куди повертаємо робота після падіння

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerMovement movement;

    public int CurrentHealth => currentHealth; // Для читання HP іншими скриптами (інтерфейс)

    void Start()
    {
        currentHealth = maxHealth;
        spawnPoint = transform.position;

        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        RefreshUI();
    }

    // Оновлюємо HP на екрані
    private void RefreshUI()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHealth(currentHealth, maxHealth);
        }

    }

    // Чекпоінт: нова точка відродження
    public void SetSpawnPoint(Vector3 point)
    {
        spawnPoint = point;
    }

    // Удар ворога. sourcePosition — звідки удар (щоб відкинути в інший бік)
    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isInvulnerable)
        {
            return;
        }


        currentHealth -= damage;
        RefreshUI();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        if (AudioManager.instance != null)
        {
            AudioManager.instance.Play(AudioManager.instance.hurtSound);
        }
        CameraFollow.Shake(0.2f, 0.15f);        // Камера трясеться від удару


        float direction = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (movement != null)
        {
            movement.Knockback(new Vector2(direction * knockbackForce.x, knockbackForce.y));
        }


        StartCoroutine(InvulnerabilityRoutine());
    }

    // Падіння в яму: мінус 1 HP і повернення на точку відродження
    public void FallIntoPit()
    {
        currentHealth -= 1;
        RefreshUI();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        transform.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(InvulnerabilityRoutine());
    }

    // Корутина: невразливість і блимання спрайта протягом invulnerabilityTime
    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float timer = 0f;

        while (timer < invulnerabilityTime)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        sr.enabled = true;
        isInvulnerable = false;
    }

    // Смерть: екран поразки (якщо GameManager немає — просто перезапуск)
    private void Die()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.GameOver();
        }
        else
        {

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

    }
}
