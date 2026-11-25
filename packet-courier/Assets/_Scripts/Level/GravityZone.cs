using UnityEngine;

public class GravityZone : MonoBehaviour
{
    public float gravity = 0;
    private PlayerController player;
    private float original;
    private void OnTriggerEnter2D(Collider2D other)
    {
        var found = other.GetComponent<PlayerController>();
        if (!found || player) return;
        player = found; original = player.gravity; player.gravity = gravity;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (player && other.gameObject == player.gameObject) Restore();
    }
    private void OnDisable() { Restore(); }
    private void Restore() { if (player) player.gravity = original; player = null; }
}
