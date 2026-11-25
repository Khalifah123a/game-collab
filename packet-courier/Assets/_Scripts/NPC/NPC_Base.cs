using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>One selectable reply in an NPC conversation. Sibling systems hook their own logic to <see cref="onChoose"/>.</summary>
[Serializable]
public class NPCDialogChoice
{
    public string label = "Continue";
    public UnityEvent onChoose = new UnityEvent();
}

/// <summary>
/// Base NPC: placeholder body, trigger proximity prompt, and the runtime dialog panel,
/// merged with the quest interaction contract so there is exactly one NPC_Base type.
///
/// Plain NPCs: subclass and override <see cref="OnInteract"/> (or call <see cref="OpenDialog"/>).
/// Quest NPCs: override <see cref="GetChoices"/> and <see cref="OnChoiceChosen"/>; the
/// default <see cref="Interact"/> routes to quest choices when present and falls back
/// to <see cref="OnInteract"/> otherwise, so PackageGiverNPC/PackageReceiverNPC keep working.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class NPC_Base : MonoBehaviour
{
    [Header("Identity")]
    public string npcId = "npc";
    public string displayName = "NPC";
    public string speakerName = "OPERATOR";
    public int worldIndex = -1;

    [Header("Dialog")]
    public string[] lines = { "..." };
    public List<NPCDialogChoice> choices = new List<NPCDialogChoice>();

    [Header("Prompt")]
    public string interactPrompt = "[ENTER] TALK";
    [Tooltip("Text shown on the interaction prompt (quest contract).")]
    public string prompt = "Talk";
    public float promptHeight = 1.75f;

    [Header("Placeholder Body")]
    public Color bodyColor = new Color(0.45f, 0.6f, 0.9f);
    public Color visorColor = new Color(0.3f, 0.95f, 1f);
    public Vector2 bodySize = new Vector2(0.7f, 1.3f);
    [Header("Custom Visual Asset")]
    public Sprite customSprite;

    private int _overlaps;

    public bool playerInRange => _overlaps > 0;
    public bool PlayerInRange => _overlaps > 0;
    public bool promptVisible => _prompt != null && _prompt.gameObject.activeSelf;
    public bool IsTalking => DialogUI.IsOpen && DialogUI.Instance != null && ReferenceEquals(DialogUI.Instance.Owner, this);

    private TextMesh _prompt;
    private InteractKeyPrompt _keyPrompt;
    private SpriteRenderer _body;
    private SpriteRenderer _head;
    private SpriteRenderer _visor;
    private SpriteRenderer _core;
    private SpriteRenderer _thruster;
    private SpriteRenderer _shadow;
    private Transform _visualRoot;
    private Transform _speechHolo;
    private SpriteRenderer[] _holoBars;

    private Vector3 _initialHeadPos;
    private Vector3 _initialVisualPos;

    protected virtual void Awake()
    {
        ConfigureTriggers();
        BuildPlaceholder();
        SnapToGround();
    }

    protected virtual void Start()
    {
        SnapToGround();
    }

    private void OnDestroy()
    {
        if (DialogUI.Instance != null) DialogUI.Instance.CloseIfOwner(this);
    }

    /// <summary>Opens the configured conversation. Override for custom dialogue behaviour.</summary>
    public virtual void OnInteract()
    {
        OpenDialog(lines, choices);
    }

    /// <summary>Pushes a conversation into the shared dialog panel; sibling logic stays in the choice events.</summary>
    public void OpenDialog(string[] dialogLines, List<NPCDialogChoice> dialogChoices)
    {
        if (DialogUI.IsOpen) return;
        int count = dialogChoices != null ? dialogChoices.Count : 0;
        var labels = new string[count];
        for (int i = 0; i < count; i++) labels[i] = dialogChoices[i] != null ? dialogChoices[i].label : string.Empty;
        var captured = dialogChoices;
        DialogUI.Ensure().Show(speakerName, dialogLines, labels, index =>
        {
            if (captured == null || index < 0 || index >= captured.Count) return;
            captured[index].onChoose?.Invoke();
        }, this);
    }

    /// <summary>Label the HUD shows for this NPC.</summary>
    public virtual string GetPrompt()
    {
        return PlayerInRange ? prompt : displayName;
    }

    public virtual bool CanInteract()
    {
        return PlayerInRange && isActiveAndEnabled;
    }

    /// <summary>Dialog options offered right now. An empty list means "use the plain lines/choices path".</summary>
    public virtual IReadOnlyList<string> GetChoices()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    /// Hands the options to the dialog layer. Override to show real choices and call
    /// <paramref name="onChosen"/> with the picked index; the default renders through DialogUI.
    /// </summary>
    public virtual void PresentChoices(IReadOnlyList<string> questChoices, Action<int> onChosen)
    {
        if (onChosen == null) return;
        int count = questChoices != null ? questChoices.Count : 0;
        if (count <= 0)
        {
            onChosen(-1);
            return;
        }
        if (DialogUI.IsOpen) return;
        var labels = new string[count];
        for (int i = 0; i < count; i++) labels[i] = questChoices[i] ?? string.Empty;
        string speaker = string.IsNullOrEmpty(displayName) ? speakerName : displayName;
        DialogUI.Ensure().Show(speaker, lines, labels, onChosen, this);
    }

    public virtual void Interact()
    {
        if (!CanInteract()) return;
        var questChoices = GetChoices();
        if (questChoices != null && questChoices.Count > 0)
            PresentChoices(questChoices, OnChoiceChosen);
        else
            OnInteract();
    }

    /// <summary>
    /// Runs the dialog without the proximity gate. Used by automated play-mode smoke runs,
    /// where the test drives the NPC directly instead of walking the player into range.
    /// </summary>
    public void InteractFromRange()
    {
        PresentChoices(GetChoices(), OnChoiceChosen);
    }

    /// <summary><paramref name="index"/> is -1 when the dialog was dismissed.</summary>
    protected virtual void OnChoiceChosen(int index)
    {
    }

    protected virtual void Update()
    {
        if (promptVisible != (playerInRange && !DialogUI.IsOpen)) UpdatePrompt();
        AnimateVisuals();

        var input = InputManager.Instance;
        if (input == null || !input.interactPressed) return;
        if (!playerInRange || DialogUI.IsOpen) return;
        if (_keyPrompt != null) _keyPrompt.TriggerClick(true);
        input.ConsumeInteract();
        input.ConsumeAttack();
        Interact();
    }

    private void AnimateVisuals()
    {
        if (_visualRoot == null) return;

        bool talking = IsTalking;
        float time = Time.time;

        // Facing direction: smoothly face player when in proximity
        if (playerInRange)
        {
            var player = PlayerHealth.Instance;
            if (player != null)
            {
                float dx = player.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.15f)
                {
                    float targetScaleX = dx > 0 ? 1f : -1f;
                    Vector3 s = _visualRoot.localScale;
                    s.x = targetScaleX;
                    _visualRoot.localScale = s;
                }
            }
        }

        if (talking)
        {
            // Talking animation: lively head nodding, visor rhythm pulsation, and speech waves
            float headNod = Mathf.Sin(time * 12f) * 4f;
            float talkBounce = Mathf.Abs(Mathf.Sin(time * 10f)) * 0.04f;
            if (_head != null)
            {
                _head.transform.localPosition = _initialHeadPos + new Vector3(0f, talkBounce, 0f);
                _head.transform.localRotation = Quaternion.Euler(0f, 0f, headNod);
            }

            // Pulsing visor & reactor core with speech cadence
            float visorPulse = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(time * 14f));
            if (_visor != null) _visor.color = new Color(visorColor.r, visorColor.g, visorColor.b, visorPulse);
            if (_core != null) _core.color = new Color(visorColor.r * 1.2f, visorColor.g * 1.2f, visorColor.b * 1.2f, visorPulse);

            // Speech Hologram Equalizer bars active and dancing
            if (_speechHolo != null)
            {
                _speechHolo.gameObject.SetActive(true);
                if (_holoBars != null && _holoBars.Length == 3)
                {
                    float h0 = 0.3f + 0.7f * Mathf.PingPong(time * 9f, 1f);
                    float h1 = 0.2f + 0.8f * Mathf.PingPong(time * 13f + 0.3f, 1f);
                    float h2 = 0.35f + 0.65f * Mathf.PingPong(time * 11f + 0.7f, 1f);
                    _holoBars[0].transform.localScale = new Vector3(0.08f, h0 * 0.35f, 1f);
                    _holoBars[1].transform.localScale = new Vector3(0.08f, h1 * 0.35f, 1f);
                    _holoBars[2].transform.localScale = new Vector3(0.08f, h2 * 0.35f, 1f);
                }
            }

            // Thruster active
            if (_thruster != null)
            {
                float tScale = 0.8f + 0.2f * Mathf.Sin(time * 20f);
                _thruster.transform.localScale = new Vector3(bodySize.x * 0.7f, 0.25f * tScale, 1f);
            }
        }
        else
        {
            // Idle ambient hover bobbing (drones only; humanoid/shop custom sprites stay grounded)
            if (customSprite == null)
            {
                float hoverBob = Mathf.Sin(time * 2.8f) * 0.045f;
                _visualRoot.localPosition = _initialVisualPos + new Vector3(0f, hoverBob, 0f);
            }
            else
            {
                _visualRoot.localPosition = _initialVisualPos;
            }

            // Gentle head sway
            if (_head != null)
            {
                _head.transform.localPosition = _initialHeadPos;
                _head.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.8f) * 1.5f);
            }

            // Visor subtle breath glow
            float visorBreath = 0.75f + 0.25f * Mathf.Sin(time * 2f);
            if (_visor != null) _visor.color = new Color(visorColor.r, visorColor.g, visorColor.b, visorBreath);
            if (_core != null) _core.color = new Color(visorColor.r, visorColor.g, visorColor.b, visorBreath * 0.9f);

            // Speech Hologram inactive when not talking
            if (_speechHolo != null) _speechHolo.gameObject.SetActive(false);

            // Thruster idle hum
            if (_thruster != null)
            {
                float tScale = 0.9f + 0.1f * Mathf.Sin(time * 10f);
                _thruster.transform.localScale = new Vector3(bodySize.x * 0.6f, 0.2f * tScale, 1f);
            }
        }

        bool showPrompt = PlayerInRange && !DialogUI.IsOpen && !ShopUI.IsShopOpen && !OverclockUI.IsOverclockOpen;
        if (_keyPrompt != null)
        {
            if (showPrompt && !_keyPrompt.gameObject.activeSelf) _keyPrompt.Show();
            else if (!showPrompt && _keyPrompt.gameObject.activeSelf) _keyPrompt.Hide();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _overlaps++;
        UpdatePrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _overlaps = Math.Max(0, _overlaps - 1);
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        bool showPrompt = PlayerInRange && !DialogUI.IsOpen && !ShopUI.IsShopOpen && !OverclockUI.IsOverclockOpen;
        if (_prompt != null) _prompt.gameObject.SetActive(showPrompt);
        if (_keyPrompt != null)
        {
            if (showPrompt) _keyPrompt.Show();
            else _keyPrompt.Hide();
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        if (other == null) return false;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        try { return other.CompareTag("Player"); }
        catch { return false; }
    }

    /// <summary>
    /// Drops the NPC root onto solid ground below so quest/shop endpoints never
    /// hover, wherever the spawner placed them. No ground in reach: unchanged.
    /// </summary>
    public virtual void SnapToGround()
    {
        if (worldIndex == 0)
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
            if (hit.normal.y < 0.5f) continue;
            if (hit.collider.GetComponentInParent<NPC_Base>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.collider.GetComponentInParent(typeof(IEnemyDamageable)) as IEnemyDamageable != null) continue;
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            return;
        }
        Debug.LogWarning($"[NPC_Base] No ground below '{name}' at {transform.position}. NPC left in place.", this);
    }

    private void ConfigureTriggers()
    {
        foreach (var col in GetComponents<Collider2D>())
        {
            col.isTrigger = true;
            if (col is CircleCollider2D circle) circle.radius = Mathf.Max(bodySize.x, bodySize.y) * 0.5f + 0.1f;
            else if (col is CapsuleCollider2D capsule) capsule.size = bodySize + new Vector2(0.2f, 0.2f);
            else if (col is BoxCollider2D box) box.size = bodySize + new Vector2(0.2f, 0.2f);
        }
    }

    private void BuildPlaceholder()
    {
        // 1. Visual container root for animations (hover bobbing, flips)
        if (_visualRoot == null)
        {
            var vRoot = new GameObject("VisualRoot");
            vRoot.transform.SetParent(transform, false);
            _visualRoot = vRoot.transform;
            _initialVisualPos = Vector3.zero;
        }

        // 2. Drop shadow under hover base
        if (_shadow == null)
        {
            var shadowGo = new GameObject("Shadow", typeof(SpriteRenderer));
            shadowGo.transform.SetParent(transform, false);
            _shadow = shadowGo.GetComponent<SpriteRenderer>();
            _shadow.sprite = ShadowSprite();
            _shadow.color = new Color(0f, 0f, 0f, 0.45f);
            _shadow.sortingOrder = 4;
            _shadow.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _shadow.transform.localScale = new Vector3(bodySize.x * 1.3f, 0.3f, 1f);
        }

        // 3. Body / Torso Cyber Chassis or Custom Sprite
        if (customSprite != null)
        {
            if (_body == null)
            {
                var body = new GameObject("Body", typeof(SpriteRenderer));
                body.transform.SetParent(_visualRoot, false);
                _body = body.GetComponent<SpriteRenderer>();
            }
            _body.sprite = customSprite;
            _body.color = Color.white;
            _body.sortingOrder = 8;
            _body.transform.localPosition = new Vector3(0f, -customSprite.bounds.min.y, 0f);
            _body.transform.localScale = Vector3.one;

            if (_core != null) _core.gameObject.SetActive(false);
            if (_head != null) _head.gameObject.SetActive(false);
            if (_visor != null) _visor.gameObject.SetActive(false);
            if (_thruster != null) _thruster.gameObject.SetActive(false);
        }
        else
        {
            if (_body == null)
            {
                var body = new GameObject("Body", typeof(SpriteRenderer));
                body.transform.SetParent(_visualRoot, false);
                _body = body.GetComponent<SpriteRenderer>();
            }
            _body.sprite = ChassisSprite();
            _body.color = bodyColor;
            _body.sortingOrder = 8;
            _body.transform.localPosition = new Vector3(0f, bodySize.y * 0.45f, 0f);
            _body.transform.localScale = new Vector3(bodySize.x, bodySize.y * 0.85f, 1f);

            // 4. Glowing Reactor Core in chest
            if (_core == null)
            {
                var coreGo = new GameObject("ReactorCore", typeof(SpriteRenderer));
                coreGo.transform.SetParent(_body.transform, false);
                _core = coreGo.GetComponent<SpriteRenderer>();
                _core.sprite = CoreSprite();
                _core.color = visorColor;
                _core.sortingOrder = 9;
                _core.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                _core.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            }

            // 5. Head / Helmet
            if (_head == null)
            {
                var head = new GameObject("Head", typeof(SpriteRenderer));
                head.transform.SetParent(_visualRoot, false);
                _head = head.GetComponent<SpriteRenderer>();
            }
            _head.sprite = HeadSprite();
            _head.color = bodyColor * 1.15f;
            _head.sortingOrder = 10;
            _initialHeadPos = new Vector3(0f, bodySize.y + bodySize.x * 0.35f, 0f);
            _head.transform.localPosition = _initialHeadPos;
            _head.transform.localScale = Vector3.one * (bodySize.x * 1.05f);

            // 6. Glowing Visor on helmet
            if (_visor == null)
            {
                var visorGo = new GameObject("Visor", typeof(SpriteRenderer));
                visorGo.transform.SetParent(_head.transform, false);
                _visor = visorGo.GetComponent<SpriteRenderer>();
                _visor.sprite = VisorSprite();
                _visor.color = visorColor;
                _visor.sortingOrder = 11;
                _visor.transform.localPosition = new Vector3(0.02f, 0.02f, 0f);
                _visor.transform.localScale = new Vector3(0.6f, 0.28f, 1f);
            }

            // 7. Hover Thruster Glow below body
            if (_thruster == null)
            {
                var thrusterGo = new GameObject("HoverThruster", typeof(SpriteRenderer));
                thrusterGo.transform.SetParent(_visualRoot, false);
                _thruster = thrusterGo.GetComponent<SpriteRenderer>();
                _thruster.sprite = ThrusterSprite();
                _thruster.color = new Color(visorColor.r, visorColor.g, visorColor.b, 0.85f);
                _thruster.sortingOrder = 7;
                _thruster.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                _thruster.transform.localScale = new Vector3(bodySize.x * 0.6f, 0.2f, 1f);
            }
        }

        // 8. Speech Hologram (Equalizer wave display active during dialogue)
        if (_speechHolo == null)
        {
            var holoGo = new GameObject("SpeechHologram");
            holoGo.transform.SetParent(_visualRoot, false);
            _speechHolo = holoGo.transform;
            _speechHolo.localPosition = new Vector3(0f, bodySize.y + bodySize.x * 0.95f, 0f);

            _holoBars = new SpriteRenderer[3];
            float[] offsets = { -0.15f, 0f, 0.15f };
            for (int i = 0; i < 3; i++)
            {
                var barGo = new GameObject($"HoloBar_{i}", typeof(SpriteRenderer));
                barGo.transform.SetParent(_speechHolo, false);
                barGo.transform.localPosition = new Vector3(offsets[i], 0f, 0f);
                var barRend = barGo.GetComponent<SpriteRenderer>();
                barRend.sprite = HoloBarSprite();
                barRend.color = new Color(0.3f, 0.95f, 1f, 0.9f);
                barRend.sortingOrder = 30;
                _holoBars[i] = barRend;
            }
            _speechHolo.gameObject.SetActive(false);
        }

        // 9. Floating Interact Hint Prompt with Animated Keyboard Key [E]
        if (_prompt == null)
        {
            var promptGo = new GameObject("Interact hint", typeof(TextMesh));
            promptGo.transform.SetParent(transform, false);
            _prompt = promptGo.GetComponent<TextMesh>();

            _keyPrompt = promptGo.AddComponent<InteractKeyPrompt>();
            string promptLabel = !string.IsNullOrEmpty(interactPrompt) ? interactPrompt : "[E] TALK";
            _keyPrompt.SetLabel(promptLabel);
        }
        _prompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _prompt.text = "";
        _prompt.fontSize = 64;
        _prompt.characterSize = 0.045f;
        _prompt.anchor = TextAnchor.MiddleCenter;
        _prompt.alignment = TextAlignment.Center;
        _prompt.color = new Color(0.45f, 0.95f, 0.75f);
        _prompt.GetComponent<MeshRenderer>().sortingOrder = 40;
        var pPos = new Vector3(0f, promptHeight + bodySize.y, 0f);
        _prompt.transform.localPosition = pPos;
        if (_keyPrompt != null) _keyPrompt.SetBasePosition(pPos);
        _prompt.gameObject.SetActive(false);
    }

    public static Sprite BoxSprite()
    {
        if (_box != null) return _box;
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false) { name = "NPC_Box" };
        var pixels = new Color32[64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        _box = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        _box.name = "NPC_Box";
        return _box;
    }

    private static Sprite ChassisSprite()
    {
        if (_chassisSprite != null) return _chassisSprite;
        int w = 16, h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NPC_Chassis" };
        var c = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Silhouette shaping: shoulders wider, waist tapered, thruster base
                bool inside = x >= 1 && x <= w - 2 && y >= 1 && y <= h - 2;
                if (!inside) { c[y * w + x] = new Color32(0, 0, 0, 0); continue; }

                // Outer border
                if (x == 1 || x == w - 2 || y == 1 || y == h - 2)
                    c[y * w + x] = new Color32(40, 50, 65, 255);
                // Shoulder guards
                else if (y >= h - 6 && (x <= 3 || x >= w - 4))
                    c[y * w + x] = new Color32(220, 230, 245, 255);
                // Belt / utility buckle
                else if (y == 5 || y == 6)
                    c[y * w + x] = new Color32(50, 60, 75, 255);
                // Chest armor plate
                else
                    c[y * w + x] = new Color32(180, 195, 215, 255);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _chassisSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _chassisSprite;
    }

    private static Sprite HeadSprite()
    {
        if (_headSprite != null) return _headSprite;
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NPC_Head" };
        var c = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Antenna top beacon
                if (x == w / 2 && y >= h - 3)
                {
                    c[y * w + x] = y == h - 1 ? new Color32(255, 230, 80, 255) : new Color32(80, 90, 105, 255);
                    continue;
                }
                // Helmet dome
                bool inHelmet = x >= 2 && x <= w - 3 && y >= 1 && y <= h - 4;
                if (!inHelmet) { c[y * w + x] = new Color32(0, 0, 0, 0); continue; }

                // Outline
                if (x == 2 || x == w - 3 || y == 1 || y == h - 4)
                    c[y * w + x] = new Color32(35, 45, 58, 255);
                // Side ear/comms pods
                else if ((x == 3 || x == w - 4) && y >= 3 && y <= 8)
                    c[y * w + x] = new Color32(70, 85, 100, 255);
                // Visor recess
                else if (y >= 5 && y <= 8 && x >= 4 && x <= w - 5)
                    c[y * w + x] = new Color32(20, 25, 35, 255);
                else
                    c[y * w + x] = new Color32(200, 215, 235, 255);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _headSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _headSprite;
    }

    private static Sprite VisorSprite()
    {
        if (_visorSprite != null) return _visorSprite;
        int w = 16, h = 6;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NPC_Visor" };
        var c = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool eye = (x >= 2 && x <= 5 && y >= 1 && y <= 4) || (x >= 10 && x <= 13 && y >= 1 && y <= 4);
                bool pupil = (x == 3 || x == 4 || x == 11 || x == 12) && (y == 2 || y == 3);
                if (pupil)
                    c[y * w + x] = new Color32(255, 255, 255, 255);
                else if (eye)
                    c[y * w + x] = new Color32(190, 245, 255, 255);
                else
                    c[y * w + x] = new Color32(60, 140, 180, 180);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _visorSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _visorSprite;
    }

    private static Sprite CoreSprite()
    {
        if (_coreSprite != null) return _coreSprite;
        int w = 8, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NPC_Core" };
        var c = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f));
                if (dist <= 3.2f)
                {
                    byte alpha = (byte)Mathf.Clamp((1f - dist / 3.5f) * 255f + 40f, 0, 255);
                    c[y * w + x] = new Color32(255, 255, 255, alpha);
                }
                else c[y * w + x] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _coreSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _coreSprite;
    }

    private static Sprite ThrusterSprite()
    {
        if (_thrusterSprite != null) return _thrusterSprite;
        int w = 10, h = 6;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NPC_Thruster" };
        var c = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Inverted flame cone
                int span = (h - y) * w / (h * 2) + 1;
                int center = w / 2;
                if (x >= center - span && x <= center + span)
                    c[y * w + x] = new Color32(255, 255, 255, (byte)(255 * (y + 1) / h));
                else
                    c[y * w + x] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _thrusterSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 16f);
        return _thrusterSprite;
    }

    private static Sprite ShadowSprite()
    {
        if (_shadowSprite != null) return _shadowSprite;
        int w = 24, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "NPC_Shadow" };
        var c = new Color32[w * h];
        Vector2 center = new Vector2(w * 0.5f - 0.5f, h * 0.5f - 0.5f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - center.x) / (w * 0.5f);
                float dy = (y - center.y) / (h * 0.5f);
                float distSq = dx * dx + dy * dy;
                if (distSq <= 1f)
                {
                    byte a = (byte)(Mathf.Clamp01(1f - distSq) * 255f);
                    c[y * w + x] = new Color32(0, 0, 0, a);
                }
                else c[y * w + x] = new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(c);
        tex.Apply();
        _shadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _shadowSprite;
    }

    private static Sprite HoloBarSprite()
    {
        if (_holoBarSprite != null) return _holoBarSprite;
        int w = 4, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "NPC_HoloBar" };
        var c = new Color32[w * h];
        for (int i = 0; i < c.Length; i++) c[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(c);
        tex.Apply();
        _holoBarSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        return _holoBarSprite;
    }

    private static Sprite _box;
    private static Sprite _chassisSprite;
    private static Sprite _headSprite;
    private static Sprite _visorSprite;
    private static Sprite _coreSprite;
    private static Sprite _thrusterSprite;
    private static Sprite _shadowSprite;
    private static Sprite _holoBarSprite;
}
