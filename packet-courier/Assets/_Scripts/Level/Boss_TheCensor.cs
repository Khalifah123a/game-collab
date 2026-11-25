using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World 2 Mini-Boss: The Censor - Network Surveillance Monolith.
/// Guarding the transition to Server B in World 2 Segment 4.
/// 
/// Attack Patterns:
/// 1. Data Redaction Sweep: Telegraphed horizontal laser beam across floor/mid altitude.
/// 2. Censorship Slam: Vertical drop creating bilateral firewall shockwaves.
/// 
/// Phases:
/// - Phase 1 (6-4 HP): Methodical sweeps and slams.
/// - Phase 2 (3-1 HP): Overclocked speed, twin redaction pulses, rapid recovery.
/// 
/// Reward: +200 DataBits and unblocks Server B goal.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss_TheCensor : MonoBehaviour, IBossController
{
    public static Boss_TheCensor Instance { get; private set; }

    [Header("Boss Stats")]
    public string bossName = "THE CENSOR // SURVEILLANCE";
    public int maxHitPoints = 6;
    public int hitPoints = 6;
    public float hoverSpeed = 1.4f;
    public float damageAmount = 15f;

    [Header("Arena Bounds")]
    public float arenaMinX = 310f;
    public float arenaMaxX = 345f;
    public float arenaY = -8f;

    public string BossName => bossName;
    public int HitPoints => hitPoints;
    public int MaxHitPoints => maxHitPoints;
    public Transform HitTransform => transform;
    public bool IsDead => hitPoints <= 0;
    public float HealthPercent => Mathf.Clamp01((float)hitPoints / maxHitPoints);

    private Rigidbody2D _body;
    private SpriteRenderer _coreRend;
    private SpriteRenderer _opticRend;
    private Transform _scannerBeam;
    private SpriteRenderer _beamRend;
    private TextMesh _warning;

    private float _attackTimer = 2.5f;
    private float _invulnTimer = 0f;
    private int _phase = 1;
    private Vector2 _home;
    private bool _isAttacking;
    private bool _slamming;

    private void Awake()
    {
        Instance = this;
        BossTracker.Register(this);
        _home = transform.position;
        arenaY = _home.y;
        if (_home.x > 50f)
        {
            arenaMinX = _home.x - 18f;
            arenaMaxX = _home.x + 18f;
        }

        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0;

        foreach (var col in GetComponents<Collider2D>()) col.isTrigger = true;
        var box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2f, 3.2f);

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
            _coreRend.color = _phase == 2 ? new Color(1f, 0.3f, 0.4f) : Color.white;
        }

        // Optic tracking
        var player = PlayerHealth.Instance;
        if (player != null && _opticRend != null)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            _opticRend.transform.localPosition = new Vector3(dir.x * 0.2f, 0.5f + dir.y * 0.15f, -0.1f);
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * (_phase == 2 ? 10f : 5f));
            _opticRend.color = new Color(1f, 0.1f, 0.3f, pulse);
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

        // Horizontal surveillance patrol
        if (!_slamming)
        {
            float targetX = Mathf.Lerp(arenaMinX + 3f, arenaMaxX - 3f, (Mathf.Sin(Time.time * hoverSpeed) + 1f) * 0.5f);
            float targetY = arenaY + Mathf.Sin(Time.time * hoverSpeed * 2.2f) * 1.4f;
            Vector2 targetPos = new Vector2(targetX, targetY);
            Vector2 moveDir = targetPos - _body.position;
            _body.linearVelocity = moveDir * 3.2f;
        }

        // Attack cycle
        _attackTimer -= Time.fixedDeltaTime;
        if (_attackTimer <= 0f && !_isAttacking)
        {
            StartCoroutine(ExecuteAttackPattern());
        }

        // Collision damage
        Vector2 delta = player.transform.position - transform.position;
        if (delta.sqrMagnitude < 3.0f && !player.IsInvulnerable && !SafeZone.IsPlayerSafe)
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
            _warning.text = "[!] REDACTING DATA";
            _warning.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(_phase == 2 ? 0.45f : 0.7f);
        if (_warning) _warning.gameObject.SetActive(false);

        if (IsDead) { _isAttacking = false; yield break; }

        if (Random.value < 0.55f)
        {
            // Attack 1: Redaction Horizontal Laser Sweep
            yield return StartCoroutine(PerformRedactionSweep());
        }
        else
        {
            // Attack 2: Censorship Slam
            yield return StartCoroutine(PerformCensorshipSlam());
        }

        _isAttacking = false;
        _attackTimer = _phase == 2 ? 1.6f : 2.5f;
    }

    private IEnumerator PerformRedactionSweep()
    {
        if (_scannerBeam != null && _beamRend != null)
        {
            _scannerBeam.gameObject.SetActive(true);
            _beamRend.color = new Color(1f, 0.1f, 0.2f, 0.85f);
            float duration = _phase == 2 ? 1.2f : 1.6f;
            float elapsed = 0f;

            while (elapsed < duration && !IsDead)
            {
                elapsed += Time.deltaTime;
                CheckLaserDamage();
                yield return null;
            }

            _scannerBeam.gameObject.SetActive(false);
        }
    }

    private void CheckLaserDamage()
    {
        var player = PlayerHealth.Instance;
        if (player == null || player.isDead || player.IsInvulnerable || SafeZone.IsPlayerSafe) return;

        // Laser ray reaches across arena at boss's current Y height
        float dy = Mathf.Abs(player.transform.position.y - transform.position.y);
        if (dy < 0.9f)
        {
            player.TakeDamage(12f, PlayerHealth.DamageType.TTLDamage);
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.Knockback(new Vector2(player.transform.position.x > transform.position.x ? 1f : -1f, 0.4f));
        }
    }

    private IEnumerator PerformCensorshipSlam()
    {
        _slamming = true;
        _body.linearVelocity = Vector2.zero;

        // Surge up
        Vector2 surgeTarget = new Vector2(transform.position.x, arenaY + 3.5f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(transform.position, surgeTarget, t / 0.4f);
            yield return null;
        }

        // Slam down
        Vector2 slamTarget = new Vector2(transform.position.x, arenaY - 2.5f);
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(transform.position, slamTarget, t / 0.25f);
            yield return null;
        }

        // Spawn ground shockwaves
        SpawnFirewallWave(Vector2.left);
        SpawnFirewallWave(Vector2.right);

        yield return new WaitForSeconds(0.4f);
        _slamming = false;
    }

    private void SpawnFirewallWave(Vector2 dir)
    {
        var wave = new GameObject("Censor_Wave");
        wave.transform.position = transform.position + (Vector3)dir * 1.2f;
        var rend = wave.AddComponent<SpriteRenderer>();
        rend.sprite = CreateBoxSprite(32, 32, new Color(1f, 0.15f, 0.35f));
        rend.sortingOrder = 8;
        var col = wave.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1.2f);
        var mover = wave.AddComponent<CensorWaveMover>();
        mover.direction = dir;
        mover.speed = _phase == 2 ? 8.5f : 6.0f;
    }

    public void TakeHit(int damage)
    {
        if (damage <= 0 || IsDead || _invulnTimer > 0f) return;

        hitPoints = Mathf.Max(0, hitPoints - damage);
        _invulnTimer = 0.35f;

        if (hitPoints <= 3 && _phase == 1)
        {
            _phase = 2;
            hoverSpeed = 2.1f;
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
        if (_scannerBeam) _scannerBeam.gameObject.SetActive(false);
        if (_warning) _warning.gameObject.SetActive(false);

        GameManager.Instance?.AddDataBits(200);
        AchievementManager.Unlock("ACH_FIREWALL_BYPASS");
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
            if (_coreRend) _coreRend.color = Color.Lerp(Color.red, Color.clear, t / 0.8f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void BuildVisuals()
    {
        var coreGo = new GameObject("MonolithCore");
        coreGo.transform.SetParent(transform, false);
        _coreRend = coreGo.AddComponent<SpriteRenderer>();
        _coreRend.sprite = CreateBoxSprite(48, 80, new Color(0.12f, 0.14f, 0.22f));
        _coreRend.sortingOrder = 5;

        var opticGo = new GameObject("OpticSensor");
        opticGo.transform.SetParent(transform, false);
        opticGo.transform.localPosition = new Vector3(0f, 0.5f, -0.1f);
        _opticRend = opticGo.AddComponent<SpriteRenderer>();
        _opticRend.sprite = CreateBoxSprite(16, 16, new Color(1f, 0.1f, 0.3f));
        _opticRend.sortingOrder = 6;

        var beamGo = new GameObject("ScannerBeam");
        beamGo.transform.SetParent(transform, false);
        beamGo.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        beamGo.transform.localScale = new Vector3(40f, 0.3f, 1f);
        _beamRend = beamGo.AddComponent<SpriteRenderer>();
        _beamRend.sprite = CreateBoxSprite(32, 32, new Color(1f, 0.1f, 0.2f, 0.85f));
        _beamRend.sortingOrder = 7;
        beamGo.SetActive(false);
        _scannerBeam = beamGo.transform;

        var warnGo = new GameObject("TelegraphWarning");
        warnGo.transform.SetParent(transform, false);
        warnGo.transform.localPosition = new Vector3(0, 2.4f, 0);
        _warning = warnGo.AddComponent<TextMesh>();
        _warning.text = "[!] REDACTING DATA";
        _warning.characterSize = 0.25f;
        _warning.fontSize = 24;
        _warning.anchor = TextAnchor.MiddleCenter;
        _warning.alignment = TextAlignment.Center;
        _warning.color = new Color(1f, 0.2f, 0.3f);
        warnGo.SetActive(false);
    }

    private static Sprite CreateBoxSprite(int w, int h, Color color)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color border = color * 1.35f; border.a = color.a;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                tex.SetPixel(x, y, edge ? border : color);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }
}

public class CensorWaveMover : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 6f;
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
            player.TakeDamage(10f, PlayerHealth.DamageType.TTLDamage);
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.Knockback(new Vector2(direction.x, 0.6f));
        }
    }
}
