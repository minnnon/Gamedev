using System.Collections.Generic;
using UnityEngine;

// Пастка з шипами: шипи циклічно висуваються й ховаються; висунуті — завдають шкоди.
public class SpikeTrap : MonoBehaviour
{
    [Header("Шипи")]
    [SerializeField] private Transform spikes;
    [SerializeField] private float hiddenDepth = 2f;
    [SerializeField] private float moveSpeed = 8f;

    [Header("Цикл")]
    [SerializeField] private float upTime = 1.5f;
    [SerializeField] private float downTime = 2f;
    [SerializeField] private float startDelay;

    [Header("Шкода")]
    [SerializeField] private int damage = 1;

    private readonly HashSet<Health> victims = new HashSet<Health>();
    private Vector3 upPosition;
    private Vector3 downPosition;
    private float timer;
    private bool isUp;
    private bool isActive = true;
    private BoxCollider zone;

    private void Start()
    {
        zone = GetComponent<BoxCollider>();
        upPosition = spikes.position;
        downPosition = upPosition + Vector3.down * hiddenDepth;
        spikes.position = downPosition;
        timer = downTime + startDelay;
    }

    private void Update()
    {
        if (isActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isUp = !isUp;
                timer = isUp ? upTime : downTime;
            }
        }
        else isUp = false;

        spikes.position = Vector3.MoveTowards(spikes.position, isUp ? upPosition : downPosition, moveSpeed * Time.deltaTime);

        // Поки шипи висунуті — б'ємо всіх, хто стоїть у зоні тригера
        if (isUp)
        {
            // Прибираємо лише тих, хто опинився далеко від плитки (наприклад, після відродження гравця).
            // Запас у 1 м потрібен, бо тригер спрацьовує вже від дотику краєм колайдера.
            if (zone != null)
            {
                Bounds area = zone.bounds;
                area.Expand(2f);
                victims.RemoveWhere(h => h == null || !area.Contains(h.transform.position + Vector3.up * 0.5f));
            }
            // Шкоду отримує лише той, чий центр стоїть над шипами, а не хто зачепив зону плечем
            foreach (var h in victims)
                if (zone == null || zone.bounds.Contains(h.transform.position + Vector3.up * 0.5f))
                    h.TakeDamage(damage);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var h = other.GetComponentInParent<Health>();
        if (h != null) victims.Add(h);
    }

    private void OnTriggerExit(Collider other)
    {
        var h = other.GetComponentInParent<Health>();
        if (h != null) victims.Remove(h);
    }

    // Важіль вимикає й вмикає пастку
    public void SetActive(bool value)
    {
        isActive = value;
        Debug.Log($"[Spikes] {name}: " + (value ? "увімкнено" : "вимкнено"));
    }
}
