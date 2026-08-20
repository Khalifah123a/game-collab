using UnityEngine;

public class Laser_Beam : MonoBehaviour
{
    [Header("Timing")]
    public float onDuration = 2f;
    public float offDuration = 1.5f;
    public float phaseOffset = 0f;

    [Header("Damage")]
    public float damageAmount = 30f;
    public float damageCooldown = 0.25f;

    [Header("Visual")]
    public SpriteRenderer beamSprite;

    public bool IsOn { get; private set; }

    private Collider2D _collider;
    private float _timer;
    private float _damageTimer;
    private bool _playerInside;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (beamSprite == null) beamSprite = GetComponent<SpriteRenderer>();
        _timer = -phaseOffset;

        if (_collider != null)
            _collider.isTrigger = true;
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
        if (_collider != null)
            _collider.enabled = on;
        if (beamSprite != null)
            beamSprite.enabled = on;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInside = false;
    }

    private void FixedUpdate()
    {
        if (!IsOn || !_playerInside) return;
        if (Time.time < _damageTimer) return;

        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
            _damageTimer = Time.time + damageCooldown;
        }
    }
}
