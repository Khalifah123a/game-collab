using UnityEngine;

/// <summary>
/// Safe Zone interactive terminal station for courier hardware overclocking.
/// Displays an animated keyboard keycap [E] prompt and opens OverclockUI.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class OverclockTerminal : MonoBehaviour
{
    public float triggerRadius = 1.9f;
    public string promptText = "OVERCLOCK";

    private bool _playerInRange;
    private SpriteRenderer _terminalRenderer;
    private InteractKeyPrompt _keyPrompt;

    public static OverclockTerminal Spawn(Vector2 position, Transform parent = null)
    {
        var go = new GameObject("OverclockTerminal", typeof(CircleCollider2D), typeof(OverclockTerminal));
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.GetComponent<OverclockTerminal>();
    }

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = triggerRadius;

        BuildVisuals();
        SnapToGround();
    }

    private void Start()
    {
        SnapToGround();
    }

    private void BuildVisuals()
    {
        // Terminal Sprite: 32x48 px, 16 PPU = 2.0w x 3.0h (center pivot).
        // Scale to 0.58 so height is ~1.74m.
        // Base flush at y = 0: localPosition = (0, 1.5 * 0.58, 0) = (0, 0.87, 0)
        float s = 0.58f;
        var spriteGo = new GameObject("TerminalSprite", typeof(SpriteRenderer));
        spriteGo.transform.SetParent(transform, false);
        spriteGo.transform.localScale = Vector3.one * s;
        spriteGo.transform.localPosition = new Vector3(0f, 1.5f * s, 0f);
        _terminalRenderer = spriteGo.GetComponent<SpriteRenderer>();
        _terminalRenderer.sprite = Resources.Load<Sprite>("GeneratedArt/overclock_terminal");
        _terminalRenderer.sortingOrder = 9;

        // Animated [E] Prompt centered right above terminal
        var promptGo = new GameObject("PromptAnchor");
        promptGo.transform.SetParent(transform, false);
        promptGo.transform.localPosition = new Vector3(0f, 3.0f * s + 0.35f, 0f);
        _keyPrompt = promptGo.AddComponent<InteractKeyPrompt>();
        _keyPrompt.SetLabel(promptText);
        promptGo.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            _playerInRange = true;
            if (_keyPrompt != null) _keyPrompt.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            _playerInRange = false;
            if (_keyPrompt != null) _keyPrompt.gameObject.SetActive(false);
            if (OverclockUI.Instance != null) OverclockUI.Instance.CloseIfOwner(this);
        }
    }

    private void Update()
    {
        // Ambient terminal glow pulse
        if (_terminalRenderer != null)
        {
            float pulse = 0.9f + 0.1f * Mathf.Sin(Time.time * 3f);
            _terminalRenderer.color = new Color(pulse, 1f, 1f, 1f);
        }

        if (!_playerInRange || OverclockUI.Instance != null && OverclockUI.Instance.IsOpen) return;

        var input = InputManager.Instance;
        if (input != null && input.interactPressed)
        {
            input.ConsumeInteract();
            input.ConsumeAttack();
            if (_keyPrompt != null) _keyPrompt.TriggerClick(true);
            OverclockUI.Ensure().Open(this);
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        if (other == null) return false;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        try { return other.CompareTag("Player"); }
        catch { return false; }
    }

    public void SnapToGround()
    {
        var lm = FindAnyObjectByType<LevelManager>();
        if (lm != null && lm.worldIndex == 0)
        {
            transform.position = new Vector3(transform.position.x, -17.0f, transform.position.z);
            Physics2D.SyncTransforms();
            return;
        }
        Physics2D.SyncTransforms();
        Vector2 origin = (Vector2)transform.position + Vector2.up * 2.5f;
        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 12f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.normal.y < 0.5f) continue;
            if (hit.collider.GetComponentInParent<NPC_Base>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;

            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            return;
        }
    }
}
