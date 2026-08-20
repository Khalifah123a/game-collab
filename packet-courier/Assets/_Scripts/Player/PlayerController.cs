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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();
        _mode = GetComponent<PlayerMode>();
        _health = GetComponent<PlayerHealth>();
        _animator = GetComponent<PlayerAnimator>();

        _rb.gravityScale = 0;

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
        if (isFrozen) return;
        if (_health == null || _health.isDead) return;

        CheckGrounded();
        CheckWall();
        HandleTimers();
        HandleDropInput();
        HandleJumpInput();
        HandleDashInput();
        HandleWallInput();
        UpdateFacing();
        HandleSprintInput();
    }

    private void FixedUpdate()
    {
        if (isFrozen) return;
        if (_health == null || _health.isDead) return;

        if (isDashing)
            FixedDash();
        else if (isOnWall && wallClingTimer > 0 && _moveInput.y < 0)
            FixedWallCling();
        else
            FixedMove();

        ApplyGravity();
    }

    private void CheckGrounded()
    {
        if (_dropTimer > 0)
        {
            isGrounded = false;
            _coyoteTimer -= Time.deltaTime;
            return;
        }

        Vector2 pos = new Vector2(_col.bounds.center.x, _col.bounds.min.y) + Vector2.down * 0.15f;
        Collider2D hit = Physics2D.OverlapBox(pos, groundCheckSize, 0, groundMask);
        if (hit == _col) hit = null;
        isGrounded = hit != null;

        if (isGrounded)
        {
            _coyoteTimer = coyoteTime;
            walljumpsLeft = walljumpsAvailable;
            airDashesLeft = 1;
            airJumpsLeft = GetAirJumps();
            _currentGround = hit;
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }
    }

    private void CheckWall()
    {
        Vector2 pos = (Vector2)transform.position;
        bool wallLeft = Physics2D.OverlapBox(pos + Vector2.left * wallCheckOffset, wallCheckSize, 0, groundMask);
        bool wallRight = Physics2D.OverlapBox(pos + Vector2.right * wallCheckOffset, wallCheckSize, 0, groundMask);

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

        if (_dropTimer > 0)
        {
            _dropTimer -= Time.deltaTime;
            if (_dropTimer <= 0 && _currentGround != null)
            {
                Physics2D.IgnoreCollision(_col, _currentGround, false);
                _currentGround = null;
            }
        }

        if (isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0) EndDash();
        }
    }

    private void HandleJumpInput()
    {
        if (_input == null) return;
        if (_input.jumpPressed && !isDashing)
        {
            if (isGrounded || _coyoteTimer > 0)
            {
                DoJump(Vector2.up, jumpForce);
                _coyoteTimer = 0;
            }
            else if (airJumpsLeft > 0 && !isOnWall)
            {
                DoJump(Vector2.up, jumpForce);
                airJumpsLeft--;
                _coyoteTimer = 0;
            }
            else if (isOnWall && wallClingTimer > 0 && walljumpsLeft > 0)
            {
                DoWalljump();
            }
        }
    }

    private void DoJump(Vector2 dir, float force)
    {
        if (isOnWall) isOnWall = false;
        _rb.velocity = new Vector2(_rb.velocity.x, 0);
        _rb.AddForce(dir * force, ForceMode2D.Impulse);
        _input.ConsumeJump();
    }

    private void DoWalljump()
    {
        walljumpsLeft--;
        isOnWall = false;
        wallClingTimer = 0;
        wallUnstickTimer = wallUnstickTime;

        Vector2 jumpDir = new Vector2(-wallSide * walljumpForceX, walljumpForceY);
        _rb.velocity = Vector2.zero;
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
        if (_moveInput.y < -0.5f && isGrounded && _currentGround != null && _dropTimer <= 0)
        {
            _dropHoldTimer += Time.deltaTime;
            if (_dropHoldTimer >= 0.15f)
            {
                Physics2D.IgnoreCollision(_col, _currentGround, true);
                isGrounded = false;
                _dropTimer = 0.3f;
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
        if (_input.dashPressed && dashCooldownTimer <= 0 && airDashesLeft > 0 && !isDashing)
            StartDash();
    }

    private void StartDash()
    {
        isDashing = true;
        _dashTimer = dashDuration;
        dashCooldownTimer = _mode.GetDashCooldown();
        _dashDir = _input.dashDirection;

        if (_dashDir.y == 0)
            _rb.gravityScale = dashGravityScale;
        else
            _rb.gravityScale = 0;

        _rb.velocity = _dashDir * dashSpeed;
        _input.ConsumeDash();

        if (!isGrounded)
            airDashesLeft--;

        _col.enabled = false;
    }

    private void EndDash()
    {
        isDashing = false;
        _rb.gravityScale = 0;
        _rb.velocity *= 0.5f;
        _col.enabled = true;
    }

    private void FixedDash()
    {
        _rb.velocity = _dashDir * dashSpeed;
    }

    private void FixedWallCling()
    {
        _rb.velocity = new Vector2(0, -1f);
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            float gravMult = _moveInput.y < 0 ? 2.5f : 1f;
            _rb.velocity -= new Vector2(0, gravity * gravMult * Time.fixedDeltaTime);
            _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Max(-maxFallSpeed, _rb.velocity.y));
        }
    }

    private void FixedMove()
    {
        if (_mode == null) return;
        float targetSpeed = _mode.GetSpeed() * (isSprinting ? sprintSpeedMultiplier : 1f) * _moveInput.x;
        float accel = isGrounded ? acceleration : airDecel;
        float decel = isGrounded ? groundDecel : airDecel;

        if (_moveInput.x != 0)
        {
            _rb.velocity = new Vector2(
                Mathf.MoveTowards(_rb.velocity.x, targetSpeed, accel * Time.fixedDeltaTime),
                _rb.velocity.y
            );
        }
        else
        {
            _rb.velocity = new Vector2(
                Mathf.MoveTowards(_rb.velocity.x, 0, decel * Time.fixedDeltaTime),
                _rb.velocity.y
            );
        }

        float airControl = _mode.GetAirControl();
        float targetVelY = _rb.velocity.y;
        if (_moveInput.y > 0 && !isGrounded && isOnWall)
            targetVelY = Mathf.MoveTowards(targetVelY, 0, 200f * airControl * Time.fixedDeltaTime);

        _rb.velocity = new Vector2(_rb.velocity.x, targetVelY);

        if (wallUnstickTimer > 0)
            _rb.velocity = new Vector2(FacingDir * 30f, _rb.velocity.y);
    }

    private void HandleSprintInput()
    {
        isSprinting = _input != null && _input.sprintHeld && Mathf.Abs(_moveInput.x) > 0.01f && isGrounded;

        if (_animator == null) return;
        float mult = isSprinting ? sprintAnimMultiplier : 1f;
        _animator.UpdateState(Mathf.Abs(_rb.velocity.x) > 0.1f, mult, FacingDir);
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

    private float GetUpgradeBonus(GameManager.UpgradeType type)
    {
        return GameManager.Instance?.GetUpgradeLevel(type) ?? 0;
    }

    private int GetAirJumps()
    {
        return airJumpsAvailable + (int)GetUpgradeBonus(GameManager.UpgradeType.AirJump);
    }
}
