using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grounded 3-Phase Boss Controller ("walk on terrain like the player").
/// Uses real Dynamic Rigidbody2D gravity + solid terrain collider with a cleanly decoupled
/// VisualRoot (SpriteRenderer + optional Animator) so custom friend artwork can drop in without
/// changing gameplay hitboxes, grounded physics, or phase AI.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Boss_Grounded : MonoBehaviour, IBossController
{
    public enum BossState
    {
        Dormant,
        Intro,
        Stalk,
        WindupCleave,
        AttackCleave,
        WindupDash,
        AttackDash,
        JumpUp,
        SlamFall,
        SlamRecover,
        PhaseShift,
        Stunned,
        Exhausted,
        Dead
    }

    [Header("Identity & Stats")]
    public string bossName = "SENTINEL_01 // CORE WARDEN";
    public int maxHitPoints = 10;
    public int totalPhases = 3;
    [Tooltip("When true, depleting HP to 0 triggers the next phase and refills the HP bar until the final phase is beaten.")]
    public bool refillHealthEachPhase = true;
    public int rewardBits = 250;
    public string rewardLedgerId = "boss:grounded:kill";
    public string unlockAchievementId = "";
    public float visualScale = 1.0f;
    public float cleaveDamage = 22f;
    public float dashDamage = 24f;
    public float slamDamage = 26f;

    [Header("Modular Visuals (Swap-Ready for Friend Assets)")]
    [Tooltip("Opsional: Drag 1 sprite diam custom langsung ke sini (Bottom-Center pivot disarankan).")]
    public Sprite customIdleSprite;
    [Tooltip("Opsional: Drag asset SpriteAnimationProfile ke sini (atau isi file di Resources/CustomAnimations/).")]
    public SpriteAnimationProfile customProfile;
    [Tooltip("Opsional: Path Resource profile otomatis (contoh: CustomAnimations/Boss_World5_Final).")]
    public string customProfileResourcePath = "";
    [Tooltip("Optional Animator Controller supporting Speed, IsGrounded, Phase, Wake, Windup, Slash, Dash, JumpSlam, PhaseShift, Hurt, Die, Exhausted.")]
    public RuntimeAnimatorController customAnimator;
    [Tooltip("Optional ModularSpriteAnimator component for frame-by-frame custom friend sprites.")]
    public ModularSpriteAnimator customSpriteAnimator;
    public Transform visualRoot;
    public SpriteRenderer bodyRenderer;
    public Animator bodyAnimator;

    [Header("Arena & Engagement")]
    public bool requireArenaTrigger = true;
    public float autoAggroRange = 13f;

    public string BossName => $"{bossName}  [FASE {_phase}/{Mathf.Max(1, totalPhases)}]";
    public int HitPoints => _hitPoints;
    public int MaxHitPoints => maxHitPoints;
    public float HealthPercent
    {
        get
        {
            if (_state == BossState.PhaseShift && refillHealthEachPhase)
                return Mathf.Clamp01(_phaseRefillProgress);
            return maxHitPoints > 0 ? Mathf.Clamp01((float)_hitPoints / maxHitPoints) : 0f;
        }
    }
    public Transform HitTransform => _chestAnchor != null ? _chestAnchor : transform;
    public bool IsDead => _state == BossState.Dead;
    public int CurrentPhase => _phase;
    public int TotalPhases => Mathf.Max(1, totalPhases);
    public BossState CurrentState => _state;

    private Rigidbody2D _rb;
    private BoxCollider2D _solidCollider;
    private BoxCollider2D _hurtboxCollider;
    private Transform _chestAnchor;
    private SpriteRenderer _auraRenderer;
    private SpriteRenderer _telegraphRenderer;
    private SpriteRenderer _slashArcRenderer;
    private TextMesh _warningBadge;

    private BossState _state = BossState.Dormant;
    private int _hitPoints;
    private int _phase = 1;
    private float _phaseRefillProgress = 1f;
    private float _phaseShiftDuration = 1.15f;
    private int _facing = -1; // -1 facing player on left at spawn
    private float _stateTimer;
    private float _attackCooldown = 0.6f;
    private float _flashTimer;
    private float _footstepTimer;
    private float _ignoreRefreshTimer;
    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _swingHitApplied;
    private bool _pendingComboSlash;
    private int _slamChainCounter;
    private int _attackPatternIndex;
    private float _dashDir = -1f;
    private Vector2 _squashScale = Vector2.one;

    private bool _hasArenaBounds;
    private float _arenaMinX;
    private float _arenaMaxX;
    private float _arenaFloorY;
    private Action _onDefeatedCallback;

    private PlayerHealth _playerHealth;
    private PlayerController _playerController;
    private PlayerCombat _playerCombat;

    private HashSet<string> _animParams;
    private Color _phaseTint = new Color(0.15f, 0.92f, 1f, 1f);
    private Color _baseArmorTint = Color.white;

    private static Sprite _cachedPixelSprite;
    private static Sprite _cachedSentinelSprite;
    private static Sprite _cachedSlashArcSprite;
    private static Sprite _cachedRingSprite;

    private void Awake()
    {
        _hitPoints = Mathf.Max(1, maxHitPoints);

        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = 3.0f;
        _rb.mass = 4.0f;
        _rb.linearDamping = 0.5f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _solidCollider = GetComponent<BoxCollider2D>();
        _solidCollider.isTrigger = false;
        _solidCollider.size = new Vector2(1.1f, 2.0f);
        _solidCollider.offset = new Vector2(0f, 1.0f); // bottom touches transform.position.y

        BuildVisualAndHurtboxHierarchy();
        RefreshColliderIgnores();
    }

    private void Start()
    {
        _hitPoints = Mathf.Max(1, maxHitPoints);
        _phase = 1;
        _phaseRefillProgress = 1f;
        ApplyCustomArtSetup();
        SnapFeetToGround();
        ApplyPhaseVisuals(1);
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
    }

    private SpriteAnimationProfile ResolveCustomProfile()
    {
        if (customProfile != null && customProfile.HasAnyCustomArt()) return customProfile;
        if (!string.IsNullOrEmpty(customProfileResourcePath))
        {
            var explicitProf = Resources.Load<SpriteAnimationProfile>(customProfileResourcePath);
            if (explicitProf != null && explicitProf.HasAnyCustomArt()) return explicitProf;
        }

        string lowerName = ((gameObject != null ? gameObject.name : "") + " " + (bossName ?? "")).ToLowerInvariant();
        string autoKey = null;
        if (lowerName.Contains("censor")) autoKey = "CustomAnimations/Boss_World2_Censor";
        else if (lowerName.Contains("solaris")) autoKey = "CustomAnimations/Boss_World4_Solaris";
        else if (lowerName.Contains("final") || lowerName.Contains("rootkit") || lowerName.Contains("akhir")) autoKey = "CustomAnimations/Boss_World5_Final";
        else if (lowerName.Contains("sentinel")) autoKey = "CustomAnimations/Boss_World1_Sentinel";

        if (!string.IsNullOrEmpty(autoKey))
        {
            var matched = Resources.Load<SpriteAnimationProfile>(autoKey);
            if (matched != null && matched.HasAnyCustomArt()) return matched;
        }

        var fallback = Resources.Load<SpriteAnimationProfile>("CustomAnimations/Boss_Default");
        if (fallback != null && fallback.HasAnyCustomArt()) return fallback;
        return null;
    }

    private void ApplyCustomArtSetup()
    {
        if (bodyRenderer == null && visualRoot != null)
            bodyRenderer = visualRoot.GetComponent<SpriteRenderer>();

        if (customSpriteAnimator == null)
        {
            customSpriteAnimator = GetComponent<ModularSpriteAnimator>();
            if (customSpriteAnimator == null && visualRoot != null)
                customSpriteAnimator = visualRoot.GetComponent<ModularSpriteAnimator>();
        }

        SpriteAnimationProfile resolvedProfile = ResolveCustomProfile();
        bool inlineAnimatorHasArt = customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt();

        if (resolvedProfile != null)
        {
            if (customSpriteAnimator == null)
            {
                customSpriteAnimator = gameObject.AddComponent<ModularSpriteAnimator>();
            }
            customSpriteAnimator.targetRenderer = bodyRenderer;
            customSpriteAnimator.useExternalFacing = true;
            customSpriteAnimator.ApplyProfile(resolvedProfile, overwriteExisting: !inlineAnimatorHasArt);
        }

        if (customSpriteAnimator != null && bodyRenderer != null)
        {
            customSpriteAnimator.targetRenderer = bodyRenderer;
            customSpriteAnimator.useExternalFacing = true;
            customSpriteAnimator.EnsureRenderer();
            if (customIdleSprite != null && customSpriteAnimator.defaultSprite == null)
                customSpriteAnimator.defaultSprite = customIdleSprite;

            if (customSpriteAnimator.HasAnyCustomArt())
            {
                if (!customSpriteAnimator.PlaySmart("Idle", _phase))
                {
                    Sprite fb = customSpriteAnimator.GetFallbackSprite();
                    if (fb != null) bodyRenderer.sprite = fb;
                }
                bodyRenderer.flipX = customSpriteAnimator.invertFacing;
            }
        }
        else if (customIdleSprite != null && bodyRenderer != null)
        {
            bodyRenderer.sprite = customIdleSprite;
        }
    }

    public void EngageArena(float arenaMinX, float arenaMaxX, float floorY, Action onDefeated = null)
    {
        _hasArenaBounds = true;
        _arenaMinX = Mathf.Min(arenaMinX, arenaMaxX);
        _arenaMaxX = Mathf.Max(arenaMinX, arenaMaxX);
        _arenaFloorY = floorY;
        _onDefeatedCallback = onDefeated;

        if (_state == BossState.Dead) return;
        if (_state == BossState.Dormant)
        {
            BossTracker.Register(this);
            EnterIntro();
        }
    }

    public void ResetForRetry(Vector3 spawnPos)
    {
        StopAllCoroutines();
        _hitPoints = Mathf.Max(1, maxHitPoints);
        _phase = 1;
        _state = BossState.Dormant;
        _attackPatternIndex = 0;
        _slamChainCounter = 0;
        _pendingComboSlash = false;
        _swingHitApplied = false;
        transform.position = spawnPos;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        if (_solidCollider != null) _solidCollider.enabled = true;
        if (_hurtboxCollider != null) _hurtboxCollider.enabled = true;
        SnapFeetToGround();
        ApplyPhaseVisuals(1);
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
        if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);
        BossTracker.ClearIfSame(this);
    }

    private void BuildVisualAndHurtboxHierarchy()
    {
        if (transform.Find("ChestAnchor") == null)
        {
            var chestGo = new GameObject("ChestAnchor");
            chestGo.transform.SetParent(transform, false);
            chestGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            _chestAnchor = chestGo.transform;
        }
        else
        {
            _chestAnchor = transform.Find("ChestAnchor");
        }

        if (transform.Find("Hurtbox") == null)
        {
            var hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(transform, false);
            _hurtboxCollider = hurtGo.AddComponent<BoxCollider2D>();
            _hurtboxCollider.isTrigger = true;
            _hurtboxCollider.size = new Vector2(1.45f, 2.25f);
            _hurtboxCollider.offset = new Vector2(0f, 1.125f);
        }
        else
        {
            _hurtboxCollider = transform.Find("Hurtbox").GetComponent<BoxCollider2D>();
        }

        if (visualRoot == null)
        {
            var existingVis = transform.Find("VisualRoot");
            if (existingVis != null) visualRoot = existingVis;
            else
            {
                var visGo = new GameObject("VisualRoot");
                visGo.transform.SetParent(transform, false);
                visGo.transform.localPosition = Vector3.zero;
                visualRoot = visGo.transform;
            }
        }

        bodyRenderer = visualRoot.GetComponent<SpriteRenderer>();
        if (bodyRenderer == null) bodyRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = customIdleSprite != null ? customIdleSprite : GetProceduralSentinelSprite();
        bodyRenderer.sortingOrder = 12;

        if (customSpriteAnimator == null)
        {
            customSpriteAnimator = GetComponent<ModularSpriteAnimator>();
            if (customSpriteAnimator == null && visualRoot != null)
                customSpriteAnimator = visualRoot.GetComponent<ModularSpriteAnimator>();
        }
        if (customSpriteAnimator != null && bodyRenderer != null)
        {
            customSpriteAnimator.targetRenderer = bodyRenderer;
            customSpriteAnimator.useExternalFacing = true;
            customSpriteAnimator.EnsureRenderer();
        }

        if (customAnimator != null)
        {
            bodyAnimator = visualRoot.GetComponent<Animator>();
            if (bodyAnimator == null) bodyAnimator = visualRoot.gameObject.AddComponent<Animator>();
            bodyAnimator.runtimeAnimatorController = customAnimator;
            CacheAnimatorParams();
        }

        // Ground phase aura indicator around boots
        var auraGo = new GameObject("PhaseAuraRing");
        auraGo.transform.SetParent(visualRoot, false);
        auraGo.transform.localPosition = new Vector3(0f, 0.14f, 0f);
        auraGo.transform.localScale = new Vector3(1.85f, 0.42f, 1f);
        _auraRenderer = auraGo.AddComponent<SpriteRenderer>();
        _auraRenderer.sprite = GetRingSprite();
        _auraRenderer.sortingOrder = 9;
        _auraRenderer.color = new Color(_phaseTint.r, _phaseTint.g, _phaseTint.b, 0.55f);

        // Floor / attack zone telegraph bar
        var telGo = new GameObject("AttackTelegraph");
        telGo.transform.SetParent(transform, false);
        _telegraphRenderer = telGo.AddComponent<SpriteRenderer>();
        _telegraphRenderer.sprite = GetPixelSprite();
        _telegraphRenderer.sortingOrder = 10;
        telGo.SetActive(false);

        // Crescent slash arc FX
        var slashGo = new GameObject("SlashArcFX");
        slashGo.transform.SetParent(visualRoot, false);
        slashGo.transform.localPosition = new Vector3(1.1f, 1.15f, 0f);
        _slashArcRenderer = slashGo.AddComponent<SpriteRenderer>();
        _slashArcRenderer.sprite = GetSlashArcSprite();
        _slashArcRenderer.sortingOrder = 15;
        _slashArcRenderer.enabled = false;

        // Crisp overhead warning sign
        var warnGo = new GameObject("BossWarningBadge");
        warnGo.transform.SetParent(transform, false);
        warnGo.transform.localPosition = new Vector3(0f, 2.75f, 0f);
        _warningBadge = warnGo.AddComponent<TextMesh>();
        _warningBadge.text = "!";
        _warningBadge.fontSize = 42;
        _warningBadge.characterSize = 0.085f;
        _warningBadge.anchor = TextAnchor.MiddleCenter;
        _warningBadge.alignment = TextAlignment.Center;
        _warningBadge.color = new Color(1f, 0.85f, 0.2f, 1f);
        var warnMr = warnGo.GetComponent<MeshRenderer>();
        if (warnMr != null) warnMr.sortingOrder = 22;
        warnGo.SetActive(false);
    }

    private void CacheAnimatorParams()
    {
        _animParams = new HashSet<string>();
        if (bodyAnimator == null || bodyAnimator.runtimeAnimatorController == null) return;
        foreach (var p in bodyAnimator.parameters)
        {
            _animParams.Add(p.name);
        }
    }

    private void SetAnimFloat(string param, float val)
    {
        if (bodyAnimator != null && _animParams != null && _animParams.Contains(param))
            bodyAnimator.SetFloat(param, val);
    }

    private void SetAnimBool(string param, bool val)
    {
        if (bodyAnimator != null && _animParams != null && _animParams.Contains(param))
            bodyAnimator.SetBool(param, val);
    }

    private void SetAnimInt(string param, int val)
    {
        if (bodyAnimator != null && _animParams != null && _animParams.Contains(param))
            bodyAnimator.SetInteger(param, val);
    }

    private void TriggerAnim(string param)
    {
        if (bodyAnimator != null && _animParams != null && _animParams.Contains(param))
            bodyAnimator.SetTrigger(param);

        if (customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt())
        {
            if (param == "Wake") customSpriteAnimator.PlaySmart("Idle", _phase);
            else if (param == "Windup") customSpriteAnimator.PlaySmart("Windup", _phase, "Slash", "Attack");
            else if (param == "Slash") customSpriteAnimator.PlaySmart("Slash", _phase, "Attack");
            else if (param == "Dash") customSpriteAnimator.PlaySmart("Dash", _phase, "Walk", "Attack");
            else if (param == "JumpSlam") customSpriteAnimator.PlaySmart("Jump", _phase, "JumpSlam", "Slam", "Attack");
            else if (param == "Hurt" || param == "Exhausted") customSpriteAnimator.PlaySmart(param, _phase, "Hurt", "Idle");
            else if (param == "PhaseShift") customSpriteAnimator.PlaySmart("PhaseShift", _phase, "Special", "Attack", "Slash");
            else if (param == "Die") customSpriteAnimator.PlaySmart("Death", _phase, "Die", "Hurt");
            else customSpriteAnimator.PlaySmart(param, _phase, "Idle");
        }
    }

    private void RefreshColliderIgnores()
    {
        if (_solidCollider == null) return;

        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            _playerController = pc;
            _playerCombat = pc.GetComponent<PlayerCombat>();
            foreach (var col in pc.GetComponentsInChildren<Collider2D>(true))
            {
                if (col != null && !col.isTrigger) Physics2D.IgnoreCollision(_solidCollider, col, true);
            }
        }

        foreach (var effector in FindObjectsByType<PlatformEffector2D>())
        {
            if (effector == null) continue;
            var col = effector.GetComponent<Collider2D>();
            if (col != null) Physics2D.IgnoreCollision(_solidCollider, col, true);
        }

        foreach (var patrol in FindObjectsByType<Enemy_Patrol>())
        {
            if (patrol == null) continue;
            var col = patrol.GetComponent<Collider2D>();
            if (col != null && !col.isTrigger) Physics2D.IgnoreCollision(_solidCollider, col, true);
        }
    }

    public void SnapFeetToGround()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 2.5f;
        float bestDist = float.MaxValue;
        float? bestY = null;

        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 12f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponent<PlatformEffector2D>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.normal.y < 0.55f) continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestY = hit.point.y;
            }
        }

        if (bestY.HasValue)
        {
            transform.position = new Vector3(transform.position.x, bestY.Value + 0.01f, transform.position.z);
            if (_rb != null) _rb.position = transform.position;
        }
    }

    private void CachePlayerRefs()
    {
        if (_playerHealth == null) _playerHealth = PlayerHealth.Instance;
        if (_playerHealth != null && _playerController == null)
        {
            _playerController = _playerHealth.GetComponent<PlayerController>();
            _playerCombat = _playerHealth.GetComponent<PlayerCombat>();
        }
    }

    private bool CheckGrounded()
    {
        Vector2 center = _rb != null ? _rb.position : (Vector2)transform.position;
        float[] offsets = { -0.45f, 0f, 0.45f };
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2 origin = new Vector2(center.x + offsets[i], center.y + 0.16f);
            foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 0.28f))
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponent<PlatformEffector2D>() != null) continue;
                if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.normal.y > 0.55f) return true;
            }
        }
        return false;
    }

    private void FixedUpdate()
    {
        if (_state == BossState.Dead) return;

        CachePlayerRefs();

        _ignoreRefreshTimer -= Time.fixedDeltaTime;
        if (_ignoreRefreshTimer <= 0f)
        {
            _ignoreRefreshTimer = 1.5f;
            RefreshColliderIgnores();
        }

        _wasGrounded = _isGrounded;
        _isGrounded = CheckGrounded();

        _flashTimer -= Time.fixedDeltaTime;
        _attackCooldown = Mathf.Max(0f, _attackCooldown - Time.fixedDeltaTime);
        _stateTimer -= Time.fixedDeltaTime;

        if (_playerHealth == null || _playerHealth.isDead || SafeZone.IsPlayerSafe)
        {
            if (_state != BossState.Dormant)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
                if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
                if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);
            }
            ClampInsideArena();
            UpdateVisualsAndAnimator();
            return;
        }

        if (_state == BossState.Dormant)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            if (!requireArenaTrigger && Vector2.Distance(_playerHealth.transform.position, transform.position) <= autoAggroRange)
            {
                BossTracker.Register(this);
                EnterIntro();
            }
            ClampInsideArena();
            UpdateVisualsAndAnimator();
            return;
        }

        switch (_state)
        {
            case BossState.Intro:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                FacePlayer();
                if (_stateTimer <= 0f)
                {
                    _state = BossState.Stalk;
                    _attackCooldown = 0.45f;
                }
                break;

            case BossState.PhaseShift:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _phaseRefillProgress = Mathf.Clamp01(1f - (_stateTimer / Mathf.Max(0.1f, _phaseShiftDuration)));
                if (_stateTimer <= 0f)
                {
                    _phaseRefillProgress = 1f;
                    if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);
                    _state = BossState.Stalk;
                    _attackCooldown = 0.35f;
                }
                break;

            case BossState.Stunned:
            case BossState.Exhausted:
                _rb.linearVelocity = new Vector2(Mathf.MoveTowards(_rb.linearVelocity.x, 0f, 18f * Time.fixedDeltaTime), _rb.linearVelocity.y);
                if (_stateTimer <= 0f)
                {
                    _state = BossState.Stalk;
                    _attackCooldown = _phase >= 3 ? 0.3f : 0.45f;
                }
                break;

            case BossState.Stalk:
                UpdateStalk();
                break;

            case BossState.WindupCleave:
                _rb.linearVelocity = new Vector2(Mathf.MoveTowards(_rb.linearVelocity.x, 0f, 24f * Time.fixedDeltaTime), _rb.linearVelocity.y);
                if (_stateTimer <= 0f) EnterAttackCleave();
                break;

            case BossState.AttackCleave:
                UpdateAttackCleave();
                break;

            case BossState.WindupDash:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                if (_stateTimer <= 0f) EnterAttackDash();
                break;

            case BossState.AttackDash:
                UpdateAttackDash();
                break;

            case BossState.JumpUp:
                UpdateJumpUp();
                break;

            case BossState.SlamFall:
                UpdateSlamFall();
                break;

            case BossState.SlamRecover:
                _rb.linearVelocity = new Vector2(Mathf.MoveTowards(_rb.linearVelocity.x, 0f, 28f * Time.fixedDeltaTime), _rb.linearVelocity.y);
                if (_stateTimer <= 0f)
                {
                    if (_phase >= 3)
                    {
                        _slamChainCounter++;
                        if (_slamChainCounter >= 2)
                        {
                            _slamChainCounter = 0;
                            EnterExhausted(0.95f);
                            break;
                        }
                    }
                    _state = BossState.Stalk;
                    _attackCooldown = _phase >= 3 ? 0.35f : 0.55f;
                }
                break;
        }

        ClampInsideArena();
        UpdateVisualsAndAnimator();
    }

    private void EnterIntro()
    {
        _state = BossState.Intro;
        _stateTimer = 0.72f;
        FacePlayer();
        _squashScale = new Vector2(1.22f, 0.82f);
        TriggerAnim("Wake");
        JuiceManager.Shake(0.32f);
        JuiceManager.SpawnImpactSparks(HitTransform.position, _phaseTint, 0f);
        if (_warningBadge != null)
        {
            _warningBadge.text = "ACTIVE";
            _warningBadge.color = _phaseTint;
            _warningBadge.gameObject.SetActive(true);
        }
    }

    private void UpdateStalk()
    {
        if (!_isGrounded)
        {
            return;
        }

        FacePlayer();
        Vector2 delta = (Vector2)_playerHealth.transform.position - (Vector2)transform.position;
        float absDx = Mathf.Abs(delta.x);
        float absDy = Mathf.Abs(delta.y);

        float walkSpeed = _phase == 1 ? 3.8f : (_phase == 2 ? 4.9f : 6.1f);
        float desiredVx = absDx > 1.7f ? Mathf.Sign(delta.x) * walkSpeed : 0f;
        _rb.linearVelocity = new Vector2(desiredVx, _rb.linearVelocity.y);

        if (Mathf.Abs(desiredVx) > 0.2f)
        {
            _footstepTimer -= Time.fixedDeltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = _phase >= 3 ? 0.24f : 0.34f;
                AudioManager.PlayFootstep(_phase >= 2);
                JuiceManager.SpawnDustPuff(transform.position + new Vector3(-_facing * 0.35f, 0.05f, 0f));
            }
        }

        if (_attackCooldown > 0f) return;

        // Close range melee cleave priority
        if (absDx <= 3.1f && absDy <= 2.3f)
        {
            EnterWindupCleave();
            return;
        }

        // Mid/long range attack rotation based on phase
        _attackPatternIndex++;
        if (_phase == 1)
        {
            if (absDx <= 9.0f) EnterWindupDash();
        }
        else if (_phase == 2)
        {
            if (_attackPatternIndex % 2 == 0) EnterJumpSlam();
            else EnterWindupDash();
        }
        else // Phase 3: Aggressive Jump Slam + Combo Dash->Cleave
        {
            if (_attackPatternIndex % 2 == 0) EnterJumpSlam();
            else
            {
                _pendingComboSlash = true;
                EnterWindupDash();
            }
        }
    }

    private void EnterWindupCleave()
    {
        FacePlayer();
        _state = BossState.WindupCleave;
        _stateTimer = _phase == 1 ? 0.42f : (_phase == 2 ? 0.34f : 0.27f);
        _swingHitApplied = false;
        _squashScale = new Vector2(0.86f, 1.16f);
        TriggerAnim("Windup");

        if (_warningBadge != null)
        {
            _warningBadge.text = "!";
            _warningBadge.color = new Color(1f, 0.85f, 0.15f, 1f);
            _warningBadge.gameObject.SetActive(true);
        }

        SetTelegraph(
            true,
            new Vector2(_facing * 1.45f, 1.05f),
            new Vector2(2.6f, 1.85f),
            new Color(1f, 0.72f, 0.12f, 0.32f));
    }

    private void EnterAttackCleave()
    {
        _state = BossState.AttackCleave;
        _stateTimer = 0.17f;
        _swingHitApplied = false;
        _rb.linearVelocity = new Vector2(_facing * (_phase >= 3 ? 9.5f : 7.0f), _rb.linearVelocity.y);
        _squashScale = new Vector2(1.24f, 0.86f);
        TriggerAnim("Slash");
        AudioManager.Play("sfx_slash", 0.9f, _phase >= 3 ? 1.12f : 0.95f);

        SetTelegraph(
            true,
            new Vector2(_facing * 1.45f, 1.05f),
            new Vector2(2.6f, 1.85f),
            new Color(1f, 0.18f, 0.25f, 0.52f));

        if (_slashArcRenderer != null)
        {
            _slashArcRenderer.enabled = true;
            _slashArcRenderer.color = _phase >= 3 ? new Color(1f, 0.2f, 0.45f, 0.95f) : new Color(1f, 0.55f, 0.15f, 0.95f);
        }
    }

    private void UpdateAttackCleave()
    {
        if (!_swingHitApplied)
        {
            Vector2 hitCenter = (Vector2)transform.position + new Vector2(_facing * 1.45f, 1.05f);
            Vector2 hitSize = new Vector2(2.65f, 1.9f);
            if (TryStrikePlayerInBox(hitCenter, hitSize, cleaveDamage, _facing, parryable: true))
            {
                _swingHitApplied = true;
                if (_state == BossState.Stunned) return;
            }
        }

        if (_stateTimer <= 0f)
        {
            SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
            if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
            if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);

            if (_phase >= 3)
            {
                _slamChainCounter++;
                if (_slamChainCounter >= 2)
                {
                    _slamChainCounter = 0;
                    EnterExhausted(0.9f);
                    return;
                }
            }
            _state = BossState.Stalk;
            _attackCooldown = _phase >= 3 ? 0.35f : 0.65f;
        }
    }

    private void EnterWindupDash()
    {
        FacePlayer();
        _dashDir = _facing;
        _state = BossState.WindupDash;
        _stateTimer = _phase == 1 ? 0.48f : (_phase == 2 ? 0.38f : 0.30f);
        _swingHitApplied = false;
        _squashScale = new Vector2(1.2f, 0.78f);
        TriggerAnim("Windup");

        if (_warningBadge != null)
        {
            _warningBadge.text = ">>";
            _warningBadge.color = new Color(1f, 0.35f, 0.2f, 1f);
            _warningBadge.gameObject.SetActive(true);
        }

        // Crisp red danger beam along floor lane
        SetTelegraph(
            true,
            new Vector2(_dashDir * 3.2f, 0.32f),
            new Vector2(6.4f, 0.38f),
            new Color(1f, 0.22f, 0.28f, 0.55f));
    }

    private void EnterAttackDash()
    {
        _state = BossState.AttackDash;
        _stateTimer = _phase >= 3 ? 0.24f : 0.28f;
        _swingHitApplied = false;
        _squashScale = new Vector2(1.32f, 0.78f);
        TriggerAnim("Dash");
        JuiceManager.Shake(0.18f);
        JuiceManager.SpawnDustPuff(transform.position + new Vector3(-_dashDir * 0.5f, 0.1f, 0f));
        AudioManager.Play("sfx_dash", 0.85f, 0.85f);
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);
    }

    private void UpdateAttackDash()
    {
        float dashSpeed = _phase == 1 ? 16.5f : (_phase == 2 ? 19.5f : 22.0f);
        _rb.linearVelocity = new Vector2(_dashDir * dashSpeed, _rb.linearVelocity.y);

        if (!_swingHitApplied)
        {
            Vector2 hitCenter = (Vector2)transform.position + new Vector2(_dashDir * 0.45f, 1.0f);
            Vector2 hitSize = new Vector2(1.85f, 1.85f);
            if (TryStrikePlayerInBox(hitCenter, hitSize, dashDamage, _dashDir, parryable: true))
            {
                _swingHitApplied = true;
                if (_state == BossState.Stunned) return;
            }
        }

        // Stop early if hitting arena wall
        if (_hasArenaBounds && ((_dashDir < 0 && transform.position.x <= _arenaMinX + 1.05f) ||
                                (_dashDir > 0 && transform.position.x >= _arenaMaxX - 1.05f)))
        {
            _stateTimer = 0f;
        }

        if (_stateTimer <= 0f)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            if (_pendingComboSlash && _playerHealth != null && Vector2.Distance(_playerHealth.transform.position, transform.position) <= 4.4f)
            {
                _pendingComboSlash = false;
                EnterWindupCleave();
                return;
            }
            _pendingComboSlash = false;
            _state = BossState.Stalk;
            _attackCooldown = _phase >= 3 ? 0.35f : 0.65f;
        }
    }

    private void EnterJumpSlam()
    {
        FacePlayer();
        _state = BossState.JumpUp;
        _stateTimer = 0.28f;
        _swingHitApplied = false;

        float dx = _playerHealth != null ? _playerHealth.transform.position.x - transform.position.x : _facing * 4f;
        float targetVx = Mathf.Clamp(dx * 1.45f, -9.5f, 9.5f);
        _rb.linearVelocity = new Vector2(targetVx, 14.5f);
        _squashScale = new Vector2(0.76f, 1.28f);
        TriggerAnim("JumpSlam");
        JuiceManager.SpawnDustPuff(transform.position + Vector3.up * 0.1f);
        AudioManager.PlayLand();

        if (_warningBadge != null)
        {
            _warningBadge.text = "SLAM";
            _warningBadge.color = new Color(1f, 0.45f, 0.15f, 1f);
            _warningBadge.gameObject.SetActive(true);
        }
    }

    private void UpdateJumpUp()
    {
        if (_stateTimer <= 0f || _rb.linearVelocity.y <= 0.5f)
        {
            _state = BossState.SlamFall;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.35f, -22.5f);
            _stateTimer = 1.0f; // failsafe max air fall time
        }
    }

    private void UpdateSlamFall()
    {
        if (_rb.linearVelocity.y > -20f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.9f, -22.5f);
        }

        if ((_isGrounded && !_wasGrounded) || (_isGrounded && _stateTimer < 0.85f) || _stateTimer <= 0f)
        {
            OnSlamImpact();
        }
    }

    private void OnSlamImpact()
    {
        _state = BossState.SlamRecover;
        _stateTimer = _phase >= 3 ? 0.34f : 0.48f;
        _rb.linearVelocity = Vector2.zero;
        _squashScale = new Vector2(1.38f, 0.68f);
        if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);

        JuiceManager.Shake(0.52f);
        JuiceManager.Hitstop(0.045f);
        AudioManager.PlayLand();
        AudioManager.Play("sfx_explosion", 0.65f, 0.9f);
        JuiceManager.SpawnDustPuff(transform.position + new Vector3(-0.7f, 0.08f, 0f));
        JuiceManager.SpawnDustPuff(transform.position + new Vector3(0.7f, 0.08f, 0f));

        // Direct slam impact box
        TryStrikePlayerInBox((Vector2)transform.position + new Vector2(0f, 0.95f), new Vector2(3.6f, 2.0f), slamDamage, _facing, parryable: false);

        // Spawn left & right ground-hugging Shockwaves across the arena floor
        float waveSpeed = _phase >= 3 ? 8.8f : 7.2f;
        float minBound = _hasArenaBounds ? _arenaMinX + 0.5f : transform.position.x - 14f;
        float maxBound = _hasArenaBounds ? _arenaMaxX - 0.5f : transform.position.x + 14f;
        SpawnGroundShockwave(-1f, waveSpeed, minBound, maxBound);
        SpawnGroundShockwave(1f, waveSpeed, minBound, maxBound);

        // Phase 3 adds angled twin cyber bolts overhead so ledges stay exciting
        if (_phase >= 3)
        {
            SpawnPhaseBolt(new Vector2(-1f, 0.25f).normalized * 9.5f);
            SpawnPhaseBolt(new Vector2(1f, 0.25f).normalized * 9.5f);
        }
    }

    private void SpawnGroundShockwave(float dir, float speed, float minX, float maxX)
    {
        var go = new GameObject($"BossShockwave_{(dir < 0 ? "L" : "R")}");
        go.transform.position = new Vector3(transform.position.x + dir * 0.9f, transform.position.y + 0.42f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPixelSprite();
        sr.sortingOrder = 14;
        sr.color = _phase >= 3 ? new Color(1f, 0.2f, 0.5f, 0.95f) : new Color(1f, 0.65f, 0.1f, 0.95f);
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        var wave = go.AddComponent<BossGroundShockwave>();
        wave.Initialize(dir, speed, 18f, minX, maxX, sr.color);
    }

    private void SpawnPhaseBolt(Vector2 velocity)
    {
        var go = new GameObject("BossPhaseBolt");
        go.transform.position = (Vector2)transform.position + new Vector2(0f, 1.3f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.sortingOrder = 14;
        sr.color = new Color(1f, 0.25f, 0.6f, 0.95f);
        go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        var bolt = go.AddComponent<BossPhaseBolt>();
        bolt.Initialize(velocity, 16f);
    }

    private void EnterExhausted(float duration)
    {
        _state = BossState.Exhausted;
        _stateTimer = duration;
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
        TriggerAnim("Exhausted");
        if (_warningBadge != null)
        {
            _warningBadge.text = "...";
            _warningBadge.color = new Color(0.55f, 0.85f, 1f, 0.9f);
            _warningBadge.gameObject.SetActive(true);
        }
    }

    private bool TryStrikePlayerInBox(Vector2 center, Vector2 size, float damage, float pushDir, bool parryable)
    {
        if (_playerHealth == null || _playerHealth.isDead || SafeZone.IsPlayerSafe) return false;

        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            if (hit == null) continue;
            if (hit.GetComponentInParent<PlayerHealth>() != _playerHealth) continue;

            if (parryable && _playerCombat != null && _playerCombat.IsParrying)
            {
                _playerCombat.OnParrySuccess(damage, PlayerHealth.DamageType.TTLDamage);
                EnterParryStun(-pushDir);
                return true;
            }

            if (!_playerHealth.IsInvulnerable)
            {
                _playerHealth.TakeDamage(damage, PlayerHealth.DamageType.TTLDamage);
                if (_playerController != null)
                {
                    _playerController.Knockback(new Vector2(Mathf.Sign(pushDir == 0f ? _facing : pushDir), 0f));
                }
                return true;
            }
        }
        return false;
    }

    private void EnterParryStun(float knockDir)
    {
        _state = BossState.Stunned;
        _stateTimer = 0.72f;
        _pendingComboSlash = false;
        _rb.linearVelocity = new Vector2(knockDir * 5.5f, 2.5f);
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
        TriggerAnim("Hurt");
        if (_warningBadge != null)
        {
            _warningBadge.text = "STUN";
            _warningBadge.color = new Color(0.25f, 1f, 0.85f, 1f);
            _warningBadge.gameObject.SetActive(true);
        }
        TakeHit(1);
    }

    public void TakeHit(int damage)
    {
        if (damage <= 0 || _state == BossState.Dead || _state == BossState.Dormant || _state == BossState.Intro) return;

        // Armor invulnerability during brief phase transition roar
        if (_state == BossState.PhaseShift)
        {
            JuiceManager.SpawnImpactSparks(HitTransform.position, Color.white, 0f);
            return;
        }

        _hitPoints = Mathf.Max(0, _hitPoints - damage);
        _flashTimer = 0.09f;
        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        _squashScale = new Vector2(1.16f, 0.88f);
        TriggerAnim("Hurt");

        if (_hitPoints <= 0)
        {
            if (refillHealthEachPhase && _phase < Mathf.Max(1, totalPhases))
            {
                // HP bar depleted -> evolve into next phase and refill HP bar for the new phase!
                StartPhaseShift(_phase + 1, refillHealth: true);
                return;
            }

            StartCoroutine(DefeatRoutine());
            return;
        }

        if (!refillHealthEachPhase)
        {
            int targetPhase = ComputeTargetPhase();
            if (targetPhase > _phase)
            {
                StartPhaseShift(targetPhase, refillHealth: false);
            }
        }
    }

    private int ComputeTargetPhase()
    {
        float pct = HealthPercent;
        if (pct > 0.667f) return 1;
        if (pct > 0.334f) return 2;
        return 3;
    }

    private void StartPhaseShift(int newPhase, bool refillHealth = false)
    {
        _phase = newPhase;
        _state = BossState.PhaseShift;
        _phaseShiftDuration = refillHealth ? 1.15f : 0.88f;
        _stateTimer = _phaseShiftDuration;
        if (refillHealth)
        {
            _hitPoints = Mathf.Max(1, maxHitPoints);
            _phaseRefillProgress = 0f;
        }
        else
        {
            _phaseRefillProgress = 1f;
        }

        _pendingComboSlash = false;
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;

        ApplyPhaseVisuals(_phase);
        TriggerAnim("PhaseShift");
        _squashScale = new Vector2(1.38f, 0.72f);

        JuiceManager.Hitstop(0.075f);
        JuiceManager.Shake(0.55f);
        JuiceManager.SpawnDataDissolve(HitTransform.position, 18, _phaseTint);
        AudioManager.Play("sfx_powerup", 0.95f, _phase == 2 ? 0.95f : 1.18f);
        AudioManager.Play("sfx_explosion", 0.55f, _phase == 2 ? 0.85f : 1.05f);

        if (_warningBadge != null)
        {
            _warningBadge.text = _phase == 2 ? "FASE 2 / 3 !!" : "FASE 3 / 3 — OVERDRIVE!!";
            _warningBadge.color = _phaseTint;
            _warningBadge.gameObject.SetActive(true);
        }

        // Gentle radial pushback if player is hugging body (no unfair damage)
        if (_playerController != null && Vector2.Distance(_playerController.transform.position, transform.position) < 3.0f)
        {
            float pushDir = Mathf.Sign(_playerController.transform.position.x - transform.position.x);
            if (pushDir == 0f) pushDir = -_facing;
            _playerController.Knockback(new Vector2(pushDir, 0f));
        }

        // Phase eruption spawns twin ground shockwaves so the phase transition feels dramatic
        float minBound = _hasArenaBounds ? _arenaMinX + 0.5f : transform.position.x - 14f;
        float maxBound = _hasArenaBounds ? _arenaMaxX - 0.5f : transform.position.x + 14f;
        float waveSpeed = _phase >= 3 ? 8.2f : 6.8f;
        SpawnGroundShockwave(-1f, waveSpeed, minBound, maxBound);
        SpawnGroundShockwave(1f, waveSpeed, minBound, maxBound);
    }

    private void ApplyPhaseVisuals(int phase)
    {
        if (phase == 1)
        {
            _phaseTint = new Color(0.15f, 0.92f, 1f, 1f);
            _baseArmorTint = Color.white;
        }
        else if (phase == 2)
        {
            _phaseTint = new Color(1f, 0.65f, 0.12f, 1f);
            _baseArmorTint = new Color(1f, 0.88f, 0.72f, 1f);
        }
        else
        {
            _phaseTint = new Color(1f, 0.18f, 0.48f, 1f);
            _baseArmorTint = new Color(1f, 0.68f, 0.78f, 1f);
        }

        if (_auraRenderer != null)
        {
            _auraRenderer.color = new Color(_phaseTint.r, _phaseTint.g, _phaseTint.b, 0.6f);
        }
        SetAnimInt("Phase", phase);
    }

    private IEnumerator DefeatRoutine()
    {
        _state = BossState.Dead;
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
        if (_solidCollider != null) _solidCollider.enabled = false;
        if (_hurtboxCollider != null) _hurtboxCollider.enabled = false;
        SetTelegraph(false, Vector2.zero, Vector2.one, Color.clear);
        if (_slashArcRenderer != null) _slashArcRenderer.enabled = false;
        if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);

        TriggerAnim("Die");
        BossTracker.Unregister(this);

        if (rewardBits > 0)
        {
            var gm = GameManager.Ensure();
            if (gm != null)
            {
                string key = string.IsNullOrEmpty(rewardLedgerId) ? $"boss:{bossName}:kill" : rewardLedgerId;
                gm.ledger.Collect(key, rewardBits);
                gm.dataBitsCollectedThisRun = gm.ledger.Bits;
            }
        }
        if (!string.IsNullOrEmpty(unlockAchievementId))
        {
            AchievementManager.Unlock(unlockAchievementId);
        }

        JuiceManager.Hitstop(0.09f);
        JuiceManager.Shake(0.65f);
        JuiceManager.SpawnDataDissolve(HitTransform.position, 24, _phaseTint);
        AudioManager.PlayEnemyDissolve();

        _onDefeatedCallback?.Invoke();

        float elapsed = 0f;
        const float duration = 0.65f;
        float baseScale = Mathf.Max(0.5f, visualScale);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (bodyRenderer != null)
            {
                Color c = _phaseTint;
                c.a = Mathf.Lerp(1f, 0f, t);
                bodyRenderer.color = c;
            }
            if (visualRoot != null)
            {
                float s = Mathf.Lerp(baseScale, baseScale * 0.55f, t);
                visualRoot.localScale = new Vector3(_facing * s, s, 1f);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void FacePlayer()
    {
        if (_playerHealth == null) return;
        float dx = _playerHealth.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.15f)
        {
            _facing = dx >= 0f ? 1 : -1;
        }
    }

    private void ClampInsideArena()
    {
        if (!_hasArenaBounds) return;
        float minX = _arenaMinX + 0.95f;
        float maxX = _arenaMaxX - 0.95f;
        if (minX > maxX) return;

        Vector3 pos = transform.position;
        if (pos.x < minX)
        {
            pos.x = minX;
            transform.position = pos;
            if (_rb != null && _rb.linearVelocity.x < 0f)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            transform.position = pos;
            if (_rb != null && _rb.linearVelocity.x > 0f)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        // Floor safety clamp so physics tunneling never pushes boss under floor
        if (pos.y < _arenaFloorY - 0.15f)
        {
            pos.y = _arenaFloorY;
            transform.position = pos;
            if (_rb != null && _rb.linearVelocity.y < 0f)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        }
    }

    private void SetTelegraph(bool active, Vector2 localPos, Vector2 size, Color color)
    {
        if (_telegraphRenderer == null) return;
        _telegraphRenderer.gameObject.SetActive(active);
        if (!active) return;
        _telegraphRenderer.transform.localPosition = localPos;
        _telegraphRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        _telegraphRenderer.color = color;
    }

    private void UpdateVisualsAndAnimator()
    {
        _squashScale = Vector2.Lerp(_squashScale, Vector2.one, 12f * Time.fixedDeltaTime);

        bool hasCustomArt = customSpriteAnimator != null && customSpriteAnimator.HasAnyCustomArt();
        float customScaleMult = hasCustomArt ? Mathf.Clamp(customSpriteAnimator.visualScaleMultiplier, 0.25f, 4f) : 1f;

        if (visualRoot != null)
        {
            float baseScale = Mathf.Max(0.5f, visualScale) * customScaleMult;
            float breathe = _state == BossState.Stalk && Mathf.Abs(_rb.linearVelocity.x) < 0.2f
                ? 1f + 0.03f * Mathf.Sin(Time.time * 4f)
                : 1f;
            visualRoot.localScale = new Vector3(_facing * _squashScale.x * baseScale, _squashScale.y * breathe * baseScale, 1f);
        }

        if (bodyRenderer != null)
        {
            if (_flashTimer > 0f) bodyRenderer.color = Color.white;
            else if (_state == BossState.PhaseShift) bodyRenderer.color = _phaseTint;
            else if (_state == BossState.Exhausted) bodyRenderer.color = new Color(0.55f, 0.72f, 0.9f, 1f);
            else bodyRenderer.color = hasCustomArt ? Color.Lerp(Color.white, _baseArmorTint, 0.35f) : _baseArmorTint;

            // VisualRoot.localScale.x already flips facing, so flipX only applies invertFacing (ZERO double-flip!)
            if (hasCustomArt)
            {
                bodyRenderer.flipX = customSpriteAnimator.invertFacing;
            }
        }

        if (_auraRenderer != null)
        {
            float pulse = 0.9f + 0.14f * Mathf.Sin(Time.time * (_phase >= 3 ? 11f : 5f));
            _auraRenderer.transform.localScale = new Vector3(1.85f * pulse, 0.42f * pulse, 1f);
        }

        SetAnimFloat("Speed", _rb != null ? Mathf.Abs(_rb.linearVelocity.x) : 0f);
        SetAnimBool("IsGrounded", _isGrounded);
        SetAnimInt("Phase", _phase);

        if (hasCustomArt)
        {
            customSpriteAnimator.useExternalFacing = true;
            if (_state == BossState.Stalk)
            {
                bool isWalking = _rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.15f;
                string preferred = isWalking ? "Walk" : "Idle";
                string expected = (_phase > 1 && customSpriteAnimator.HasValidState($"{preferred}_P{_phase}"))
                    ? $"{preferred}_P{_phase}"
                    : preferred;
                if (!customSpriteAnimator.IsPlayingState(expected))
                {
                    customSpriteAnimator.PlaySmart(preferred, _phase, "Idle", "Walk");
                }
            }
            else if (_state == BossState.SlamFall && !customSpriteAnimator.IsPlayingState("Slam"))
            {
                customSpriteAnimator.PlaySmart("Slam", _phase, "JumpSlam", "Attack");
            }
        }
    }

    private void OnDestroy()
    {
        BossTracker.ClearIfSame(this);
    }

    // -------------------------------------------------------------------------
    // Procedural Pixel Art Builders (Placeholder until friend provides sprite)
    // -------------------------------------------------------------------------
    private static Sprite GetPixelSprite()
    {
        if (_cachedPixelSprite == null)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _cachedPixelSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
        return _cachedPixelSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (_cachedRingSprite == null)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = (r <= 1f && r >= 0.55f) ? 1f : (r < 0.55f ? 0.25f : 0f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
        return _cachedRingSprite;
    }

    private static Sprite GetSlashArcSprite()
    {
        if (_cachedSlashArcSprite == null)
        {
            const int w = 40;
            const int h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float arcCenter = 0.72f - 0.55f * (ny * ny);
                    float dist = Mathf.Abs(nx - arcCenter);
                    float a = (nx <= arcCenter + 0.12f && dist < 0.22f) ? Mathf.Clamp01(1f - dist / 0.22f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _cachedSlashArcSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.2f, 0.5f), 20f);
        }
        return _cachedSlashArcSprite;
    }

    private static Sprite GetProceduralSentinelSprite()
    {
        if (_cachedSentinelSprite != null) return _cachedSentinelSprite;

        const int w = 48;
        const int h = 56;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        Color outline = new Color(0.06f, 0.08f, 0.14f, 1f);
        Color steelDark = new Color(0.18f, 0.23f, 0.34f, 1f);
        Color steelMid = new Color(0.32f, 0.40f, 0.55f, 1f);
        Color steelHi = new Color(0.56f, 0.68f, 0.86f, 1f);
        Color glowCyan = new Color(0.25f, 0.96f, 1f, 1f);
        Color bladeGlow = new Color(1f, 0.32f, 0.38f, 1f);

        void FillRect(int x0, int y0, int rw, int rh, Color c)
        {
            for (int y = y0; y < y0 + rh; y++)
                for (int x = x0; x < x0 + rw; x++)
                    if (x >= 0 && x < w && y >= 0 && y < h)
                        tex.SetPixel(x, y, c);
        }

        // Boots touching y = 0 (Bottom-Center pivot!)
        FillRect(12, 0, 8, 12, outline);
        FillRect(13, 1, 6, 10, steelDark);
        FillRect(13, 8, 6, 3, steelMid);

        FillRect(24, 0, 8, 12, outline);
        FillRect(25, 1, 6, 10, steelDark);
        FillRect(25, 8, 6, 3, steelMid);

        // Armored Waist & Torso
        FillRect(11, 12, 22, 6, outline);
        FillRect(12, 13, 20, 4, steelDark);
        FillRect(9, 18, 26, 20, outline);
        FillRect(10, 19, 24, 18, steelMid);
        FillRect(12, 34, 20, 3, steelHi);

        // Chest Reactor Core
        FillRect(19, 24, 6, 7, outline);
        FillRect(20, 25, 4, 5, glowCyan);

        // Heavy Shoulder Pauldrons
        FillRect(6, 28, 8, 11, outline);
        FillRect(7, 29, 6, 9, steelHi);
        FillRect(30, 27, 9, 12, outline);
        FillRect(31, 28, 7, 10, steelHi);

        // Cyber Helmet + Glowing Visor facing +X
        FillRect(14, 38, 16, 14, outline);
        FillRect(15, 39, 14, 12, steelMid);
        FillRect(17, 52, 10, 3, steelHi);
        FillRect(21, 43, 8, 4, outline);
        FillRect(22, 44, 7, 2, glowCyan);

        // Right Sword Arm & Energy Blade
        FillRect(34, 17, 5, 12, outline);
        FillRect(35, 18, 3, 10, steelDark);
        FillRect(37, 12, 9, 26, outline);
        FillRect(38, 14, 7, 22, bladeGlow);
        FillRect(40, 16, 3, 18, Color.white);

        tex.Apply();
        _cachedSentinelSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 24f);
        return _cachedSentinelSprite;
    }
}

/// <summary>
/// Ground-hugging shockwave pulse launched by Boss_Grounded Jump Slam.
/// Slides across terrain and dissipates cleanly at arena boundaries.
/// </summary>
public class BossGroundShockwave : MonoBehaviour
{
    private float _dir;
    private float _speed;
    private float _damage;
    private float _minX;
    private float _maxX;
    private float _lifetime = 2.2f;
    private bool _hitPlayer;

    public void Initialize(float dir, float speed, float damage, float minX, float maxX, Color tint)
    {
        _dir = Mathf.Sign(dir);
        _speed = speed;
        _damage = damage;
        _minX = minX;
        _maxX = maxX;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tint;
    }

    private void Update()
    {
        _lifetime -= Time.deltaTime;
        transform.position += new Vector3(_dir * _speed * Time.deltaTime, 0f, 0f);
        transform.localScale = new Vector3(0.85f, 0.72f + 0.18f * Mathf.Sin(Time.time * 28f), 1f);

        if (_lifetime <= 0f || transform.position.x <= _minX || transform.position.x >= _maxX)
        {
            JuiceManager.SpawnImpactSparks(transform.position, Color.yellow, -_dir);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitPlayer || other == null) return;
        var ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null || ph.isDead || SafeZone.IsPlayerSafe) return;

        var pc = ph.GetComponent<PlayerCombat>();
        if (pc != null && pc.IsParrying)
        {
            _hitPlayer = true;
            pc.OnParrySuccess(_damage, PlayerHealth.DamageType.TTLDamage);
            Destroy(gameObject);
            return;
        }

        if (!ph.IsInvulnerable)
        {
            _hitPlayer = true;
            ph.TakeDamage(_damage, PlayerHealth.DamageType.TTLDamage);
            var ctrl = ph.GetComponent<PlayerController>();
            if (ctrl != null) ctrl.Knockback(new Vector2(_dir, 0f));
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Cyber bolt spawned by Phase 3 Boss_Grounded Jump Slam.
/// </summary>
public class BossPhaseBolt : MonoBehaviour
{
    private Vector2 _velocity;
    private float _damage;
    private float _lifetime = 2.5f;

    public void Initialize(Vector2 velocity, float damage)
    {
        _velocity = velocity;
        _damage = damage;
    }

    private void Update()
    {
        _lifetime -= Time.deltaTime;
        transform.position += (Vector3)(_velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        if (_lifetime <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        var ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null && !ph.isDead && !SafeZone.IsPlayerSafe)
        {
            var pc = ph.GetComponent<PlayerCombat>();
            if (pc != null && pc.IsParrying)
            {
                pc.OnParrySuccess(_damage, PlayerHealth.DamageType.TTLDamage);
                Destroy(gameObject);
                return;
            }
            if (!ph.IsInvulnerable)
            {
                ph.TakeDamage(_damage, PlayerHealth.DamageType.TTLDamage);
                Destroy(gameObject);
            }
            return;
        }

        if (!other.isTrigger && other.GetComponentInParent<Boss_Grounded>() == null &&
            other.GetComponent<PlatformEffector2D>() == null)
        {
            JuiceManager.SpawnImpactSparks(transform.position, new Color(1f, 0.2f, 0.5f, 1f), 0f);
            Destroy(gameObject);
        }
    }
}
