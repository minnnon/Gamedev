using System.Collections;
using UnityEngine;

// Висить на фінальному босі: коли він помирає — фінальний текст, а після нього перемога.
[RequireComponent(typeof(Health))]
public class BossWin : MonoBehaviour
{
    [SerializeField, TextArea(2, 6)] private string finalText;
    [SerializeField] private float delay = 2.5f;     // дати програтися анімації смерті

    private Health health;

    private void Awake() { health = GetComponent<Health>(); }
    private void OnEnable() { health.Died += OnDied; }
    private void OnDisable() { health.Died -= OnDied; }

    private void OnDied() { StartCoroutine(Finish()); }

    private IEnumerator Finish()
    {
        yield return new WaitForSeconds(delay);
        // Показуємо текст; коли гравець закриє — перемога
        if (!NoteUI.Show("Кінець", finalText, () => GameManager.Instance.Win()))
            GameManager.Instance.Win();
    }
}
