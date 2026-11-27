using System;
using UnityEngine;

/// <summary>
/// Bridge component connecting Enemy controllers (Enemy_Patrol, Enemy_Flying, Enemy_GlitchPhantom, Enemy_FirewallTurret)
/// to a ModularSpriteAnimator. Automatically forwards state changes, orientation flips, and damage feedback
/// while safely falling back if only a subset of animation states (or a single sprite) was assigned.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ModularSpriteAnimator))]
[AddComponentMenu("PacketCourier/Animation/Enemy Animation Bridge")]
public class EnemyAnimationBridge : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The ModularSpriteAnimator component driving sprites. Auto-assigned if null.")]
    public ModularSpriteAnimator animator;

    [Tooltip("Target enemy component. If null, automatically locates any enemy controller on this GameObject.")]
    public MonoBehaviour enemyController;

    [Header("State Mapping Customization")]
    [Tooltip("State played when idle / stationary / recovering.")]
    public string idleState = "Idle";

    [Tooltip("State played during Patrol / Walk / Fly.")]
    public string patrolState = "Walk";

    [Tooltip("State played during Chase / Aggro.")]
    public string chaseState = "Chase";

    [Tooltip("State played during Attack Windup / Telegraph.")]
    public string windupState = "Windup";

    [Tooltip("State played during Attack / Strike / Dive.")]
    public string attackState = "Attack";

    [Tooltip("State played during Attack Recovery / Cooldown.")]
    public string recoveryState = "Idle";

    [Tooltip("State played during Stun / Hurt.")]
    public string hurtState = "Hurt";

    [Tooltip("State played on Death.")]
    public string deathState = "Death";

    [Header("Auto-Orientation")]
    [Tooltip("Automatically flip sprite facing based on horizontal velocity or movement direction.")]
    public bool autoFacing = true;

    private Enemy_Patrol _patrolEnemy;
    private Enemy_Flying _flyingEnemy;
    private Enemy_GlitchPhantom _phantomEnemy;
    private Enemy_FirewallTurret _turretEnemy;
    private IEnemyDamageable _genericEnemy;
    private Rigidbody2D _rb;

    private int _lastHp = -1;
    private int _lastStateIndex = -1;
    private Vector3 _lastPosition;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<ModularSpriteAnimator>();
        }

        _rb = GetComponent<Rigidbody2D>();
        _lastPosition = transform.position;
        ResolveEnemyController();
        LinkAnimatorToController();
    }

    private void ResolveEnemyController()
    {
        if (enemyController != null)
        {
            _patrolEnemy = enemyController as Enemy_Patrol;
            _flyingEnemy = enemyController as Enemy_Flying;
            _phantomEnemy = enemyController as Enemy_GlitchPhantom;
            _turretEnemy = enemyController as Enemy_FirewallTurret;
            _genericEnemy = enemyController as IEnemyDamageable;
            return;
        }

        _patrolEnemy = GetComponent<Enemy_Patrol>();
        if (_patrolEnemy != null) { enemyController = _patrolEnemy; _genericEnemy = _patrolEnemy; return; }

        _flyingEnemy = GetComponent<Enemy_Flying>();
        if (_flyingEnemy != null) { enemyController = _flyingEnemy; _genericEnemy = _flyingEnemy; return; }

        _phantomEnemy = GetComponent<Enemy_GlitchPhantom>();
        if (_phantomEnemy != null) { enemyController = _phantomEnemy; _genericEnemy = _phantomEnemy; return; }

        _turretEnemy = GetComponent<Enemy_FirewallTurret>();
        if (_turretEnemy != null) { enemyController = _turretEnemy; _genericEnemy = _turretEnemy; return; }

        _genericEnemy = GetComponent<IEnemyDamageable>();
        if (_genericEnemy != null)
        {
            enemyController = _genericEnemy as MonoBehaviour;
        }
    }

    private void LinkAnimatorToController()
    {
        if (animator == null) return;
        if (_patrolEnemy != null && _patrolEnemy.customSpriteAnimator == null) _patrolEnemy.customSpriteAnimator = animator;
        if (_flyingEnemy != null && _flyingEnemy.customSpriteAnimator == null) _flyingEnemy.customSpriteAnimator = animator;
        if (_phantomEnemy != null && _phantomEnemy.customSpriteAnimator == null) _phantomEnemy.customSpriteAnimator = animator;
        if (_turretEnemy != null && _turretEnemy.customSpriteAnimator == null) _turretEnemy.customSpriteAnimator = animator;
    }

    private void Start()
    {
        if (_patrolEnemy != null) _lastHp = _patrolEnemy.hitPoints;
        else if (_flyingEnemy != null) _lastHp = _flyingEnemy.hitPoints;
        else if (_phantomEnemy != null) _lastHp = _phantomEnemy.hitPoints;
        else if (_turretEnemy != null) _lastHp = _turretEnemy.hitPoints;
    }

    private void Update()
    {
        if (animator == null || !animator.HasAnyCustomArt()) return;

        if (_patrolEnemy != null)
        {
            UpdatePatrolEnemy();
        }
        else if (_flyingEnemy != null)
        {
            UpdateFlyingEnemy();
        }
        else if (_phantomEnemy != null)
        {
            UpdatePhantomEnemy();
        }
        else if (_turretEnemy != null)
        {
            UpdateTurretEnemy();
        }
        else if (_genericEnemy != null)
        {
            UpdateGenericEnemy();
        }

        UpdateFacing();
    }

    private void UpdatePatrolEnemy()
    {
        int stateIdx = (int)_patrolEnemy.currentState;
        if (stateIdx != _lastStateIndex)
        {
            _lastStateIndex = stateIdx;
            string targetState = MapPatrolState(_patrolEnemy.currentState);
            PlaySmartState(targetState);
        }
        _lastHp = _patrolEnemy.hitPoints;
    }

    private string MapPatrolState(Enemy_Patrol.State state)
    {
        switch (state)
        {
            case Enemy_Patrol.State.Patrol: return _patrolEnemy.stationary ? idleState : patrolState;
            case Enemy_Patrol.State.Chase: return chaseState;
            case Enemy_Patrol.State.Windup: return windupState;
            case Enemy_Patrol.State.Attack: return attackState;
            case Enemy_Patrol.State.Recovery: return recoveryState;
            case Enemy_Patrol.State.Stunned: return hurtState;
            case Enemy_Patrol.State.Dead: return deathState;
            default: return patrolState;
        }
    }

    private void UpdateFlyingEnemy()
    {
        int stateIdx = (int)_flyingEnemy.currentState;
        if (stateIdx != _lastStateIndex)
        {
            _lastStateIndex = stateIdx;
            string targetState = MapFlyingState(_flyingEnemy.currentState);
            PlaySmartState(targetState);
        }
        _lastHp = _flyingEnemy.hitPoints;
    }

    private string MapFlyingState(Enemy_Flying.State state)
    {
        switch (state)
        {
            case Enemy_Flying.State.Patrol:
            case Enemy_Flying.State.Return:
                return animator.HasValidState("Fly") ? "Fly" : patrolState;
            case Enemy_Flying.State.Chase: return chaseState;
            case Enemy_Flying.State.Windup: return windupState;
            case Enemy_Flying.State.Attack: return attackState;
            case Enemy_Flying.State.Recovery: return recoveryState;
            case Enemy_Flying.State.Stunned: return hurtState;
            case Enemy_Flying.State.Dead: return deathState;
            default: return patrolState;
        }
    }

    private void UpdatePhantomEnemy()
    {
        int stateIdx = _phantomEnemy.isDead ? 99 : (int)_phantomEnemy.currentState;
        if (stateIdx != _lastStateIndex)
        {
            _lastStateIndex = stateIdx;
            if (_phantomEnemy.isDead) PlaySmartState(deathState);
            else
            {
                switch (_phantomEnemy.currentState)
                {
                    case Enemy_GlitchPhantom.PhantomState.Pursuit: PlaySmartState(chaseState); break;
                    case Enemy_GlitchPhantom.PhantomState.Desynced: animator.PlaySmart("Dash", 1, chaseState, patrolState, idleState); break;
                    case Enemy_GlitchPhantom.PhantomState.AmbushWindup: PlaySmartState(windupState); break;
                    case Enemy_GlitchPhantom.PhantomState.LungeAttack: PlaySmartState(attackState); break;
                    default: PlaySmartState(recoveryState); break;
                }
            }
        }
        _lastHp = _phantomEnemy.hitPoints;
    }

    private void UpdateTurretEnemy()
    {
        int stateIdx = _turretEnemy.isDead ? 2 : (_turretEnemy.isTargeting ? 1 : 0);
        if (stateIdx != _lastStateIndex)
        {
            _lastStateIndex = stateIdx;
            if (_turretEnemy.isDead) PlaySmartState(deathState);
            else if (_turretEnemy.isTargeting) PlaySmartState(windupState);
            else PlaySmartState(idleState);
        }
        _lastHp = _turretEnemy.hitPoints;
    }

    private void UpdateGenericEnemy()
    {
        if (_genericEnemy.IsDead)
        {
            PlaySmartState(deathState);
        }
    }

    private void UpdateFacing()
    {
        if (!autoFacing || animator.useExternalFacing) return;

        if (_turretEnemy != null)
        {
            animator.SetFacing(_turretEnemy.facingDirection);
            return;
        }

        float deltaX = transform.position.x - _lastPosition.x;
        _lastPosition = transform.position;

        if (_rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.05f)
        {
            animator.SetFacing(_rb.linearVelocity.x);
        }
        else if (Mathf.Abs(deltaX) > 0.001f)
        {
            animator.SetFacing(deltaX);
        }
    }

    public bool PlaySmartState(string requestedState)
    {
        if (animator == null || string.IsNullOrEmpty(requestedState)) return false;

        string fallback = ResolveFallback(requestedState);
        return animator.PlaySmart(requestedState, 1, fallback, idleState, patrolState);
    }

    private string ResolveFallback(string state)
    {
        if (state == chaseState) return patrolState;
        if (state == windupState) return attackState;
        if (state == recoveryState) return idleState;
        if (state == hurtState) return idleState;
        return string.Empty;
    }
}
