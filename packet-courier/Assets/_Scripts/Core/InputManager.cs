using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public static InputManager EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("InputManager");
        return go.AddComponent<InputManager>();
    }

    [Header("Axis")]
    public Vector2 moveInput;
    public Vector2 dashDirection;

    [Header("Buttons")]
    public bool jumpPressed;
    public bool jumpHeld;
    public bool dashPressed;
    public bool modeTogglePressed;
    public bool portForwardPressed;
    public bool portBackPressed;
    public bool interactPressed;
    public bool pausePressed;
    public bool powerUpPressed;

    public bool sprintHeld;

    [Header("Touch")]
    public bool useTouchInput;
    public Vector2 touchMoveDir;
    public bool touchJump;
    public bool touchDash;
    public bool touchModeToggle;

    private float _jumpBufferTimer;
    private const float JumpBufferTime = 0.1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        useTouchInput = true;
#else
        useTouchInput = false;
#endif

        if (!useTouchInput)
            HandleKeyboardInput();

        HandleJumpBuffer();
        HandleDashDirection();

        ClearPressedFlags();
    }

    private void HandleKeyboardInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
            jumpHeld = true;
            _jumpBufferTimer = JumpBufferTime;
        }
        jumpHeld = Input.GetButton("Jump");

        if (Input.GetButtonDown("Fire1"))
            dashPressed = true;

        if (Input.GetButtonDown("Fire2"))
            modeTogglePressed = true;

        if (Input.GetKeyDown(KeyCode.Q))
            portBackPressed = true;
        if (Input.GetKeyDown(KeyCode.E))
            portForwardPressed = true;

        if (Input.GetButtonDown("Submit"))
            interactPressed = true;

        if (Input.GetButtonDown("Cancel"))
            pausePressed = true;

        sprintHeld = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetKeyDown(KeyCode.LeftShift))
                powerUpPressed = true;
    }

    public void SetTouchInput(Vector2 move, bool jump, bool dash, bool modeToggle)
    {
        touchMoveDir = move;
        touchJump = jump;
        touchDash = dash;
        touchModeToggle = modeToggle;

        if (touchJump && _jumpBufferTimer <= 0)
        {
            jumpPressed = true;
            _jumpBufferTimer = JumpBufferTime;
        }

        if (touchDash)
            dashPressed = true;
        if (touchModeToggle)
            modeTogglePressed = true;
    }

    private void HandleJumpBuffer()
    {
        if (_jumpBufferTimer > 0)
        {
            jumpPressed = true;
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void HandleDashDirection()
    {
        Vector2 dir = useTouchInput ? touchMoveDir : moveInput;

        if (dashPressed)
        {
            if (dir.sqrMagnitude > 0.1f)
                dashDirection = dir.normalized;
            else
            {
                float facing = FindObjectOfType<PlayerController>()?.FacingDir ?? 1;
                dashDirection = new Vector2(facing, 0);
            }
        }
    }

    private void ClearPressedFlags()
    {
        if (_jumpBufferTimer <= 0)
            jumpPressed = false;
    }

    public void ConsumeJump() => _jumpBufferTimer = 0;
    public void ConsumeDash() => dashPressed = false;
    public void ConsumeModeToggle() => modeTogglePressed = false;
    public void ConsumeInteract() => interactPressed = false;
    public void ConsumePortForward() => portForwardPressed = false;
    public void ConsumePortBack() => portBackPressed = false;
    public void ConsumePowerUp() => powerUpPressed = false;
    public void ConsumePause() => pausePressed = false;
}
