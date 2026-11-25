using UnityEngine;

[DefaultExecutionOrder(-100)]
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
    public bool attackPressed;
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

        GamepadControllerSupport.Ensure();
        TouchControlsUI.Ensure();
    }

    private void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        useTouchInput = true;
#else
        useTouchInput = TouchControlsUI.IsTouchEnabled;
#endif

        if (!useTouchInput)
            HandleKeyboardInput();

        HandleJumpBuffer();
        HandleDashDirection();
    }

    private void LateUpdate()
    {
        ClearPressedFlags();
    }

    private void HandleKeyboardInput()
    {
        float h = 0f;
        try { h = Input.GetAxisRaw("Horizontal"); } catch {}
        if (Mathf.Abs(h) < 0.01f)
        {
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        }

        float v = 0f;
        try { v = Input.GetAxisRaw("Vertical"); } catch {}
        if (Mathf.Abs(v) < 0.01f)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        }

        moveInput = new Vector2(h, v);

        bool jumpDown = false;
        try { jumpDown = Input.GetButtonDown("Jump"); } catch {}
        if (!jumpDown)
            jumpDown = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.JoystickButton0);

        if (jumpDown)
        {
            jumpPressed = true;
            jumpHeld = true;
            _jumpBufferTimer = JumpBufferTime;
        }

        bool jumpHold = false;
        try { jumpHold = Input.GetButton("Jump"); } catch {}
        if (!jumpHold)
            jumpHold = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.JoystickButton0);
        jumpHeld = jumpHold;

        attackPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton2);
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.JoystickButton1))
            dashPressed = true;

                if (Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.JoystickButton3) ||
            Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftAlt))
            modeTogglePressed = true;

        if (Input.GetKeyDown(KeyCode.Q) || Input.mouseScrollDelta.y < -0.1f)
            portBackPressed = true;
        if (Input.GetKeyDown(KeyCode.E) || Input.mouseScrollDelta.y > 0.1f)
            portForwardPressed = true;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton2))
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
                float facing = FindAnyObjectByType<PlayerController>()?.FacingDir ?? 1;
                dashDirection = new Vector2(facing, 0);
            }
        }
    }

    private void ClearPressedFlags()
    {
        if (_jumpBufferTimer <= 0)
            jumpPressed = false;

        interactPressed = false;
        dashPressed = false;
        attackPressed = false;
        modeTogglePressed = false;
        portBackPressed = false;
        portForwardPressed = false;
        pausePressed = false;
        powerUpPressed = false;
    }

    public void ConsumeJump() { _jumpBufferTimer = 0; jumpPressed = false; }
    public void ConsumeDash() => dashPressed = false;
    public void ConsumeAttack() => attackPressed = false;
    public void ConsumeModeToggle() => modeTogglePressed = false;
    public void ConsumeInteract() => interactPressed = false;
    public void ConsumePortForward() => portForwardPressed = false;
    public void ConsumePortBack() => portBackPressed = false;
    public void ConsumePowerUp() => powerUpPressed = false;
    public void ConsumePause() => pausePressed = false;
}
