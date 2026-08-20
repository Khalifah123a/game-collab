using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("Damage Trigger")]
    public float damageTriggerRadius = 1.5f;

    public enum State { Patrol, Chase }
    [Header("State")]
    public State currentState = State.Patrol;

    private Vector2 _startPos;
    private Vector2 _target;
    private Rigidbody2D _rb;
    private Transform _player;
    private float _damageTimer;
    private SpriteRenderer _sprite;
    private CircleCollider2D _damageTrigger;

    private void Awake()
    {
        _startPos = transform.position;
        _target = _startPos + pointA;
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        _damageTrigger = GetComponent<CircleCollider2D>();
        if (_damageTrigger == null)
        {
            _damageTrigger = gameObject.AddComponent<CircleCollider2D>();
            _damageTrigger.isTrigger = true;
            _damageTrigger.radius = damageTriggerRadius;
        }
        else
        {
            _damageTrigger.isTrigger = true;
        }
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
            float dirX = Mathf.Sign(_player.position.x - transform.position.x);
            moveDir = new Vector2(dirX, 0f);
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < _damageTimer) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageTriggerRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(_startPos + pointA, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(_startPos + pointB, Vector3.one * 0.4f);
        Gizmos.DrawLine(_startPos + pointA, _startPos + pointB);
    }
}
