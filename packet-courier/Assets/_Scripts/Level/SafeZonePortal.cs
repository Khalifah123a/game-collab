using UnityEngine;

/// <summary>
/// Holographic cyber data portal placed in Safe Zones / Server B exits.
/// Provides a visual gateway to the next stage or world.
/// Displays lock/firewall status while quests are incomplete, and activates
/// with swirling warp vortex and [E] prompt once unlocked.
/// </summary>
public class SafeZonePortal : MonoBehaviour
{
    public ServerB_Goal goal;
    public LevelManager levelManager;

    private SpriteRenderer _portalRenderer;
    private Transform _ringTransform;
    private TextMesh _statusText;
    private InteractKeyPrompt _prompt;
    private bool _isUnlocked;
    private float _pulseTimer;

    private void Start()
    {
        if (goal == null) goal = GetComponentInParent<ServerB_Goal>();
        if (levelManager == null && goal != null) levelManager = goal.levelManager;
        if (levelManager == null) levelManager = FindAnyObjectByType<LevelManager>();

        BuildVisuals();
    }

    private void BuildVisuals()
    {
        // 1. Base Portal Arch & Vortex
        var portalGo = new GameObject("PortalArch", typeof(SpriteRenderer));
        portalGo.transform.SetParent(transform, false);
        portalGo.transform.localPosition = Vector3.zero;
        _portalRenderer = portalGo.GetComponent<SpriteRenderer>();
        _portalRenderer.sprite = Resources.Load<Sprite>("GeneratedArt/portal_warp");
        _portalRenderer.sortingOrder = 5;

        // 2. Swirling Energy Ring
        var ringGo = new GameObject("PortalRing", typeof(SpriteRenderer));
        ringGo.transform.SetParent(transform, false);
        ringGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        _ringTransform = ringGo.transform;
        var ringSr = ringGo.GetComponent<SpriteRenderer>();
        ringSr.sprite = Resources.Load<Sprite>("GeneratedArt/portal_ring");
        ringSr.sortingOrder = 6;
        ringGo.transform.localScale = Vector3.one * 1.25f;

        // 3. Status Hologram Text
        var textGo = new GameObject("PortalStatus", typeof(TextMesh));
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        _statusText = textGo.GetComponent<TextMesh>();
        _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _statusText.fontSize = 32;
        _statusText.characterSize = 0.045f;
        _statusText.anchor = TextAnchor.MiddleCenter;
        _statusText.alignment = TextAlignment.Center;

        var mr = textGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 15;

        // 4. [E] Interact Prompt
        _prompt = InteractKeyPrompt.Attach(gameObject, "WARP", new Vector2(0f, 1.15f));
        if (_prompt != null) _prompt.Hide();
    }

    private void Update()
    {
        if (goal == null && levelManager == null) return;

        bool locked = (goal != null && goal.GateLocked) || (levelManager != null && levelManager.IsGoalLocked);
        _isUnlocked = !locked;

        _pulseTimer += Time.deltaTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * 3.5f);

        // Spin energy ring
        if (_ringTransform != null)
        {
            float spinSpeed = _isUnlocked ? 85f : 18f;
            _ringTransform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
            float scale = _isUnlocked ? 1.2f + pulse * 0.15f : 1.0f;
            _ringTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // Color & Status Text
        if (_isUnlocked)
        {
            Color onlineCol = new Color(0.1f, 0.95f, 1f, 0.95f);
            if (_portalRenderer != null) _portalRenderer.color = onlineCol;
            if (_statusText != null)
            {
                _statusText.text = "< DATA GATEWAY ONLINE >\n[E] WARP TO NEXT SERVER";
                _statusText.color = new Color(0.3f, 1f, 0.85f, 0.8f + pulse * 0.2f);
            }
        }
        else
        {
            Color lockedCol = new Color(1f, 0.2f, 0.35f, 0.85f);
            if (_portalRenderer != null) _portalRenderer.color = lockedCol;
            if (_statusText != null)
            {
                _statusText.text = "< GATEWAY LOCKED >\nFIREWALL ACTIVE // DELIVER PACKET";
                _statusText.color = new Color(1f, 0.3f, 0.4f, 0.75f + pulse * 0.25f);
            }
        }

        // Player proximity check for [E] prompt
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null && !player.isFrozen)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            bool inRange = dist < 2.5f;

            if (_prompt != null)
            {
                if (inRange && _isUnlocked) _prompt.Show();
                else _prompt.Hide();
            }

            // Interact with [E]
            if (inRange && Input.GetKeyDown(KeyCode.E))
            {
                if (_isUnlocked)
                {
                    TriggerWarp();
                }
                else if (levelManager != null)
                {
                    levelManager.ShowGateHint();
                    AudioManager.PlayInteract();
                }
            }
        }
    }

    public void TriggerWarp()
    {
        AudioManager.DuckBGMForWarp(0.12f, 0.45f);
        AudioManager.Play("sfx_ui_open", 1.0f, 1.2f);
        AudioManager.PlayInteract();
        JuiceManager.Shake(0.25f);
        JuiceManager.Hitstop(0.04f);

        // Spawn warp flash
        if (JuiceManager.Instance != null)
        {
            JuiceManager.SpawnImpactSparks(transform.position, new Color(0.2f, 1f, 0.9f, 1f));
        }

        if (levelManager != null)
        {
            levelManager.CompleteSegment();
        }
        else if (goal != null && goal.levelManager != null)
        {
            goal.levelManager.CompleteSegment();
        }
    }
}
