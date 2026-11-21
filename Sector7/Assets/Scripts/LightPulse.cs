using UnityEngine;
using UnityEngine.Rendering.Universal;

// Декор: світло 2D "дихає" — яскравість плавно змінюється.
[RequireComponent(typeof(Light2D))]
public class LightPulse : MonoBehaviour
{
    public float speed = 2f;       // Швидкість пульсації
    public float amount = 0.25f;   // На скільки змінюється яскравість (0.25 = ±25%)

    private Light2D light2D;
    private float baseIntensity;
    private float phase;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        phase = Random.value * 10f;
    }

    void Update()
    {
        light2D.intensity = baseIntensity * (1f + Mathf.Sin(Time.time * speed + phase) * amount);
    }
}
