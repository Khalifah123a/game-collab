using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World 5 Boss: Boss Akhir (Modular Rootkit Core).
/// Implements IBossController with multi-phase attack patterns:
/// Phase 1 (10-7 HP): Tracking hover and horizontal laser barrage sweeps.
/// Phase 2 (6-4 HP): Arena shockwave slams and defensive minion summons.
/// Phase 3 (3-0 HP): Overdrive emergency storm with rapid spread pulses.
/// Defeating the boss awards 500 bonus DataBits and unlocks the Server B exit goal.
/// Separates boss identity, dialogue, and visuals so they can be easily replaced.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss_Rootkit : MonoBehaviour, IBossController
{
    public static Boss_Rootkit Instance { get; private set; }

    [Header("Modular Boss Identity")]
    public string bossName = "Boss Akhir";
    public string titleSubtitle = "Rootkit Core // Final Signal";
    [TextArea(2, 4)]
    public string introDialogue = "Sinyal terlarang terdeteksi. Protokol pemusnahan diaktifkan.";
    [TextArea(2, 4)]
    public string defeatDialogue = "Koneksi terputus... Sistem inti berhasil dipulihkan.";

    [Header("Boss Stats")]
    public int maxHitPoints = 10;
    public int hitPoints = 10;
    public float hoverSpeed = 1.2f;
    public float damageAmount = 20f;

    [Header("Arena Bounds")]
    public float arenaMinX = 405f;
    public float arenaMaxX = 445f;
    public float arenaY = -8f;

    public string BossName => bossName;
    public int HitPoints => hitPoints;
    public int MaxHitPoints => maxHitPoints;
    public Transform HitTransform => transform;
    public bool IsDead => hitPoints <= 0;
    public float HealthPercent => Mathf.Clamp01((float)hitPoints / maxHitPoints);

    private Rigidbody2D _body;
    private SpriteRenderer _coreRend;
    private SpriteRenderer _eyeRend;
    private Transform _shieldRing;
    private SpriteRenderer[] _shieldSegments;
    private TextMesh _warning;

    private float _attackTimer = 2f;
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
            arenaMinX = _home.x - 20f;
            arenaMaxX = _home.x + 20f;
        }

        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0;

        foreach (var col in GetComponents<Collider2D>()) col.isTrigger = true;

        BuildBossVisuals();
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
            _coreRend.color = _phase == 3 ? new Color(1f, 0.2f, 0.2f) : _phase == 2 ? new Color(1f, 0.6f, 0.2f) : Color.white;
        }

        // Orbit shield rotation
        if (_shieldRing != null)
        {
            float rotSpeed = _phase == 3 ? 180f : _phase == 2 ? 110f : 60f;
            _shieldRing.Rotate(0f, 0f, rotSpeed * Time.deltaTime);
        }

        // Eye tracks player
        var player = PlayerHealth.Instance;
        if (player != null && _eyeRend != null)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            _eyeRend.transform.localPosition = (Vector3)(dir * 0.25f);
            float eyeGlow = 0.8f + 0.2f * Mathf.Sin(Time.time * (_phase * 6f));
            _eyeRend.color = new Color(1f, 0.1f, 0.1f, eyeGlow);
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

        // Hover movement
        if (!_slamming)
        {
            float targetX = Mathf.Lerp(arenaMinX + 4f, arenaMaxX - 4f, (Mathf.Sin(Time.time * hoverSpeed) + 1f) * 0.5f);
            float targetY = arenaY + Mathf.Sin(Time.time * hoverSpeed * 2f) * 1.8f;
            Vector2 targetPos = new Vector2(targetX, targetY);
            Vector2 moveDir = targetPos - _body.position;
            _body.linearVelocity = moveDir * 3.5f;
        }

        // Attack timing
        _attackTimer -= Time.fixedDeltaTime;
        if (_attackTimer <= 0f && !_isAttacking)
        {
            StartCoroutine(ExecuteAttackPattern());
        }

        // Contact damage to player
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
        if (_warning) _warning.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.6f);
        if (_warning) _warning.gameObject.SetActive(false);

        if (IsDead) { _isAttacking = false; yield break; }

        if (_phase == 1)
        {
            // Phase 1: Triple energy pulse toward player
            for (int i = 0; i < 3; i++)
            {
                FireEnergyPulse();
                yield return new WaitForSeconds(0.35f);
            }
            _attackTimer = 2.5f;
        }
        else if (_phase == 2)
        {
            // Phase 2: Ground Slam or Drone Support
            if (Random.value > 0.5f)
            {
                yield return StartCoroutine(GroundSlam());
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    FireEnergyPulse();
                    yield return new WaitForSeconds(0.25f);
                }
            }
            _attackTimer = 2.0f;
        }
        else
        {
            // Phase 3 Overdrive: Rapid 5-way pulse storm
            for (int i = 0; i < 5; i++)
            {
                FireEnergyPulse();
                yield return new WaitForSeconds(0.18f);
            }
            _attackTimer = 1.4f;
        }

        _isAttacking = false;
    }

    private IEnumerator GroundSlam()
    {
        _slamming = true;
        var player = PlayerHealth.Instance;
        float slamX = player != null ? Mathf.Clamp(player.transform.position.x, arenaMinX + 2f, arenaMaxX - 2f) : transform.position.x;

        // Hover up telegraph
        _body.linearVelocity = new Vector2((slamX - transform.position.x) * 4f, 8f);
        yield return new WaitForSeconds(0.4f);

        // Slam down fast
        _body.linearVelocity = new Vector2(0f, -22f);
        yield return new WaitForSeconds(0.35f);

        // Shockwave impact
        CreateShockwave(transform.position);

        // Recover back to hover
        _body.linearVelocity = new Vector2(0f, 6f);
        yield return new WaitForSeconds(0.4f);

        _slamming = false;
    }

    private void FireEnergyPulse()
    {
        var player = PlayerHealth.Instance;
        if (player == null) return;

        Vector2 origin = transform.position;
        Vector2 target = player.transform.position;
        Vector2 dir = (target - origin).normalized;

        var bullet = new GameObject("Boss_EnergyPulse");
        bullet.transform.position = origin;

        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = PulseSprite();
        sr.color = _phase == 3 ? new Color(1f, 0.1f, 0.2f) : new Color(1f, 0.5f, 0.1f);
        sr.sortingOrder = 25;
        bullet.transform.localScale = Vector3.one * 0.7f;

        var col = bullet.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        col.isTrigger = true;

        var rb = bullet.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = dir * (_phase == 3 ? 12f : 8.5f);

        bullet.AddComponent<BossBulletHit>().damage = 15f;
        Destroy(bullet, 4f);
    }

    private void CreateShockwave(Vector2 epicenter)
    {
        for (int dir = -1; dir <= 1; dir += 2)
        {
            var wave = new GameObject("Boss_Shockwave");
            wave.transform.position = epicenter + new Vector2(dir * 0.8f, 0f);

            var sr = wave.AddComponent<SpriteRenderer>();
            sr.sprite = PulseSprite();
            sr.color = new Color(1f, 0.2f, 0.1f, 0.9f);
            sr.sortingOrder = 24;
            wave.transform.localScale = new Vector3(1.2f, 0.6f, 1f);

            var col = wave.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 0.6f);
            col.isTrigger = true;

            var rb = wave.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = new Vector2(dir * 9f, 0f);

            wave.AddComponent<BossBulletHit>().damage = 15f;
            Destroy(wave, 1.8f);
        }
    }

    public void TakeHit(int damage)
    {
        if (damage <= 0 || IsDead || _invulnTimer > 0) return;

        hitPoints = Mathf.Max(0, hitPoints - damage);
        _invulnTimer = 0.35f;

        // Update phase
        if (hitPoints <= 3) _phase = 3;
        else if (hitPoints <= 6) _phase = 2;
        else _phase = 1;

        if (hitPoints == 0)
        {
            StartCoroutine(DefeatSequence());
        }
    }

    private IEnumerator DefeatSequence()
    {
        _body.linearVelocity = Vector2.zero;
        if (_warning) _warning.gameObject.SetActive(false);

        // Explode shields
        if (_shieldRing != null) Destroy(_shieldRing.gameObject);

        // Flashing death rattle
        for (int i = 0; i < 8; i++)
        {
            if (_coreRend) _coreRend.color = (i % 2 == 0) ? Color.white : Color.red;
            transform.localScale = Vector3.one * (1.8f + (i % 2 == 0 ? 0.3f : -0.2f));
            yield return new WaitForSeconds(0.12f);
        }

        // Grant huge completion bonus
        var gm = GameManager.Ensure();
        gm.AddDataBits(500);
        gm.ledger.Collect("boss:rootkit:kill", 500);
        gm.dataBitsCollectedThisRun = gm.ledger.Bits;
        AchievementManager.Unlock("ACH_ROOTKIT_PURGE");

        // Disable collision
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

        // Shrink & destroy
        Destroy(gameObject, 0.2f);
    }

    private void BuildBossVisuals()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = 1.3f;
        col.isTrigger = true;

        // Core sprite
        var coreGo = new GameObject("CoreVisual", typeof(SpriteRenderer));
        coreGo.transform.SetParent(transform, false);
        _coreRend = coreGo.GetComponent<SpriteRenderer>();
        _coreRend.sprite = BossCoreSprite();
        _coreRend.sortingOrder = 15;
        coreGo.transform.localScale = Vector3.one * 2.2f;

        // Eye sprite
        var eyeGo = new GameObject("EyeVisual", typeof(SpriteRenderer));
        eyeGo.transform.SetParent(transform, false);
        _eyeRend = eyeGo.GetComponent<SpriteRenderer>();
        _eyeRend.sprite = BossEyeSprite();
        _eyeRend.color = Color.red;
        _eyeRend.sortingOrder = 16;
        eyeGo.transform.localScale = Vector3.one * 0.9f;

        // Orbiting Shields
        var ringGo = new GameObject("ShieldRing");
        ringGo.transform.SetParent(transform, false);
        _shieldRing = ringGo.transform;

        _shieldSegments = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var segGo = new GameObject($"ShieldSegment_{i}", typeof(SpriteRenderer));
            segGo.transform.SetParent(_shieldRing, false);
            float angle = i * 90f * Mathf.Deg2Rad;
            segGo.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.1f, Mathf.Sin(angle) * 2.1f, 0f);
            segGo.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);

            var sr = segGo.GetComponent<SpriteRenderer>();
            sr.sprite = ShieldSprite();
            sr.color = new Color(0.2f, 0.8f, 1f, 0.85f);
            sr.sortingOrder = 17;
            segGo.transform.localScale = new Vector3(0.8f, 1.4f, 1f);
            _shieldSegments[i] = sr;
        }

        // Warning sign
        var warnGo = new GameObject("BossWarning");
        warnGo.transform.SetParent(transform, false);
        warnGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        _warning = warnGo.AddComponent<TextMesh>();
        _warning.text = "! WARNING !";
        _warning.fontSize = 44;
        _warning.characterSize = 0.05f;
        _warning.anchor = TextAnchor.MiddleCenter;
        _warning.color = Color.red;
        warnGo.GetComponent<MeshRenderer>().sortingOrder = 35;
        warnGo.SetActive(false);
    }

    private static Sprite BossCoreSprite()
    {
        if (_bossCore != null) return _bossCore;
        int w = 32, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "BossCore" };
        var c = new Color32[w * h];
        Vector2 center = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d <= 14.5f)
                {
                    if (d >= 13f) c[y * w + x] = new Color32(20, 25, 35, 255);
                    else if (d >= 10f) c[y * w + x] = new Color32(45, 55, 75, 255);
                    else c[y * w + x] = new Color32(15, 18, 26, 255);
                }
                else c[y * w + x] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _bossCore = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _bossCore;
    }

    private static Sprite BossEyeSprite()
    {
        if (_bossEye != null) return _bossEye;
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "BossEye" };
        var c = new Color32[w * h];
        Vector2 center = new Vector2(7.5f, 7.5f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d <= 7f)
                {
                    byte a = (byte)(Mathf.Clamp01(1f - d / 7f) * 255f);
                    c[y * w + x] = new Color32(255, 255, 255, a);
                }
                else c[y * w + x] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _bossEye = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _bossEye;
    }

    private static Sprite ShieldSprite()
    {
        if (_shield != null) return _shield;
        int w = 8, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "BossShield" };
        var c = new Color32[w * h];
        for (int i = 0; i < c.Length; i++) c[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(c);
        tex.Apply();
        _shield = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _shield;
    }

    private static Sprite PulseSprite()
    {
        if (_pulse != null) return _pulse;
        int w = 12, h = 12;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "BossPulse" };
        var c = new Color32[w * h];
        Vector2 center = new Vector2(5.5f, 5.5f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d <= 5.5f) c[y * w + x] = new Color32(255, 255, 255, 255);
                else c[y * w + x] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _pulse = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _pulse;
    }

    private static Sprite _bossCore;
    private static Sprite _bossEye;
    private static Sprite _shield;
    private static Sprite _pulse;
}

/// <summary>Helper bullet script for boss projectiles.</summary>
public class BossBulletHit : MonoBehaviour
{
    public float damage = 15f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (SafeZone.IsPlayerSafe) return;
        var player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && !player.IsInvulnerable)
        {
            player.TakeDamage(damage, PlayerHealth.DamageType.TTLDamage);
            Destroy(gameObject);
        }
    }
}
