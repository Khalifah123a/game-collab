using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World 4 Mini-Boss: Solaris - Superheated Thermal Processor Core.
/// Guarding the transition to Server B in World 4 Segment 5 (Solaris Core).
/// 
/// Attack Patterns:
/// 1. Solar Flare Barrage: Multi-directional plasma projectiles radiating toward player.
/// 2. Overheat Thermal Sweep: Searing focused laser beam tracking player position.
/// 3. Corona Nova (Phase 2): Full-arena thermal pulse requiring vertical platform evasion.
/// 
/// Reward: +300 DataBits and unblocks Server B goal.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss_Solaris : MonoBehaviour, IBossController
{
    public static Boss_Solaris Instance { get; private set; }

    [Header("Boss Stats")]
    public string bossName = "SOLARIS // THERMAL PROCESSOR";
    public int maxHitPoints = 8;
    public int hitPoints = 8;
    public float hoverSpeed = 1.5f;
    public float damageAmount = 18f;

    [Header("Arena Bounds")]
    public float arenaMinX = 230f;
    public float arenaMaxX = 270f;
    public float arenaY = -8f;

    public string BossName => bossName;
    public int HitPoints => hitPoints;
    public int MaxHitPoints => maxHitPoints;
    public Transform HitTransform => transform;
    public bool IsDead => hitPoints <= 0;
    public float HealthPercent => Mathf.Clamp01((float)hitPoints / maxHitPoints);

    private Rigidbody2D _body;
    private SpriteRenderer _coreRend;
    private Transform _coronaRing;
    private Transform _laserBeam;
    private SpriteRenderer _laserRend;
    private TextMesh _warning;

    private float _attackTimer = 2.8f;
    private float _invulnTimer = 0f;
    private int _phase = 1;
    private Vector2 _home;
    private bool _isAttacking;

    private void Awake()
    {
        Instance = this;
        BossTracker.Register(this);
        _home = transform.position;
        arenaY = _home.y;
        if (_home.x > 50f)
        {
            arenaMinX = _home.x - 20f;
            arenaMaxX = _home.x + 20f;
        }

        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0;

        foreach (var col in GetComponents<Collider2D>()) col.isTrigger = true;
        var circle = GetComponent<CircleCollider2D>();
        if (circle == null) circle = gameObject.AddComponent<CircleCollider2D>();
        circle.radius = 1.4f;

        BuildVisuals();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        BossTracker.Unregister(this);
    }

    private void Update()
    {
        if (IsDead) return;

        if (_invulnTimer > 0)
        {
            _invulnTimer -= Time.deltaTime;
            if (_coreRend) _coreRend.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 20f, 1f));
        }
        else if (_coreRend)
        {
            _coreRend.color = _phase == 2 ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.5f, 0.1f);
        }

        // Corona spin and pulse
        if (_coronaRing != null)
        {
            float rotSpeed = _phase == 2 ? 160f : 80f;
            _coronaRing.Rotate(0f, 0f, rotSpeed * Time.deltaTime);
            float scale = 1.0f + 0.15f * Mathf.Sin(Time.time * 6f);
            _coronaRing.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        var player = PlayerHealth.Instance;
        if (player == null || player.isDead)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        // Floating sinusoidal movement
        float targetX = Mathf.Lerp(arenaMinX + 3f, arenaMaxX - 3f, (Mathf.Sin(Time.time * hoverSpeed) + 1f) * 0.5f);
        float targetY = arenaY + Mathf.Sin(Time.time * hoverSpeed * 2.5f) * 1.6f;
        Vector2 targetPos = new Vector2(targetX, targetY);
        Vector2 moveDir = targetPos - _body.position;
        _body.linearVelocity = moveDir * 3.5f;

        // Attack cooldown
        _attackTimer -= Time.fixedDeltaTime;
        if (_attackTimer <= 0f && !_isAttacking)
        {
            StartCoroutine(ExecuteAttackPattern());
        }

        // Contact damage
        Vector2 delta = player.transform.position - transform.position;
        if (delta.sqrMagnitude < 3.2f && !player.IsInvulnerable && !SafeZone.IsPlayerSafe)
        {
            player.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.Knockback(new Vector2(Mathf.Sign(delta.x), 0.5f));
        }
    }

    private IEnumerator ExecuteAttackPattern()
    {
        _isAttacking = true;
        if (_warning)
        {
            _warning.text = "[!] THERMAL CRITICAL";
            _warning.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(_phase == 2 ? 0.4f : 0.7f);
        if (_warning) _warning.gameObject.SetActive(false);

        if (IsDead) { _isAttacking = false; yield break; }

        if (Random.value < 0.5f)
        {
            // Attack 1: Solar Flare Barrage
            yield return StartCoroutine(FireSolarFlareBarrage());
        }
        else
        {
            // Attack 2: Focused Thermal Beam
            yield return StartCoroutine(FireThermalBeam());
        }

        _isAttacking = false;
        _attackTimer = _phase == 2 ? 1.8f : 2.8f;
    }

    private IEnumerator FireSolarFlareBarrage()
    {
        var player = PlayerHealth.Instance;
        Vector2 target = player != null ? (Vector2)player.transform.position : (Vector2)transform.position + Vector2.down;
        Vector2 baseDir = (target - (Vector2)transform.position).normalized;

        int count = _phase == 2 ? 7 : 5;
        float spread = 45f;
        float startAngle = -spread * 0.5f;
        float step = spread / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
            SpawnSolarProjectile(dir);
        }

        yield return new WaitForSeconds(0.4f);
    }

    private void SpawnSolarProjectile(Vector2 dir)
    {
        var proj = new GameObject("Solar_Flare_Proj");
        proj.transform.position = transform.position + (Vector3)dir * 1.2f;
        var rend = proj.AddComponent<SpriteRenderer>();
        rend.sprite = CreateCircleSprite(24, new Color(1f, 0.6f, 0.1f));
        rend.sortingOrder = 8;
        var col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;
        var mover = proj.AddComponent<SolarProjectileMover>();
        mover.direction = dir;
        mover.speed = _phase == 2 ? 8.5f : 6.5f;
    }

    private IEnumerator FireThermalBeam()
    {
        if (_laserBeam != null && _laserRend != null)
        {
            var player = PlayerHealth.Instance;
            if (player != null)
            {
                Vector2 dir = (player.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _laserBeam.rotation = Quaternion.Euler(0, 0, angle);
            }

            _laserBeam.gameObject.SetActive(true);
            _laserRend.color = new Color(1f, 0.8f, 0.2f, 0.9f);

            float duration = _phase == 2 ? 1.0f : 1.4f;
            float elapsed = 0f;
            while (elapsed < duration && !IsDead)
            {
                elapsed += Time.deltaTime;
                CheckBeamHit();
                yield return null;
            }

            _laserBeam.gameObject.SetActive(false);
        }
    }

    private void CheckBeamHit()
    {
        var player = PlayerHealth.Instance;
        if (player == null || player.isDead || player.IsInvulnerable || SafeZone.IsPlayerSafe) return;

        Vector2 beamDir = _laserBeam.right;
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float proj = Vector2.Dot(toPlayer, beamDir);

        if (proj > 0f && proj < 30f)
        {
            Vector2 closest = (Vector2)transform.position + beamDir * proj;
            float dist = Vector2.Distance(player.transform.position, closest);
            if (dist < 0.9f)
            {
                player.TakeDamage(14f, PlayerHealth.DamageType.TTLDamage);
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.Knockback(new Vector2(beamDir.x, 0.4f));
            }
        }
    }

    public void TakeHit(int damage)
    {
        if (damage <= 0 || IsDead || _invulnTimer > 0f) return;

        hitPoints = Mathf.Max(0, hitPoints - damage);
        _invulnTimer = 0.35f;

        if (hitPoints <= 4 && _phase == 1)
        {
            _phase = 2;
            hoverSpeed = 2.4f;
        }

        if (hitPoints <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        hitPoints = 0;
        _body.linearVelocity = Vector2.zero;
        if (_laserBeam) _laserBeam.gameObject.SetActive(false);
        if (_warning) _warning.gameObject.SetActive(false);

        GameManager.Instance?.AddDataBits(300);
        BossTracker.Unregister(this);

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / 0.8f);
            if (_coreRend) _coreRend.color = Color.Lerp(Color.yellow, Color.clear, t / 0.8f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void BuildVisuals()
    {
        var coreGo = new GameObject("SolarCore");
        coreGo.transform.SetParent(transform, false);
        _coreRend = coreGo.AddComponent<SpriteRenderer>();
        _coreRend.sprite = CreateCircleSprite(64, new Color(1f, 0.5f, 0.1f));
        _coreRend.sortingOrder = 5;

        var coronaGo = new GameObject("CoronaRing");
        coronaGo.transform.SetParent(transform, false);
        _coronaRing = coronaGo.transform;
        for (int i = 0; i < 6; i++)
        {
            var flare = new GameObject("Flare_" + i);
            flare.transform.SetParent(_coronaRing, false);
            float a = i * 60f * Mathf.Deg2Rad;
            flare.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.5f, Mathf.Sin(a) * 1.5f, 0f);
            var fr = flare.AddComponent<SpriteRenderer>();
            fr.sprite = CreateCircleSprite(20, new Color(1f, 0.8f, 0.2f, 0.75f));
            fr.sortingOrder = 4;
        }

        var beamGo = new GameObject("ThermalBeam");
        beamGo.transform.SetParent(transform, false);
        beamGo.transform.localPosition = Vector3.zero;
        beamGo.transform.localScale = new Vector3(30f, 0.4f, 1f);
        _laserRend = beamGo.AddComponent<SpriteRenderer>();
        _laserRend.sprite = CreateCircleSprite(32, new Color(1f, 0.8f, 0.2f, 0.9f));
        _laserRend.sortingOrder = 7;
        beamGo.SetActive(false);
        _laserBeam = beamGo.transform;

        var warnGo = new GameObject("WarningHUD");
        warnGo.transform.SetParent(transform, false);
        warnGo.transform.localPosition = new Vector3(0, 2.5f, 0);
        _warning = warnGo.AddComponent<TextMesh>();
        _warning.text = "[!] THERMAL CRITICAL";
        _warning.characterSize = 0.25f;
        _warning.fontSize = 24;
        _warning.anchor = TextAnchor.MiddleCenter;
        _warning.alignment = TextAlignment.Center;
        _warning.color = new Color(1f, 0.6f, 0.1f);
        warnGo.SetActive(false);
    }

    private static Sprite CreateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d <= r)
                {
                    float fade = 1f - Mathf.Clamp01(d / r);
                    Color col = color;
                    col.a *= fade * 1.2f;
                    tex.SetPixel(x, y, col);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }
}

public class SolarProjectileMover : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 7f;
    private float _life = 3.5f;

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        _life -= Time.deltaTime;
        if (_life <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerHealth>();
        if (player != null && !player.isDead && !player.IsInvulnerable && !SafeZone.IsPlayerSafe)
        {
            player.TakeDamage(12f, PlayerHealth.DamageType.TTLDamage);
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.Knockback(new Vector2(direction.x, 0.5f));
            Destroy(gameObject);
        }
    }
}
