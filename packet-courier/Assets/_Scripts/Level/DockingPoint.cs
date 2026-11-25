using UnityEngine;

public class DockingPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (!player) return;
        player.airDashesLeft = 1;
        if (player.gravity == 0 && !player.isDashing) player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }
}
