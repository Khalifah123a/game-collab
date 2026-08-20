using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Fan_Vortex : MonoBehaviour
{
    [Header("Push")]
    public Vector2 pushDirection = Vector2.up;
    public float pushForce = 25f;
    public float pushRadius = 3f;

    [Header("Visual")]
    public GameObject visualEffect;
    public float pulseSpeed = 4f;
    public float pulseMin = 0.8f;
    public float pulseMax = 1.2f;

    [Header("Damage")]
    public float damageAmount = 10f;
    public float damageCooldown = 0.5f;
    private float _damageTimer;

    private SpriteRenderer _sprite;
    private Collider2D _pushCollider;
    private Vector3 _baseScale;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _pushCollider = GetComponent<Collider2D>();
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1) / 2);
        transform.localScale = _baseScale * scale;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(pushDirection * pushForce, ForceMode2D.Force);

        if (Time.time >= _damageTimer && damageAmount > 0)
        {
            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
                _damageTimer = Time.time + damageCooldown;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pushRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, pushDirection.normalized * pushRadius);
    }
}
