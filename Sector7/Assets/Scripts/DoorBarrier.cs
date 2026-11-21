using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Гермодвері: окремий тайлмап з колайдером. Open() плавно піднімає їх угору.
public class DoorBarrier : MonoBehaviour
{
    public float openHeight = 3f;            // На скільки клітинок піднятися
    public float openTime = 1.5f;            // За скільки секунд
    public Light2D statusLight;              // Лампа над дверима: червона — зачинено, зелена — відчинено

    private bool isOpen;

    public void Open()
    {
        if (isOpen)
        {
            return;
        }


        isOpen = true;
        StartCoroutine(OpenRoutine());
        if (statusLight != null)
        {
            statusLight.color = new Color(0.3f, 1f, 0.45f);
        }
        if (AudioManager.instance != null)
        {
            AudioManager.instance.Play(AudioManager.instance.doorSound);
        }

    }

    private IEnumerator OpenRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * openHeight;
        float t = 0f;

        while (t < openTime)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / openTime);
            yield return null;                   // Чекаємо наступний кадр
        }

        // Прохід гарантовано вільний
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

    }
}
