using UnityEngine;

// CharacterController сам не штовхає Rigidbody, тому робимо це вручну в момент зіткнення.
public class PlayerPush : MonoBehaviour
{
    [SerializeField] private float pushSpeed = 2.5f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.rigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;          // стоїмо зверху — не штовхаємо

        var pushable = body.GetComponent<Pushable>();
        if (pushable == null) return;

        // Штовхаємо тільки по горизонталі, вертикальну швидкість тіла не чіпаємо
        Vector3 direction = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        Vector3 velocity = direction * (pushSpeed / pushable.Resistance);
        velocity.y = body.linearVelocity.y;
        body.linearVelocity = velocity;
    }
}
