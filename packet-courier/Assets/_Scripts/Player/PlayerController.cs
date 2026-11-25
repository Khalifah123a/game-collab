using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 1200f;
    public float groundDecel = 900f;
    public float airDecel = 600f;
    public float gravity = 1500f;
    public float maxFallSpeed = 500f;

    [Header("Jump")]
    public float jumpForce = 400f;
    public float coyoteTime = 0.067f;
    public float jumpBufferTime = 0.1f;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    [Header("Air Jump")]
    public int airJumpsAvailable = 1;
    public int airJumpsLeft;

    [Header("Walljump")]
    public float walljumpForceX = 300f;
    public float walljumpForceY = 380f;
    public float wallClingTime = 0.5f;
    public float wallClingTimer;
    public int walljumpsAvailable = 1;
    public int walljumpsLeft;
    public float wallUnstickTime = 0.3f;
    public float wallUnstickTimer;
    [Header("Wall Cling (Stage 2)")]
    [Tooltip("Downward slide speed while stuck to a wall. Small values read as hanging, not climbing.")]
    public float clingSlideSpeed = 1f;
    [Tooltip("Cling stamina spent per second (before the WallClingTime upgrade discount).")]
    public float clingDrainRate = 28f;
    [Tooltip("Seconds the wall is ignored after cling stamina runs out, so the player actually peels off.")]
    public float clingFallLockout = 0.4f;
    public bool isClinging;
    private float _clingLockTimer;

    [Header("Dash")]
    public float dashSpeed = 700f;
    public float dashDuration = 0.15f;
    public float dashCooldownTimer;
    public int airDashesLeft = 1;
    public bool isDashing;
    public float dashGravityScale = 0.6f;
    public LayerMask dashBlockMask;
    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.5f;
    public float sprintAnimMultiplier = 1.8f;
    public bool isSprinting;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public Vector2 groundCheckSize = new Vector2(0.4f, 0.2f);
    public bool isGrounded;

    [Header("Wall Check")]
    public Vector2 wallCheckSize = new Vector2(0.5f, 2f);
    public float wallCheckOffset = 0.5f;
    public bool isOnWall;
    public int wallSide;

    [Header("State")]
    public float FacingDir = 1;
    public bool isFrozen;

    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private PlayerMode _mode;
    private InputManager _input;
    private PlayerHealth _health;
    private PlayerAnimator _animator;

    private Vector2 _moveInput;
    private Vector2 _dashDir;
    private float _dashTimer;
    private Collider2D _currentGround;
    private float _dropTimer;
    private float _dropHoldTimer;
    private bool _warnedNoInput;
    private PlayerStamina _stamina;
    private bool _wasGrounded;
    private float _hurtUntil;
    private bool _sprintExhausted;
    private float _dashRemaining;
    private float _wallJumpDirection;
    private float _footstepTimer;
    private float _wallSparksTimer;
    private float _lastJumpTime = -10f;
    private Platform_Moving _currentMovingPlatform;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();
        _mode = GetComponent<PlayerMode>();
        _health = GetComponent<PlayerHealth>();
        _animator = GetComponent<PlayerAnimator>();
        _stamina = GetComponent<PlayerStamina>();
        if (!_stamina) _stamina = gameObject.AddComponent<PlayerStamina>();
        if (GetComponent<OverclockManager>() == null) gameObject.AddComponent<OverclockManager>();
        if (GetComponent<DashGhostTrail>() == null) gameObject.AddComponent<DashGhostTrail>();

        _rb.gravityScale = 0;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (groundMask == 0)
            groundMask = 1;
    }

    private void Start()
    {
        _input = InputManager.Instance;
        _mode = GetComponent<PlayerMode>();
        walljumpsLeft = walljumpsAvailable;
        airDashesLeft = 1;
        airJumpsLeft = GetAirJumps();
    }

    private void Update()
    {
        if (isFrozen)
        {
            bool anyModalOpen = DialogUI.IsOpen || 
                                ShopUI.IsShopOpen || 
                                OverclockUI.IsOverclockOpen || 
                                SoftwareQuizUI.IsQuizOpen || 
                                PauseMenuUI.IsPaused;
            if (!anyModalOpen || Input.GetKeyDown(KeyCode.Escape))
            {
                isFrozen = false;
            }
            else
            {
                if (_rb != null) _rb.linearVelocity = Vector2.zero;
                return;
            }
        }
        if (Time.timeScale == 0) return;
        if (_health == null || _health.isDead) return;

        HandleWallInput();
        CheckGrounded();
        CheckWall();
        HandleTimers();
        HandleDropInput();
        HandleJumpInput();
        HandleDashInput();
        UpdateFacing();
        HandleSprintInput();
        HandleFootsteps();
    }

    private void FixedUpdate()
    {
        if (isFrozen)
        {
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            return;
        }
        if (_health == null || _health.isDead) return;
        if (Time.time < _hurtUntil) { ApplyGravity(); return; }

        if (isDashing)
        {
            StopCling();
            FixedDash();
        }
        else if (isOnWall && wallClingTimer > 0 && (_moveInput.x * wallSide > 0.1f) && (_stamina == null || !_stamina.clingExhausted))
        {
            isClinging = true;
            FixedWallCling();
        }
        else
        {
            StopCling();
            FixedMove();
        }

        // While clinging the slide speed is authored in FixedWallCling, so gravity stays off.
        if (!isDashing && !isClinging) ApplyGravity();
    }

    private void StopCling()
    {
        if (!isClinging) return;
        isClinging = false;
        if (_stamina != null) _stamina.EndCling();
    }

    /// <summary>Cling stamina per second, discounted by the WallClingTime upgrade.</summary>
    private float ClingDrainPerSecond()
    {
        float rate = _stamina != null ? _stamina.clingDrainRate : clingDrainRate;
        return Mathf.Max(rate * 0.5f, rate - rate * 0.15f * GetUpgradeBonus(GameManager.UpgradeType.WallClingTime));
    }

    private void CheckGrounded()
    {
        if (_dropTimer > 0)
        {
            isGrounded = false;
            _currentMovingPlatform = null;
            _coyoteTimer -= Time.deltaTime;
            return;
        }

        bool recentlyJumped = (Time.time - _lastJumpTime) < 0.09f;
        float downOffset = (!recentlyJumped && _wasGrounded && _currentMovingPlatform != null) ? 0.16f : 0.06f;
        Vector2 checkSize = (!recentlyJumped && _wasGrounded && _currentMovingPlatform != null)
            ? new Vector2(groundCheckSize.x, groundCheckSize.y + 0.14f)
            : groundCheckSize;

        Vector2 pos = new Vector2(_col.bounds.center.x, _col.bounds.min.y) + Vector2.down * downOffset;
        Collider2D hit = FindSolid(pos, checkSize);
        Platform_Moving movingPlat = hit != null ? hit.GetComponentInParent<Platform_Moving>() : null;
        float platVelY = movingPlat != null ? movingPlat.CurrentVelocity.y : 0f;
        float relVelY = _rb.linearVelocity.y - platVelY;

        isGrounded = hit != null && !recentlyJumped && (relVelY <= 0.25f || movingPlat != null);
        _currentMovingPlatform = isGrounded ? movingPlat : null;

        if (!_wasGrounded && isGrounded && _rb != null && relVelY <= 0.05f)
        {
            JuiceManager.SpawnDustPuff(transform.position);
            AudioManager.PlayLand();
        }
        _wasGrounded = isGrounded;

        if (isGrounded)
        {
            _coyoteTimer = coyoteTime;
            walljumpsLeft = walljumpsAvailable;
            airDashesLeft = 1;
            airJumpsLeft = GetAirJumps();
            _currentGround = hit;
            _clingLockTimer = 0;
            StopCling();
            _stamina?.ResetClingExhausted();
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }
    }

    private void CheckWall()
    {
        if (wallUnstickTimer > 0 || _clingLockTimer > 0) { isOnWall = false; return; }
        Vector2 pos = (Vector2)transform.position;
        bool wallLeft = FindSolid(pos + Vector2.left * wallCheckOffset, wallCheckSize, true);
        bool wallRight = FindSolid(pos + Vector2.right * wallCheckOffset, wallCheckSize, true);

        if (!isGrounded && (wallLeft || wallRight))
        {
            isOnWall = true;
            wallSide = wallRight ? 1 : -1;
            if (!isDashing)
            {
                wallClingTimer = wallClingTime + GetUpgradeBonus(GameManager.UpgradeType.WallClingTime) * 0.2f;
                wallUnstickTimer = 0;
            }
        }
        else
        {
            isOnWall = false;
            wallSide = 0;
        }
    }

    private void HandleTimers()
    {
        if (_jumpBufferTimer > 0) _jumpBufferTimer -= Time.deltaTime;
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (wallClingTimer > 0) wallClingTimer -= Time.deltaTime;
        if (wallUnstickTimer > 0) wallUnstickTimer -= Time.deltaTime;
        if (_clingLockTimer > 0) _clingLockTimer -= Time.deltaTime;

        if (_dropTimer > 0)
        {
            _dropTimer -= Time.deltaTime;
            if (_dropTimer <= 0 && _currentGround != null)
            {
                Physics2D.IgnoreCollision(_col, _currentGround, false);
                _currentGround = null;
            }
        }

    }

    private void HandleJumpInput()
    {
        if (_input == null) return;
        if (_input.jumpPressed && !isDashing && Time.time >= _hurtUntil)
        {
            float jMultiplier = PacketIntegrityManager.Instance?.ActiveModifier?.jumpMultiplier ?? 1.0f;
            if (isGrounded || _coyoteTimer > 0)
            {
                DoJump(Vector2.up, jumpForce * jMultiplier);
                _coyoteTimer = 0;
                AudioManager.PlayJump();
            }
            else if (airJumpsLeft > 0 && !isOnWall)
            {
                DoJump(Vector2.up, jumpForce * jMultiplier);
                airJumpsLeft--;
                _animator?.NotifyDoubleJump();
                _coyoteTimer = 0;
                SpawnAirJumpPulse(transform.position - new Vector3(0f, 0.45f, 0f));
                AudioManager.PlayDoubleJump();
            }
            else if (isOnWall && wallClingTimer > 0 && walljumpsLeft > 0)
            {
                DoWalljump();
            }
        }
    }

    private void DoJump(Vector2 dir, float force)
    {
        _lastJumpTime = Time.time;
        isGrounded = false;
        if (_currentMovingPlatform != null)
        {
            _currentMovingPlatform.NotifyPassengerJumped();
            _currentMovingPlatform = null;
        }
        if (isOnWall) isOnWall = false;
        StopCling();
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
        _rb.AddForce(dir * force, ForceMode2D.Impulse);
        _input.ConsumeJump();
        JuiceManager.SpawnDustPuff(transform.position);
    }

    private void DoWalljump()
    {
        _wallJumpDirection = -wallSide;
        walljumpsLeft--;
        isOnWall = false;
        wallClingTimer = 0;
        wallUnstickTimer = wallUnstickTime;
        StopCling();

        Vector2 jumpDir = new Vector2(-wallSide * walljumpForceX, walljumpForceY);
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(jumpDir, ForceMode2D.Impulse);
        _input.ConsumeJump();
    }

    private void HandleWallInput()
    {
        if (_input == null)
        {
            _input = InputManager.EnsureExists();
            if (_input == null)
            {
                if (!_warnedNoInput)
                {
                    Debug.LogWarning("[PlayerController] InputManager tidak ditemukan di scene. Jalankan Tools > Packet Courier > Setup Scene.", this);
                    _warnedNoInput = true;
                }
                return;
            }
        }

        _moveInput = _input.useTouchInput ? _input.touchMoveDir : _input.moveInput;
    }

    private void HandleDropInput()
    {
        bool downHeld = _moveInput.y < -0.5f;
        bool wantDrop = downHeld && isGrounded && _currentGround != null && _currentGround.usedByEffector && _dropTimer <= 0;

        if (wantDrop)
        {
            // Intentional drop: Down + Jump, or holding Down deliberately for > 0.45s
            bool dropPressed = _input != null && _input.jumpPressed;
            _dropHoldTimer += Time.deltaTime;

            if (dropPressed || _dropHoldTimer >= 0.45f)
            {
                if (dropPressed && _input != null) _input.ConsumeJump();
                Physics2D.IgnoreCollision(_col, _currentGround, true);
                isGrounded = false;
                _dropTimer = 0.35f;
                _dropHoldTimer = 0f;
            }
        }
        else
        {
            _dropHoldTimer = 0f;
        }
    }

    private void HandleDashInput()
    {
        if (_input == null) return;
        if (!_input.dashPressed) return;
        if (Time.time >= _hurtUntil && dashCooldownTimer <= 0 && airDashesLeft > 0 && !isDashing && _stamina.TryDash()) StartDash();
        _input.ConsumeDash();
    }

    private void StartDash()
    {
        isDashing = true;
        AudioManager.PlayDash();
        _dashTimer = dashDuration;
        dashCooldownTimer = _mode.GetDashCooldown();
        _dashDir = _input.dashDirection;
        if (_dashDir.sqrMagnitude < 0.01f) _dashDir = new Vector2(FacingDir, 0);
        _dashDir.Normalize();
        _dashRemaining = _mode.GetDashDistance();
        dashSpeed = _dashRemaining / Mathf.Max(0.05f, dashDuration);

        if (_dashDir.y == 0)
            _rb.gravityScale = dashGravityScale;
        else
            _rb.gravityScale = 0;

        _rb.linearVelocity = _dashDir * dashSpeed;
        _input.ConsumeDash();

        if (!isGrounded)
            airDashesLeft--;

        _rb.gravityScale = 0;
    }

    private void EndDash()
    {
        isDashing = false;
        _rb.gravityScale = 0;
        _rb.linearVelocity = Vector2.zero;
        _col.enabled = true;
    }

    private void FixedDash()
    {
        float step = Mathf.Min(_dashRemaining, dashSpeed * Time.fixedDeltaTime);
        if (step <= 0.001f) { EndDash(); return; }
        var filter = new ContactFilter2D();
        filter.SetLayerMask(dashBlockMask.value == 0 ? groundMask : dashBlockMask);
        filter.useTriggers = false;
        int count = _col.Cast(_dashDir, filter, _hits, step + 0.02f);
        bool blocked = false;
        for (int i = 0; i < count; i++)
        {
            if (_hits[i].normal == Vector2.zero || Vector2.Dot(_hits[i].normal, _dashDir) >= -0.01f) continue;
            if (_hits[i].collider.GetComponent<PlatformEffector2D>() && _dashDir.y >= 0) continue;
            step = Mathf.Min(step, Mathf.Max(0, _hits[i].distance - 0.02f));
            blocked = true;
        }
        _rb.linearVelocity = _dashDir * (step / Time.fixedDeltaTime);
        _dashRemaining = blocked ? 0 : _dashRemaining - step;
    }

    private Collider2D FindSolid(Vector2 point, Vector2 size, bool forWall = false)
    {
        foreach (var hit in Physics2D.OverlapBoxAll(point, size, 0, groundMask))
            if (hit != _col && !hit.isTrigger && !hit.GetComponent<Enemy_Patrol>() && (!forWall || !hit.usedByEffector)) return hit;
        return null;
    }

    public void Knockback(Vector2 direction)
    {
        if (isDashing) EndDash();
        _hurtUntil = Time.time + 0.15f;
        _rb.linearVelocity = new Vector2(direction.x * 7f, 5f);
    }

    private void FixedWallCling()
    {
        if (!_stamina.TryCling(Time.fixedDeltaTime, ClingDrainPerSecond()))
        {
            // Stamina gone: peel off the wall and drop, ignoring the wall until lockout ends.
            StopCling();
            isOnWall = false;
            wallSide = 0;
            wallClingTimer = 0;
            _clingLockTimer = clingFallLockout;
            return;
        }

        // Stick to the wall and creep down. Not climbing: horizontal velocity is killed.
        _rb.linearVelocity = new Vector2(0f, -clingSlideSpeed);

        _wallSparksTimer -= Time.fixedDeltaTime;
        if (_wallSparksTimer <= 0f)
        {
            _wallSparksTimer = 0.08f;
            Vector3 contactPos = transform.position + new Vector3(wallSide * 0.35f, -0.2f, 0f);
            JuiceManager.SpawnWallSparks(contactPos, wallSide);
        }
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            float gravMult = _moveInput.y < 0 ? 2.5f : 1f;
            _rb.linearVelocity -= new Vector2(0, gravity * gravMult * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, Mathf.Max(-maxFallSpeed, _rb.linearVelocity.y));
        }
        else if (_currentMovingPlatform != null)
        {
            float adhereY = Mathf.Min(0f, _currentMovingPlatform.CurrentVelocity.y) - 0.5f;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, adhereY);
        }
        else if (_rb.linearVelocity.y <= 0.01f)
        {
            // Ground adherence on normal ground; on one-way platforms keep Y velocity flat at 0 to avoid effector penetration
            float adhereY = (_currentGround != null && _currentGround.usedByEffector) ? 0f : -0.5f;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, adhereY);
        }
    }

    private void FixedMove()
    {
        if (_mode == null) return;
        float modSpeed = PacketIntegrityManager.Instance?.ActiveModifier?.speedMultiplier ?? 1.0f;
        float targetSpeed = _mode.GetSpeed() * (isSprinting ? sprintSpeedMultiplier : 1f) * modSpeed * _moveInput.x;
        float accel = isGrounded ? acceleration : airDecel;
        float decel = isGrounded ? groundDecel : airDecel;

        if (_moveInput.x != 0)
        {
            _rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime),
                _rb.linearVelocity.y
            );
        }
        else
        {
            _rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(_rb.linearVelocity.x, 0, decel * Time.fixedDeltaTime),
                _rb.linearVelocity.y
            );
        }

        float airControl = _mode.GetAirControl();
        float targetVelY = _rb.linearVelocity.y;
        if (_moveInput.y > 0 && !isGrounded && isOnWall)
            targetVelY = Mathf.MoveTowards(targetVelY, 0, 200f * airControl * Time.fixedDeltaTime);

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, targetVelY);

        if (wallUnstickTimer > 0)
            _rb.linearVelocity = new Vector2(_wallJumpDirection * walljumpForceX, _rb.linearVelocity.y);
    }

    private void HandleSprintInput()
    {
        isSprinting = _input != null && _input.sprintHeld && Mathf.Abs(_moveInput.x) > 0.01f && isGrounded;
        if (_input == null || !_input.sprintHeld) _sprintExhausted = false;
        if (isDashing || _sprintExhausted) isSprinting = false;
        if (isSprinting && !_stamina.TrySprint(Time.deltaTime)) { _sprintExhausted = true; isSprinting = false; }

        if (_animator == null) return;
        float mult = isSprinting ? sprintAnimMultiplier : 1f;
        _animator.UpdateState(Mathf.Abs(_rb.linearVelocity.x) > 0.1f, mult, FacingDir);
    }

    private void UpdateFacing()
    {
        if (_moveInput.x != 0)
            FacingDir = Mathf.Sign(_moveInput.x);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Vector2 pos = new Vector2(col.bounds.center.x, col.bounds.min.y) + Vector2.down * 0.05f;
        Gizmos.DrawWireCube(pos, groundCheckSize);
        Gizmos.color = Color.blue;
        pos = (Vector2)transform.position + Vector2.left * wallCheckOffset;
        Gizmos.DrawWireCube(pos, wallCheckSize);
        pos = (Vector2)transform.position + Vector2.right * wallCheckOffset;
        Gizmos.DrawWireCube(pos, wallCheckSize);
    }

    private void HandleFootsteps()
    {
        if (isGrounded && Mathf.Abs(_moveInput.x) > 0.1f && !isDashing && Time.timeScale > 0)
        {
            float stepInterval = isSprinting ? 0.22f : 0.34f;
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = stepInterval;
                AudioManager.PlayFootstep(isSprinting);
            }
        }
        else
        {
            _footstepTimer = 0.05f;
        }
    }

    private void SpawnAirJumpPulse(Vector2 pos)
    {
        var fxGo = new GameObject("AirJumpPulse");
        fxGo.transform.position = pos;
        var sr = fxGo.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("GeneratedArt/key_e_ring");
        sr.color = new Color(0.2f, 0.95f, 1f, 0.9f);
        sr.sortingOrder = 30;
        sr.transform.localScale = new Vector3(0.6f, 0.3f, 1f);
        var fx = fxGo.AddComponent<FadeAndGrowFX>();
        fx.duration = 0.22f;
        fx.targetScale = 2.2f;
    }

    private float GetUpgradeBonus(GameManager.UpgradeType type)
    {
        return GameManager.Instance?.GetUpgradeLevel(type) ?? 0;
    }

    private int GetAirJumps()
    {
        int bonus = (int)GetUpgradeBonus(GameManager.UpgradeType.AirJump);
        if (OverclockManager.HasTripleJump) bonus = Mathf.Max(bonus, 1);
        return airJumpsAvailable + bonus;
    }

    public void RefreshUpgradedStats()
    {
        if (isGrounded)
        {
            airJumpsLeft = GetAirJumps();
        }
        if (_health != null)
        {
            _health.RefreshStats();
        }
    }

    public void AddAttackImpulse(Vector2 impulse)
    {
        if (_rb != null && !isFrozen)
        {
            _rb.linearVelocity = new Vector2(impulse.x, _rb.linearVelocity.y + impulse.y);
        }
    }
}
