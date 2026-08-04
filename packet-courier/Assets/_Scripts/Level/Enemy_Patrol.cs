using UnityEngine;

public class Enemy_Patrol : MonoBehaviour
{
    [Header("Patrol")]
    public Vector2 pointA = new Vector2(-2, 0);
    public Vector2 pointB = new Vector2(2, 0);
    public float patrolSpeed = 2f;

    [Header("Chase")]
    public float detectionRadius = 5f;
    public float chaseSpeed = 4f;

    [Header("Damage")]
    public float damageAmount = 40f;
    public float damageCooldown = 1f;

    public enum State { Patrol, Chase }
    [Header("State")]
    public State currentState = State.Patrol;

    private Vector2 _startPos;
    private Vector2 _target;
    private Rigidbody2D _rb;
    private Transform _player;
    private float _damageTimer;
    private SpriteRenderer _sprite;

    private void Awake()
    {
        _startPos = transform.position;
        _target = _startPos + pointA;
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>()?.transform;
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        if (_player == null)
        {
            _player = FindObjectOfType<PlayerController>()?.transform;
            if (_player == null) return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);
        currentState = dist <= detectionRadius ? State.Chase : State.Patrol;

        Vector2 moveDir;
        if (currentState == State.Chase)
        {
            moveDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.velocity = moveDir * chaseSpeed;
        }
        else
        {
            if (Vector2.Distance(transform.position, _target) < 0.2f)
                _target = Vector2.Distance(_target, _startPos + pointA) < 0.1f
                    ? _startPos + pointB
                    : _startPos + pointA;

            moveDir = ((Vector2)_target - (Vector2)transform.position).normalized;
            _rb.velocity = moveDir * patrolSpeed;
        }

        if (_sprite)
            _sprite.flipX = _rb.velocity.x < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void TryDamage(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < _damageTimer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health)
        {
            health.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
            _damageTimer = Time.time + damageCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            _startPos = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(_startPos + pointA, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(_startPos + pointB, Vector3.one * 0.4f);
        Gizmos.DrawLine(_startPos + pointA, _startPos + pointB);
    }
}
