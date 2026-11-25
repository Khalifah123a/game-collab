using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hazard_Spike : MonoBehaviour
{
    [Header("Damage")]
    public PlayerHealth.DamageType damageType = PlayerHealth.DamageType.TTLDamage;
    public float ttlDamage = 18f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (SafeZone.IsPlayerSafe || DialogUI.IsOpen || SoftwareQuizUI.IsQuizOpen) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null && !health.IsInvulnerable)
        {
            health.TakeDamage(ttlDamage, damageType);

            // Upward safety spring bounce to launch player back onto platforms
            var rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.4f, 13.5f);
            }
        }
    }
}
