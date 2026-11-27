using System.Collections;
using UnityEngine;

/// <summary>
/// Firewall Sentry Turret (Stateful Packet Inspection Sentry).
/// Educational Network Concept: Simulates SPI firewalls that inspect datagram headers
/// and deploy active countermeasures against unauthorized packet traversal.
/// </summary>
public class Enemy_FirewallTurret : MonoBehaviour, IEnemyDamageable
{
    [Header("Turret Config")]
    public int hitPoints = 2;
    public float detectionRange = 10f;
    public float windupTime = 0.55f;
    public float fireCooldown = 1.8f;
    public float projectileSpeed = 14f;
    public float damageAmount = 15f;
    public int facingDirection = -1; // -1 for Left, 1 for Right

    [Header("Custom Sprite & Animasi (Opsional — Drag Asset Sendiri)")]
    public Sprite customSprite;
    public SpriteAnimationProfile customProfile;
    public ModularSpriteAnimator customSpriteAnimator;

    [Header("State")]
    public bool isTargeting;
    public bool isDead;

    private float _timer;
    private Transform _player;
    private SpriteRenderer _sprite;
    private LineRenderer _laserSight;

    public bool IsDead => isDead;
    public Transform HitTransform => transform;

    private void Awake()
    {
        _sprite = GetComponentInChildren<SpriteRenderer>();
        if (_sprite == null)
        {
            var go = new GameObject("TurretSprite");
            go.transform.SetParent(transform, false);
            _sprite = go.AddComponent<SpriteRenderer>();
            _sprite.sprite = Resources.Load<Sprite>("GeneratedArt/server_rack");
            _sprite.color = new Color(1f, 0.35f, 0.25f, 1f); // Firewall red-orange
        }

        // Laser sight telegraph line
        _laserSight = gameObject.AddComponent<LineRenderer>();
        _laserSight.startWidth = 0.04f;
        _laserSight.endWidth = 0.04f;
        _laserSight.positionCount = 2;
        _laserSight.material = new Material(Shader.Find("Sprites/Default"));
        _laserSight.startColor = new Color(0f, 1f, 0.8f, 0.25f);
        _laserSight.endColor = new Color(0f, 1f, 0.8f, 0.25f);
        _laserSight.sortingOrder = 40;
        _laserSight.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(1.2f, 1.8f);
        }
    }

    private void Start()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) _player = pc.transform;
        ApplyCustomArtSetup();
    }

    private void ApplyCustomArtSetup()
    {
        if (customSpriteAnimator == null) customSpriteAnimator = GetComponent<ModularSpriteAnimator>();

        SpriteAnimationProfile resolved = (customProfile != null && customProfile.HasAnyCustomArt())
            ? customProfile
            : Resources.Load<SpriteAnimationProfile>("CustomAnimations/Enemy_FirewallTurret");
        if (resolved != null && !resolved.HasAnyCustomArt()) resolved = null;

        bool inlineHasArt = customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt();
        if (resolved != null)
        {
            if (customSpriteAnimator == null) customSpriteAnimator = gameObject.AddComponent<ModularSpriteAnimator>();
            customSpriteAnimator.targetRenderer = _sprite;
            customSpriteAnimator.ApplyProfile(resolved, overwriteExisting: !inlineHasArt);
        }

        if (customSpriteAnimator != null && _sprite != null)
        {
            customSpriteAnimator.targetRenderer = _sprite;
            customSpriteAnimator.EnsureRenderer();
            if (customSprite != null && customSpriteAnimator.defaultSprite == null)
                customSpriteAnimator.defaultSprite = customSprite;
            if (customSpriteAnimator.HasAnyCustomArt())
            {
                _sprite.color = Color.white;
                if (!customSpriteAnimator.PlaySmart("Idle", 1, "Walk"))
                {
                    Sprite fb = customSpriteAnimator.GetFallbackSprite();
                    if (fb != null) _sprite.sprite = fb;
                }
            }
        }
        else if (customSprite != null && _sprite != null)
        {
            _sprite.sprite = customSprite;
            _sprite.color = Color.white;
        }
    }

    private void Update()
    {
        if (isDead || Time.timeScale == 0f) return;

        if (_player == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) _player = pc.transform;
            return;
        }

        if (SafeZone.IsPlayerSafe)
        {
            _laserSight.enabled = false;
            return;
        }

        Vector2 origin = transform.position;
        Vector2 target = _player.position;
        Vector2 dir = target - origin;

        // Check if player is on the facing side and within line-of-sight range
        bool inFacingArc = (facingDirection > 0 && dir.x > 0) || (facingDirection < 0 && dir.x < 0);
        float dist = dir.magnitude;

        if (inFacingArc && dist <= detectionRange)
        {
            // Check line of sight
            var hit = Physics2D.Raycast(origin, dir.normalized, dist);
            if (hit.collider == null || hit.collider.GetComponent<PlayerController>() != null)
            {
                // Clear sight line to player!
                _laserSight.enabled = true;
                _laserSight.SetPosition(0, origin);
                _laserSight.SetPosition(1, target);

                _timer += Time.deltaTime;
                if (!isTargeting)
                {
                    isTargeting = true;
                    _laserSight.startColor = new Color(1f, 0.2f, 0.1f, 0.8f);
                    _laserSight.endColor = new Color(1f, 0.2f, 0.1f, 0.8f);
                    AudioManager.Play("sfx_shoot", 0.5f, 1.8f);
                    if (customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt())
                        customSpriteAnimator.PlaySmart("Windup", 1, "Attack", "Idle");
                }

                if (_timer >= windupTime)
                {
                    FireFirewallLaser(dir.normalized);
                    _timer = -fireCooldown;
                    isTargeting = false;
                    _laserSight.enabled = false;
                    if (customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt())
                        customSpriteAnimator.PlaySmart("Attack", 1, "Idle");
                }
                return;
            }
        }

        // Lost sight
        if (isTargeting && customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt())
            customSpriteAnimator.PlaySmart("Idle", 1, "Walk");
        isTargeting = false;
        _laserSight.enabled = false;
        _timer = Mathf.Max(0f, _timer - Time.deltaTime);
    }

    private void FireFirewallLaser(Vector2 direction)
    {
        AudioManager.Play("sfx_attack", 0.8f, 1.5f);
        JuiceManager.Shake(0.18f);

        // Spawn firewall projectile
        var projGO = new GameObject("FirewallBeam");
        projGO.transform.position = transform.position + (Vector3)(direction * 0.5f);

        var sr = projGO.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/key_e_ring");
        sr.color = new Color(1f, 0.2f, 0.1f, 1f);
        sr.sortingOrder = 55;
        projGO.transform.localScale = new Vector3(0.6f, 0.25f, 1f);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projGO.transform.rotation = Quaternion.Euler(0, 0, angle);

        var col = projGO.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        var beam = projGO.AddComponent<FirewallBeamMover>();
        beam.direction = direction;
        beam.speed = projectileSpeed;
        beam.damage = damageAmount;
    }

    public void TakeHit(int damage)
    {
        if (isDead || damage <= 0) return;

        hitPoints = Mathf.Max(0, hitPoints - damage);
        JuiceManager.Hitstop(0.04f);
        JuiceManager.SpawnImpactSparks(transform.position, new Color(1f, 0.4f, 0.2f, 1f));

        if (hitPoints == 0)
        {
            isDead = true;
            _laserSight.enabled = false;

            JuiceManager.Shake(0.35f);
            JuiceManager.SpawnDataDissolve(transform.position, 16, new Color(1f, 0.25f, 0.1f, 1f));
            AudioManager.PlayEnemyDissolve();

            if (GameManager.Instance != null)
            {
                OptionalQuestService.ReportKill(GameManager.Instance.currentWorldIndex, "enemy:firewall_turret", gameObject.GetHashCode().ToString());
            }

            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            Destroy(gameObject, 0.15f);
        }
    }
}

/// <summary>
/// Moves firewall laser projectile, applies damage to player, and cleans up on impact.
/// </summary>
public class FirewallBeamMover : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 14f;
    public float damage = 15f;
    private float _lifeTimer = 2.0f;

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;

        var player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage, PlayerHealth.DamageType.TTLDamage);
            Destroy(gameObject);
            return;
        }

        // Destroy on solid obstacles
        if (!other.GetComponent<Enemy_FirewallTurret>())
        {
            JuiceManager.SpawnImpactSparks(transform.position, new Color(1f, 0.3f, 0.1f, 1f));
            Destroy(gameObject);
        }
    }
}
