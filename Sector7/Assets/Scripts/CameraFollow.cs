using UnityEngine;

// Камера плавно рухається за гравцем.
public class CameraFollow : MonoBehaviour
{
    [Header("Ціль")]
    public Transform target;

    [Header("Налаштування")]
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 1.5f, -10f);

    // Тряска камери (викликається з будь-якого скрипта: CameraFollow.Shake(...))
    private static float shakeTime;
    private static float shakeMagnitude;

    public static void Shake(float duration, float magnitude)
    {
        shakeTime = duration;
        shakeMagnitude = magnitude;
    }

    // LateUpdate — після того, як гравець уже перемістився в цьому кадрі
    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }


        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Поки триває тряска — додаємо випадкове зміщення
        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            smoothedPosition += (Vector3)(Random.insideUnitCircle * shakeMagnitude);
        }

        transform.position = smoothedPosition;
    }
}
