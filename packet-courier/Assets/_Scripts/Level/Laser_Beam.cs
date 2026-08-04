using UnityEngine;

public class Laser_Beam : MonoBehaviour
{
    [Header("Timing")]
    public float onDuration = 2f;
    public float offDuration = 1.5f;
    public float phaseOffset = 0f;

    [Header("Damage")]
    public float damageAmount = 30f;
    public float damageCooldown = 1f;

    [Header("Visual")]
    public SpriteRenderer beamSprite;

    public bool IsOn { get; private set; }

    private Collider2D _collider;
    private float _timer;
    private float _damageTimer;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (beamSprite == null) beamSprite = GetComponent<SpriteRenderer>();
        _timer = -phaseOffset;
    }

    private void Update()
    {
        float cycle = onDuration + offDuration;
        if (cycle <= 0) return;

        _timer += Time.deltaTime;
        SetOn(Mathf.Repeat(_timer, cycle) < onDuration);
    }

    private void SetOn(bool on)
    {
        IsOn = on;
        if (_collider) _collider.enabled = on;
        if (beamSprite) beamSprite.enabled = on;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOn) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time < _damageTimer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health)
        {
            health.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
            _damageTimer = Time.time + damageCooldown;
        }
    }
}
