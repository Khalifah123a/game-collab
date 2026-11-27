using System.Collections;
using UnityEngine;

/// <summary>
/// Glitch Phantom (Zero-Day Desynchronization Exploit).
/// Educational Network Concept: Simulates timing and race condition exploits where packets
/// desynchronize from the system clock to bypass buffer filters, requiring alert timing to counter.
/// </summary>
public class Enemy_GlitchPhantom : MonoBehaviour, IEnemyDamageable
{
    public enum PhantomState
    {
        Pursuit,
        Desynced,
        AmbushWindup,
        LungeAttack,
        Recovery
    }

    [Header("Phantom Config")]
    public int hitPoints = 2;
    public float moveSpeed = 3.2f;
    public float ambushSpeed = 7.5f;
    public float desyncDuration = 1.4f;
    public float ambushWindup = 0.45f;
    public float contactDamage = 18f;

    [Header("Custom Sprite & Animasi (Opsional — Drag Asset Sendiri)")]
    public Sprite customSprite;
    public SpriteAnimationProfile customProfile;
    public ModularSpriteAnimator customSpriteAnimator;

    [Header("State")]
    public PhantomState currentState = PhantomState.Pursuit;
    public bool isDesynced;
    public bool isDead;

    private Transform _player;
    private SpriteRenderer _sprite;
    private Rigidbody2D _rb;
    private float _stateTimer;
    private float _desyncCooldown = 3.0f;
    private Vector2 _lungeDir;

    public bool IsDead => isDead;
    public Transform HitTransform => transform;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        _sprite = GetComponentInChildren<SpriteRenderer>();
        if (_sprite == null)
        {
            var go = new GameObject("PhantomSprite");
            go.transform.SetParent(transform, false);
            _sprite = go.AddComponent<SpriteRenderer>();
            _sprite.sprite = Resources.Load<Sprite>("GeneratedArt/portal_ring");
            _sprite.color = new Color(0.6f, 0.2f, 1f, 1f); // Neon purple glitch
            _sprite.sortingOrder = 45;
        }

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var cc = gameObject.AddComponent<CircleCollider2D>();
            cc.radius = 0.45f;
            cc.isTrigger = true;
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
            : Resources.Load<SpriteAnimationProfile>("CustomAnimations/Enemy_GlitchPhantom");
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
                if (!customSpriteAnimator.PlaySmart("Walk", 1, "Idle"))
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
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        bool hasCustomArt = customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt();
        Vector2 playerPos = _player.position;
        Vector2 myPos = transform.position;
        Vector2 toPlayer = playerPos - myPos;

        switch (currentState)
        {
            case PhantomState.Pursuit:
                isDesynced = false;
                _sprite.color = hasCustomArt ? Color.white : new Color(0.7f, 0.25f, 1f, 1f);

                _rb.linearVelocity = toPlayer.normalized * moveSpeed;

                _desyncCooldown -= Time.deltaTime;
                if (_desyncCooldown <= 0f && toPlayer.sqrMagnitude < 8f * 8f)
                {
                    EnterDesync();
                }
                break;

            case PhantomState.Desynced:
                isDesynced = true;
                // Flicker scanline effect
                float flicker = (Mathf.Sin(Time.time * 30f) * 0.5f + 0.5f) * 0.4f + 0.15f;
                _sprite.color = hasCustomArt ? new Color(0.8f, 0.9f, 1f, flicker) : new Color(0.5f, 0.8f, 1f, flicker);

                // Move swiftly around player's flank
                Vector2 flankPos = playerPos + new Vector2(toPlayer.x > 0 ? -2.2f : 2.2f, 1.2f);
                _rb.linearVelocity = (flankPos - myPos).normalized * (moveSpeed * 1.5f);

                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    EnterAmbushWindup(toPlayer);
                }
                break;

            case PhantomState.AmbushWindup:
                isDesynced = false;
                _rb.linearVelocity = Vector2.zero;
                _sprite.color = hasCustomArt ? new Color(1f, 0.72f, 0.72f, 1f) : new Color(1f, 0.1f, 0.3f, 1f);
                transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 25f) * 0.15f);

                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    EnterLunge(toPlayer.normalized);
                }
                break;

            case PhantomState.LungeAttack:
                _rb.linearVelocity = _lungeDir * ambushSpeed;
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    currentState = PhantomState.Recovery;
                    _stateTimer = 0.6f;
                    _rb.linearVelocity = Vector2.zero;
                }
                break;

            case PhantomState.Recovery:
                _sprite.color = hasCustomArt ? new Color(0.85f, 0.85f, 0.92f, 0.9f) : new Color(0.6f, 0.6f, 0.8f, 0.85f);
                transform.localScale = Vector3.one;
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    currentState = PhantomState.Pursuit;
                    _desyncCooldown = 3.2f;
                }
                break;
        }

        if (hasCustomArt)
        {
            string preferred = currentState == PhantomState.Desynced ? "Dash"
                : currentState == PhantomState.AmbushWindup ? "Windup"
                : currentState == PhantomState.LungeAttack ? "Attack"
                : currentState == PhantomState.Recovery ? "Idle"
                : "Chase";
            string fallback = currentState == PhantomState.AmbushWindup ? "Attack" : "Walk";
            string targetPlay = customSpriteAnimator.HasValidState(preferred) ? preferred
                : (customSpriteAnimator.HasValidState(fallback) ? fallback : "Idle");
            if (!customSpriteAnimator.IsPlayingState(targetPlay))
                customSpriteAnimator.PlaySmart(preferred, 1, fallback, "Idle", "Walk");
            if (Mathf.Abs(_rb.linearVelocity.x) > 0.05f)
                customSpriteAnimator.SetFacing(_rb.linearVelocity.x);
        }
    }

    private void EnterDesync()
    {
        currentState = PhantomState.Desynced;
        _stateTimer = desyncDuration;
        AudioManager.Play("sfx_dash", 0.6f, 1.6f);
        JuiceManager.SpawnImpactSparks(transform.position, new Color(0.5f, 0.2f, 1f, 0.8f));
    }

    private void EnterAmbushWindup(Vector2 toPlayer)
    {
        currentState = PhantomState.AmbushWindup;
        _stateTimer = ambushWindup;
        _lungeDir = toPlayer.normalized;
        AudioManager.Play("sfx_attack", 0.6f, 1.8f);
    }

    private void EnterLunge(Vector2 dir)
    {
        currentState = PhantomState.LungeAttack;
        _stateTimer = 0.45f;
        _lungeDir = dir;
        AudioManager.Play("sfx_shoot", 0.7f, 1.4f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || other.isTrigger) return;

        var player = other.GetComponent<PlayerHealth>();
        if (player != null && (currentState == PhantomState.LungeAttack || currentState == PhantomState.Pursuit))
        {
            player.TakeDamage(contactDamage, PlayerHealth.DamageType.TTLDamage);
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.Knockback(_lungeDir != Vector2.zero ? _lungeDir : Vector2.left);
        }
    }

    public void TakeHit(int damage)
    {
        if (isDead || damage <= 0) return;

        // In desync state, the phantom phased out and ignores attacks!
        if (isDesynced)
        {
            JuiceManager.SpawnImpactSparks(transform.position, new Color(0.6f, 0.3f, 1f, 0.5f));
            AudioManager.Play("sfx_parry", 0.5f, 1.8f);
            return;
        }

        hitPoints = Mathf.Max(0, hitPoints - damage);
        JuiceManager.Hitstop(0.045f);
        JuiceManager.SpawnImpactSparks(transform.position, new Color(0.8f, 0.2f, 1f, 1f));

        if (hitPoints == 0)
        {
            isDead = true;
            _rb.linearVelocity = Vector2.zero;

            JuiceManager.Shake(0.35f);
            JuiceManager.SpawnDataDissolve(transform.position, 16, new Color(0.7f, 0.2f, 1f, 1f));
            AudioManager.PlayEnemyDissolve();

            if (GameManager.Instance != null)
            {
                OptionalQuestService.ReportKill(GameManager.Instance.currentWorldIndex, "enemy:glitch_phantom", gameObject.GetHashCode().ToString());
            }

            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            Destroy(gameObject, 0.15f);
        }
    }
}
