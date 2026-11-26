using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PlayerController), typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    public float windup = 0.05f, activeTime = 0.12f, recovery = 0.14f;
    public float reach = 1.35f;
    public int damage = 1;
    public bool IsAttacking { get; private set; }
    public bool IsParrying => Time.time < _parryUntil;
    public float ParryActiveRemaining => Mathf.Max(0f, _parryUntil - Time.time);
    public float ShootCooldownRemaining => Mathf.Max(0f, _shootCooldownTimer - Time.time);
    public const float ShootCooldownDuration = 0.32f;

    private PlayerController player;
    private PlayerHealth health;
    private float elapsed, facing;
    private readonly HashSet<IEnemyDamageable> struck = new HashSet<IEnemyDamageable>();
    private LineRenderer slash;
    private Material slashMaterial;
    private SpriteRenderer _slashSpriteRenderer;
    private Transform _slashTransform;

    private float _parryUntil;
    private float _shootCooldownTimer;
    private SpriteRenderer _parryShieldRenderer;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();

        // Cyberpunk Neon Data-Blade Slash Visual
        var visual = new GameObject("CyberSlashBlade", typeof(SpriteRenderer));
        visual.transform.SetParent(transform, false);
        _slashTransform = visual.transform;
        _slashSpriteRenderer = visual.GetComponent<SpriteRenderer>();
        _slashSpriteRenderer.sprite = Resources.Load<Sprite>("GeneratedArt/cyber_slash_arc");
        _slashSpriteRenderer.sortingOrder = 30;
        _slashSpriteRenderer.enabled = false;

        var shieldGo = new GameObject("ParryShieldVisual", typeof(SpriteRenderer));
        shieldGo.transform.SetParent(transform, false);
        _parryShieldRenderer = shieldGo.GetComponent<SpriteRenderer>();
        _parryShieldRenderer.sprite = Resources.Load<Sprite>("GeneratedArt/shield_parry");
        _parryShieldRenderer.sortingOrder = 32;
        _parryShieldRenderer.enabled = false;
    }

    public bool TryAttack()
    {
        if (Time.timeScale == 0 || health.isDead || player.isFrozen || player.isDashing || IsAttacking) return false;
        IsAttacking = true;
        elapsed = 0;
        facing = player.FacingDir;
        struck.Clear();
        if (player.isGrounded)
        {
            player.AddAttackImpulse(new Vector2(facing * 1.5f, 0f));
        }
        player.GetComponent<PlayerAnimator>()?.NotifyAttack();
        AudioManager.PlayAttack();
        return true;
    }

    private static int ShopLevel(int idx)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.shopLevels == null || idx < 0 || idx >= gm.shopLevels.Length) return 0;
        return gm.shopLevels[idx];
    }

    public int GetDamage() => damage + ShopLevel(1) + ShopLevel(2) + OverclockManager.BonusDamage;
    public float GetReach() => reach + 0.25f * ShopLevel(3) + 0.1f * ShopLevel(2);
    public float GetRecovery() => Mathf.Max(0.05f, recovery * Mathf.Pow(0.9f, ShopLevel(4)));

    public bool TryShoot()
    {
        if (Time.timeScale == 0 || health.isDead || player.isFrozen || player.isDashing || IsAttacking || IsParrying) return false;
        if (!OverclockManager.HasBlaster) return false;
        if (Time.time < _shootCooldownTimer) return false;

        _shootCooldownTimer = Time.time + 0.32f;
        float dir = player.FacingDir;
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(dir * 0.85f, 0.2f);
        int projDamage = 1 + OverclockManager.BlasterLevel + OverclockManager.BonusDamage;
        CyberProjectile.Spawn(spawnPos, dir, projDamage);
        AudioManager.PlayShoot();

        // Flash recoil
        player.GetComponent<PlayerAnimator>()?.NotifyAttack();
        return true;
    }

    public bool TryParry()
    {
        if (Time.timeScale == 0 || health.isDead || player.isFrozen || player.isDashing || IsAttacking || IsParrying) return false;
        if (!OverclockManager.HasParry) return false;

        _parryUntil = Time.time + 0.25f;
        if (_parryShieldRenderer != null)
        {
            _parryShieldRenderer.enabled = true;
            _parryShieldRenderer.transform.localPosition = new Vector3(player.FacingDir * 0.75f, 0.15f, 0f);
            _parryShieldRenderer.color = new Color(0.2f, 0.95f, 1f, 0.95f);
        }
        return true;
    }

    public void OnParrySuccess(float incomingDamage, PlayerHealth.DamageType type)
    {
        _parryUntil = 0f;
        if (_parryShieldRenderer != null) _parryShieldRenderer.enabled = false;

        // Counter Shockwave: damage all nearby enemies
        int counterDamage = 3 + OverclockManager.ParryLevel * 2 + OverclockManager.BonusDamage;
        Vector2 center = transform.position;
        float blastRadius = 3.6f;

        foreach (var col in Physics2D.OverlapCircleAll(center, blastRadius))
        {
            var enemy = col.GetComponentInParent(typeof(IEnemyDamageable)) as IEnemyDamageable;
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeHit(counterDamage);
            }
        }

        // Reward player: restore TTL and Stamina
        health.ttlCurrent = Mathf.Min(health.ttlMax, health.ttlCurrent + 12f);
        player.GetComponent<PlayerStamina>()?.AddStamina(25f);

        // Spawn visual shockwave burst
        SpawnParryCounterBurst(center);
        AudioManager.PlayParry();
        JuiceManager.Hitstop(0.06f);
        JuiceManager.Shake(0.45f);
    }

    private static void SpawnParryCounterBurst(Vector2 pos)
    {
        var fxGo = new GameObject("ParryShockwave");
        fxGo.transform.position = pos;
        var sr = fxGo.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/key_e_ring");
        sr.color = new Color(0.3f, 1f, 0.9f, 0.95f);
        sr.sortingOrder = 45;
        sr.transform.localScale = Vector3.one * 1.5f;

        var anim = fxGo.AddComponent<FadeAndGrowFX>();
        anim.duration = 0.3f;
        anim.targetScale = 4.5f;
    }

    private void Update()
    {
        var input = InputManager.Instance;
        if (input && input.attackPressed)
        {
            input.ConsumeAttack();
            if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject()) TryAttack();
        }

        // Key F or RMB for Cyber Blaster
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryShoot();
        }

        // Key Q for Nano Parry
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryParry();
        }

        // Secondary fire (RMB) context: Parry if available, else Shoot
        if (Input.GetMouseButtonDown(1))
        {
            if (OverclockManager.HasParry) TryParry();
            else if (OverclockManager.HasBlaster) TryShoot();
        }

        // Update parry visual state
        if (_parryShieldRenderer != null)
        {
            bool parrying = IsParrying;
            _parryShieldRenderer.enabled = parrying;
            if (parrying)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 25f);
                _parryShieldRenderer.transform.localScale = Vector3.one * pulse;
                _parryShieldRenderer.transform.localPosition = new Vector3(player.FacingDir * 0.75f, 0.15f, 0f);
            }
        }
        if (Time.timeScale == 0) return;
        if (health.isDead || player.isFrozen || player.isDashing)
        {
            CancelAttack();
            return;
        }
        if (!IsAttacking) return;
        elapsed += Time.deltaTime;
        bool active = elapsed >= windup && elapsed < windup + activeTime;
        if (_slashSpriteRenderer != null) _slashSpriteRenderer.enabled = active;
        if (active)
        {
            float curReach = GetReach();
            DrawSlash((elapsed - windup) / activeTime, curReach);
            Vector2 center = (Vector2)transform.position + new Vector2(facing * (curReach * 0.5f), 0.08f);
            foreach (var collider in Physics2D.OverlapBoxAll(center, new Vector2(curReach, 1.15f), 0))
            {
                var enemy = collider.GetComponentInParent(typeof(IEnemyDamageable)) as IEnemyDamageable;
                if (enemy == null || enemy.IsDead || struck.Contains(enemy) || IsObstructed(enemy)) continue;
                struck.Add(enemy);
                try
                {
                    Vector3 hitPos = (enemy.HitTransform != null) ? enemy.HitTransform.position : (Vector3)center;
                    bool wasDead = enemy.IsDead;
                    enemy.TakeHit(GetDamage());
                    bool isDeadNow = enemy.IsDead;

                    if (!wasDead && isDeadNow)
                    {
                        JuiceManager.Hitstop(0.075f);
                        JuiceManager.Shake(0.26f);
                        JuiceManager.PunchCamera(new Vector2(facing, 0.15f), 0.18f);
                    }
                    else
                    {
                        JuiceManager.Hitstop(0.045f);
                        JuiceManager.Shake(0.14f);
                        JuiceManager.PunchCamera(new Vector2(facing, 0.08f), 0.11f);
                    }

                    // Tactile melee recoil so blade impacts feel solid
                    if (player != null)
                    {
                        var prb = player.GetComponent<Rigidbody2D>();
                        if (prb != null)
                        {
                            prb.linearVelocity = new Vector2(-facing * 1.15f, prb.linearVelocity.y);
                        }
                    }

                    JuiceManager.SpawnImpactSparks(hitPos, new Color(0.75f, 0.96f, 1f, 1f), facing);
                    AudioManager.PlayHitImpact();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PlayerCombat] Error during enemy strike: {ex.Message}");
                }
            }
        }
        if (elapsed >= windup + activeTime + GetRecovery()) CancelAttack();
    }

    private bool IsObstructed(IEnemyDamageable enemy)
    {
        if (enemy == null || enemy.HitTransform == null) return false;
        Vector2 start = transform.position;
        Vector2 delta = (Vector2)enemy.HitTransform.position - start;
        foreach (var hit in Physics2D.RaycastAll(start, delta.normalized, delta.magnitude))
            if (!hit.collider.isTrigger && hit.collider.attachedRigidbody != player.GetComponent<Rigidbody2D>() &&
                hit.collider.GetComponentInParent(typeof(IEnemyDamageable)) == null) return true;
        return false;
    }

    private void DrawSlash(float progress, float curReach)
    {
        if (_slashSpriteRenderer == null) return;
        _slashSpriteRenderer.enabled = true;

        // Snappy cyber blade slash arc aligned with attack hitbox
        float currentAngle = Mathf.Lerp(38f * facing, -38f * facing, progress);

        _slashTransform.localPosition = new Vector3(facing * 0.20f, 0.05f, 0f);
        _slashTransform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

        float punchScale = Mathf.Lerp(0.85f, 1.15f, Mathf.Sin(progress * Mathf.PI));
        float reachScale = curReach / 1.35f;
        _slashTransform.localScale = new Vector3(facing * reachScale * punchScale, reachScale * punchScale, 1f);

        Color col = Color.Lerp(new Color(0.7f, 1f, 1f, 0.95f), new Color(0.1f, 0.95f, 1f, 0.1f), progress);
        _slashSpriteRenderer.color = col;
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        if (_slashSpriteRenderer != null) _slashSpriteRenderer.enabled = false;
        if (slash != null) slash.enabled = false;
    }
    private void OnDisable() { CancelAttack(); }
    private void OnDestroy() { if (slashMaterial) Destroy(slashMaterial); }
}

public class FadeAndGrowFX : MonoBehaviour
{
    public float duration = 0.3f;
    public float targetScale = 4f;
    private SpriteRenderer _sr;
    private Vector3 _startScale;
    private Color _startColor;
    private float _timer;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _startScale = transform.localScale;
        if (_sr != null) _startColor = _sr.color;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / duration);
        transform.localScale = Vector3.Lerp(_startScale, Vector3.one * targetScale, t);
        if (_sr != null)
        {
            _sr.color = new Color(_startColor.r, _startColor.g, _startColor.b, (1f - t) * _startColor.a);
        }
        if (t >= 1f) Destroy(gameObject);
    }
}
