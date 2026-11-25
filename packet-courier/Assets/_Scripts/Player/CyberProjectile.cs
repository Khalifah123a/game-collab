using UnityEngine;

/// <summary>
/// High-speed photon data projectile fired by courier Cyber Blaster.
/// Damages enemies (patrol, flying, boss) upon impact.
/// </summary>
[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class CyberProjectile : MonoBehaviour
{
    public float speed = 24f;
    public int damage = 2;
    public float lifetime = 2.0f;
    public float facing = 1f;

    private Rigidbody2D _rb;
    private SpriteRenderer _renderer;
    private float _spawnTime;

    public static CyberProjectile Spawn(Vector2 position, float direction, int hitDamage)
    {
        var go = new GameObject("CyberProjectile", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CyberProjectile));
        go.transform.position = position;
        var p = go.GetComponent<CyberProjectile>();
        p.facing = Mathf.Sign(direction);
        p.damage = hitDamage;
        return p;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sprite = Resources.Load<Sprite>("GeneratedArt/projectile_databased");
        _renderer.sortingOrder = 35;
        _spawnTime = Time.time;
    }

    private void Start()
    {
        _rb.linearVelocity = new Vector2(facing * speed, 0f);
        transform.localScale = new Vector3(facing, 1f, 1f);
    }

    private void Update()
    {
        if (Time.time - _spawnTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<PlayerController>() != null) return; // Don't hit self
        if (other.isTrigger && other.GetComponentInParent(typeof(IEnemyDamageable)) == null) return; // Skip safe triggers

        var enemy = other.GetComponentInParent(typeof(IEnemyDamageable)) as IEnemyDamageable;
        if (enemy != null && !enemy.IsDead)
        {
            enemy.TakeHit(damage);
            JuiceManager.Hitstop(0.035f);
            JuiceManager.Shake(0.15f);
            JuiceManager.SpawnImpactSparks(transform.position, new Color(0f, 1f, 0.85f, 1f));
            SpawnHitSparks(transform.position);
            Destroy(gameObject);
            return;
        }

        // Hit solid terrain / ground
        if (!other.isTrigger)
        {
            SpawnHitSparks(transform.position);
            Destroy(gameObject);
        }
    }

    private static void SpawnHitSparks(Vector2 pos)
    {
        var fxGo = new GameObject("BlasterHitSparks");
        fxGo.transform.position = pos;
        var lr = fxGo.AddComponent<LineRenderer>();
        lr.positionCount = 4;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.2f, 1f, 0.9f, 1f);
        lr.endColor = new Color(1f, 0.3f, 0.9f, 0f);
        lr.sortingOrder = 40;
        lr.SetPosition(0, pos);
        lr.SetPosition(1, pos + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.4f)));
        lr.SetPosition(2, pos + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.3f)));
        lr.SetPosition(3, pos);
        Destroy(fxGo, 0.18f);
    }
}
