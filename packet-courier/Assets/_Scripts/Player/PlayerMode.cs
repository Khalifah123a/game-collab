using UnityEngine;

public class PlayerMode : MonoBehaviour
{
    public static PlayerMode Instance { get; private set; }

    [Header("Current Mode")]
    public bool isEncrypted;
    public int currentPort = 80;

    [Header("Port Options")]
    public int[] availablePorts = { 21, 22, 80, 443, 8080 };
    public string[] portNames = { "FTP", "SSH", "HTTP", "HTTPS", "Alt HTTP" };

    [Header("Mode Switch")]
    public float switchCooldown = 0.15f;
    public float lastSwitchTime;

    [Header("Mode Stats")]
    public float plaintextSpeed = 10f;
    public float encryptedSpeed = 6f;
    public float plaintextDashDist = 10f;
    public float encryptedDashDist = 7f;
    public float plaintextDashCooldown = 0.3f;
    public float encryptedDashCooldown = 0.5f;
    public float plaintextAirControl = 0.8f;
    public float encryptedAirControl = 0.4f;
    public Vector2 plaintextHitbox = new Vector2(0.5f, 1f);
    public Vector2 encryptedHitbox = new Vector2(0.75f, 1.25f);

    private BoxCollider2D _collider;
    private InputManager _input;

    public delegate void ModeChanged(bool encrypted);
    public event ModeChanged OnModeChanged;
    public delegate void PortChanged(int port);
    public event PortChanged OnPortChanged;

    private void Awake()
    {
        Instance = this;
        _collider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        _input = InputManager.Instance;
        SetMode(false);
    }

    private void Update()
    {
        if (_input == null)
        {
            _input = InputManager.Instance;
            if (_input == null) return;
        }

        if (_input.modeTogglePressed &&
            Time.time - lastSwitchTime >= switchCooldown)
        {
            ToggleMode();
            _input.ConsumeModeToggle();
        }

        if (_input.portForwardPressed)
        {
            CyclePort(1);
            _input.ConsumePortForward();
        }
        if (_input.portBackPressed)
        {
            CyclePort(-1);
            _input.ConsumePortBack();
        }
    }

    public void ToggleMode()
    {
        SetMode(!isEncrypted);
    }

    public void SetMode(bool encrypted)
    {
        isEncrypted = encrypted;
        lastSwitchTime = Time.time;

        if (_collider)
        {
            Vector2 size = isEncrypted ? encryptedHitbox : plaintextHitbox;
            _collider.size = size;
        }

        OnModeChanged?.Invoke(isEncrypted);
    }

    public void CyclePort(int direction)
    {
        int idx = System.Array.IndexOf(availablePorts, currentPort);
        idx = (idx + direction + availablePorts.Length) % availablePorts.Length;
        currentPort = availablePorts[idx];
        OnPortChanged?.Invoke(currentPort);
    }

    public void SetPort(int port)
    {
        currentPort = port;
        OnPortChanged?.Invoke(currentPort);
    }

    public float GetSpeed()
    {
        return isEncrypted ? encryptedSpeed : plaintextSpeed;
    }

    public float GetDashDistance()
    {
        return isEncrypted ? encryptedDashDist : plaintextDashDist;
    }

    public float GetDashCooldown()
    {
        float reduc = GetUpgradeBonus(GameManager.UpgradeType.DashCooldown) * 0.05f;
        float baseCD = isEncrypted ? encryptedDashCooldown : plaintextDashCooldown;
        return Mathf.Max(0.1f, baseCD - reduc);
    }

    public float GetAirControl()
    {
        float bonus = GetUpgradeBonus(GameManager.UpgradeType.AirControl) * 0.1f;
        float baseAC = isEncrypted ? encryptedAirControl : plaintextAirControl;
        return Mathf.Min(1f, baseAC + bonus);
    }

    private float GetUpgradeBonus(GameManager.UpgradeType type)
    {
        return GameManager.Instance?.GetUpgradeLevel(type) ?? 0;
    }
}
