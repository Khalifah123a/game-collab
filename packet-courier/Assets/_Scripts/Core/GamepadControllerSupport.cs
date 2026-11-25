using System.Collections;
using UnityEngine;

/// <summary>
/// Full Gamepad / Controller support (Xbox, PlayStation, Generic XInput).
/// Translates joystick axes and hardware buttons into InputManager commands,
/// and provides controller vibration rumble feedback on impacts.
/// </summary>
public class GamepadControllerSupport : MonoBehaviour
{
    public static GamepadControllerSupport Instance { get; private set; }

    [Header("Controller Deadzone")]
    public float joystickDeadzone = 0.18f;

    [Header("Vibration Settings")]
    public static bool EnableVibration = true;

    public static GamepadControllerSupport Ensure()
    {
        if (Instance != null) return Instance;
        var im = InputManager.EnsureExists();
        var support = im.GetComponent<GamepadControllerSupport>();
        if (support == null) support = im.gameObject.AddComponent<GamepadControllerSupport>();
        Instance = support;
        return support;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        var input = InputManager.Instance;
        if (input == null || input.useTouchInput) return;

        // Joystick Axis: Left Stick / D-Pad
        float horiz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horiz) > joystickDeadzone || Mathf.Abs(vert) > joystickDeadzone)
        {
            input.moveInput = new Vector2(
                Mathf.Abs(horiz) > joystickDeadzone ? Mathf.Sign(horiz) : 0f,
                Mathf.Abs(vert) > joystickDeadzone ? Mathf.Sign(vert) : 0f
            );
        }

        // Button A / Cross (JoystickButton0) -> Jump
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            input.jumpPressed = true;
            input.jumpHeld = true;
        }
        if (Input.GetKey(KeyCode.JoystickButton0))
        {
            input.jumpHeld = true;
        }

        // Button B / Circle (JoystickButton1) -> Dash
        if (Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            input.dashPressed = true;
        }

        // Button X / Square (JoystickButton2) -> Melee Attack / Blaster
        if (Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            input.attackPressed = true;
        }

        // Button Y / Triangle (JoystickButton3) -> Mode Toggle (Data / Encrypted)
        if (Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            input.modeTogglePressed = true;
        }

        // Bumpers (LB: Button4, RB: Button5) -> Sprint / PowerUp
        if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton5))
        {
            input.sprintHeld = true;
        }

        // Start / Options (JoystickButton7) -> Pause
        if (Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            input.pausePressed = true;
        }

        // Interact prompt on Controller: Button X or Bumper
        if (Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton5))
        {
            input.interactPressed = true;
        }
    }

    public static void TriggerRumble(float duration = 0.15f)
    {
        var support = Ensure();
        if (support != null && EnableVibration)
        {
            support.StartCoroutine(support.RumbleRoutine(duration));
        }
    }

    private IEnumerator RumbleRoutine(float duration)
    {
        // Safe cross-platform vibration check
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        yield return new WaitForSecondsRealtime(duration);
    }
}
