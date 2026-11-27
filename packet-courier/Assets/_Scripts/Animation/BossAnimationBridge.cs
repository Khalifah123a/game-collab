using System;
using UnityEngine;

/// <summary>
/// Bridge component connecting Boss controllers (Boss_Grounded, Boss_Solaris, Boss_TheCensor, Boss_Rootkit, IBossController)
/// to a ModularSpriteAnimator. Translates boss combat states, 3-phase transitions, and damage feedback
/// into animator calls with smart fallbacks (including optional phase-specific states like Slash_P2 / Slash_P3).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ModularSpriteAnimator))]
[AddComponentMenu("PacketCourier/Animation/Boss Animation Bridge")]
public class BossAnimationBridge : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The ModularSpriteAnimator component driving sprites. Auto-assigned if null.")]
    public ModularSpriteAnimator animator;

    [Tooltip("Target Boss component. If empty, automatically locates any boss controller on this GameObject.")]
    public MonoBehaviour bossController;

    [Header("State Mapping Customization")]
    [Tooltip("Animation state name played during Idle / Dormant / Intro.")]
    public string idleState = "Idle";

    [Tooltip("Animation state name played during Walk / Stalk.")]
    public string walkState = "Walk";

    [Tooltip("Animation state name played during Windup / Telegraph.")]
    public string windupState = "Windup";

    [Tooltip("Animation state name played during Slash / Attack.")]
    public string slashState = "Slash";

    [Tooltip("Fallback Attack state name if Slash is empty.")]
    public string attackState = "Attack";

    [Tooltip("Animation state name played during Dash / Charge.")]
    public string dashState = "Dash";

    [Tooltip("Animation state name played during Jump.")]
    public string jumpState = "Jump";

    [Tooltip("Animation state name played during Slam / Fall.")]
    public string slamState = "Slam";

    [Tooltip("Animation state name played during Hurt / Stun / Exhausted.")]
    public string hurtState = "Hurt";

    [Tooltip("Animation state name played during Phase Shift / Overdrive.")]
    public string phaseShiftState = "PhaseShift";

    [Tooltip("Animation state name played on Death.")]
    public string deathState = "Death";

    [Header("Auto-Orientation")]
    [Tooltip("Automatically orient sprite facing towards movement direction (disabled automatically for Boss_Grounded which flips VisualRoot itself).")]
    public bool autoFacing = true;

    private Boss_Grounded _groundedBoss;
    private Boss_TheCensor _censorBoss;
    private Boss_Solaris _solarisBoss;
    private Boss_Rootkit _rootkitBoss;
    private IBossController _genericBoss;
    private Rigidbody2D _rb;

    private int _lastHp = -1;
    private int _lastPhase = -1;
    private string _activeMappedState = string.Empty;
    private Boss_Grounded.BossState _lastGroundedState = (Boss_Grounded.BossState)(-1);
    private bool _lastGroundedMoving;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<ModularSpriteAnimator>();
        }

        _rb = GetComponent<Rigidbody2D>();
        ResolveBossController();

        if (_groundedBoss != null && animator != null)
        {
            _groundedBoss.customSpriteAnimator = animator;
            animator.useExternalFacing = true;
            if (_groundedBoss.bodyRenderer != null)
            {
                animator.targetRenderer = _groundedBoss.bodyRenderer;
            }
        }
    }

    private void Start()
    {
        if (_groundedBoss != null && animator != null && _groundedBoss.bodyRenderer != null)
        {
            animator.targetRenderer = _groundedBoss.bodyRenderer;
            animator.useExternalFacing = true;
        }
        if (_genericBoss != null)
        {
            _lastHp = _genericBoss.HitPoints;
        }
    }

    private void ResolveBossController()
    {
        if (bossController != null)
        {
            _groundedBoss = bossController as Boss_Grounded;
            _censorBoss = bossController as Boss_TheCensor;
            _solarisBoss = bossController as Boss_Solaris;
            _rootkitBoss = bossController as Boss_Rootkit;
            _genericBoss = bossController as IBossController;
            return;
        }

        _groundedBoss = GetComponent<Boss_Grounded>();
        if (_groundedBoss != null) { bossController = _groundedBoss; _genericBoss = _groundedBoss; return; }

        _censorBoss = GetComponent<Boss_TheCensor>();
        if (_censorBoss != null) { bossController = _censorBoss; _genericBoss = _censorBoss; return; }

        _solarisBoss = GetComponent<Boss_Solaris>();
        if (_solarisBoss != null) { bossController = _solarisBoss; _genericBoss = _solarisBoss; return; }

        _rootkitBoss = GetComponent<Boss_Rootkit>();
        if (_rootkitBoss != null) { bossController = _rootkitBoss; _genericBoss = _rootkitBoss; return; }

        _genericBoss = GetComponent<IBossController>();
        if (_genericBoss != null)
        {
            bossController = _genericBoss as MonoBehaviour;
        }
    }

    private void Update()
    {
        if (animator == null || !animator.HasAnyCustomArt()) return;

        if (_groundedBoss != null)
        {
            UpdateGroundedBoss();
        }
        else if (_censorBoss != null)
        {
            UpdateLegacyBoss(_censorBoss.IsDead, _censorBoss.HitPoints, 1);
        }
        else if (_solarisBoss != null)
        {
            UpdateLegacyBoss(_solarisBoss.IsDead, _solarisBoss.HitPoints, 1);
        }
        else if (_rootkitBoss != null)
        {
            UpdateLegacyBoss(_rootkitBoss.IsDead, _rootkitBoss.HitPoints, 1);
        }
        else if (_genericBoss != null)
        {
            UpdateLegacyBoss(_genericBoss.IsDead, _genericBoss.HitPoints, 1);
        }
    }

    private void UpdateGroundedBoss()
    {
        Boss_Grounded.BossState current = _groundedBoss.CurrentState;
        int phase = _groundedBoss.CurrentPhase;
        bool isMoving = _rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.15f;

        if (current != _lastGroundedState || (current == Boss_Grounded.BossState.Stalk && isMoving != _lastGroundedMoving))
        {
            _lastGroundedState = current;
            _lastGroundedMoving = isMoving;
            PlayGroundedState(current, phase, isMoving);
        }

        if (_lastPhase > 0 && phase > _lastPhase)
        {
            PlaySmartState(phaseShiftState, phase);
        }
        _lastPhase = phase;
        _lastHp = _groundedBoss.HitPoints;
    }

    private void PlayGroundedState(Boss_Grounded.BossState state, int phase, bool isMoving)
    {
        switch (state)
        {
            case Boss_Grounded.BossState.Dormant:
            case Boss_Grounded.BossState.Intro:
                animator.PlaySmart(idleState, phase);
                break;

            case Boss_Grounded.BossState.Stalk:
                animator.PlaySmart(isMoving ? walkState : idleState, phase, idleState, walkState);
                break;

            case Boss_Grounded.BossState.WindupCleave:
                animator.PlaySmart(windupState, phase, slashState, attackState);
                break;

            case Boss_Grounded.BossState.AttackCleave:
                animator.PlaySmart(slashState, phase, attackState);
                break;

            case Boss_Grounded.BossState.WindupDash:
                animator.PlaySmart(windupState, phase, dashState, walkState);
                break;

            case Boss_Grounded.BossState.AttackDash:
                animator.PlaySmart(dashState, phase, walkState, attackState);
                break;

            case Boss_Grounded.BossState.JumpUp:
                animator.PlaySmart(jumpState, phase, "JumpSlam", walkState);
                break;

            case Boss_Grounded.BossState.SlamFall:
            case Boss_Grounded.BossState.SlamRecover:
                animator.PlaySmart(slamState, phase, "JumpSlam", attackState, slashState);
                break;

            case Boss_Grounded.BossState.PhaseShift:
                animator.PlaySmart(phaseShiftState, phase, "Special", attackState, slashState);
                break;

            case Boss_Grounded.BossState.Stunned:
            case Boss_Grounded.BossState.Exhausted:
                animator.PlaySmart(hurtState, phase, "Exhausted", idleState);
                break;

            case Boss_Grounded.BossState.Dead:
                animator.PlaySmart(deathState, phase, "Die", hurtState);
                break;

            default:
                animator.PlaySmart(idleState, phase);
                break;
        }
    }

    private void UpdateLegacyBoss(bool isDead, int currentHp, int currentPhase)
    {
        if (isDead)
        {
            PlaySmartState(deathState, currentPhase);
            return;
        }

        if (_lastHp > 0 && currentHp < _lastHp)
        {
            animator.FlashColor(new Color(1f, 0.35f, 0.35f, 1f), 0.10f);
        }
        _lastHp = currentHp;

        if (_lastPhase > 0 && currentPhase > _lastPhase)
        {
            PlaySmartState(phaseShiftState, currentPhase);
        }
        _lastPhase = currentPhase;

        if (autoFacing && !animator.useExternalFacing && _rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.1f)
        {
            animator.SetFacing(_rb.linearVelocity.x);
        }
    }

    /// <summary>
    /// Plays the requested state with phase-aware smart fallbacks when friend assets only provide a subset of states.
    /// </summary>
    public bool PlaySmartState(string requestedState, int phase = 1)
    {
        if (animator == null || string.IsNullOrEmpty(requestedState)) return false;

        string fallback = ResolveFallback(requestedState);
        bool played = animator.PlaySmart(requestedState, phase, fallback, attackState, idleState);
        if (played) _activeMappedState = animator.CurrentStateName;
        return played;
    }

    private string ResolveFallback(string state)
    {
        if (state == windupState) return attackState;
        if (state == slashState) return attackState;
        if (state == dashState) return walkState;
        if (state == jumpState || state == slamState) return walkState;
        if (state == phaseShiftState) return attackState;
        if (state == hurtState) return idleState;
        return string.Empty;
    }
}
