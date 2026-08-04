using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hazard_Spike : MonoBehaviour
{
    [Header("Damage")]
    public PlayerHealth.DamageType damageType = PlayerHealth.DamageType.InstaKill;
    public float ttlDamage = 50f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health)
            health.TakeDamage(ttlDamage, damageType);
    }
}
