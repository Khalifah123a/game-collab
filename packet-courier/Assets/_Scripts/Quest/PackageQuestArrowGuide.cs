using UnityEngine;

/// <summary>
/// Live Directional Arrow Guidance System for the Mandatory Package Quest across all stages:
/// - Phase 1 (Before Pickup - Offered/None): Prominent Cyan/Gold Directional Arrow pointing from
///   the Player to PackageGiverNPC + large bobbing World Marker above PackageGiverNPC ("WAJIB: AMBIL PAKET [E]").
/// - Phase 2 (After Pickup - Active): Sleek Small Tactical Arrow orbiting the Player pointing
///   toward PackageReceiverNPC (or ServerB_Goal) + Delivery Marker above Receiver ("SERAHKAN PAKET [E]").
/// - Phase 3 (Delivered/Claimed): Compact Gold Arrow pointing to ServerB_Goal warp portal.
/// </summary>
public class PackageQuestArrowGuide : MonoBehaviour
{
    public static PackageQuestArrowGuide Instance { get; private set; }

    private PlayerController _player;
    private LevelManager _levelManager;
    private PackageGiverNPC _giverNpc;
    private PackageReceiverNPC _receiverNpc;
    private ServerB_Goal _goalPortal;

    // Orbiting pointer around player
    private Transform _orbitPivot;
    private SpriteRenderer _arrowHeadRenderer;
    private SpriteRenderer _arrowGlowRenderer;
    private Transform _orbitLabelTransform;
    private TextMesh _orbitLabelText;
    private MeshRenderer _orbitLabelMr;

    // Floating world beacon above target NPC
    private Transform _npcBeaconRoot;
    private SpriteRenderer _npcBeaconArrowSr;
    private SpriteRenderer _npcBeaconPlateSr;
    private TextMesh _npcBeaconText;
    private MeshRenderer _npcBeaconMr;

    private float _rescanTimer;
    private PackageState _lastState = (PackageState)(-1);

    private static Sprite _cachedArrowSprite;
    private static Sprite _cachedPlateSprite;

    public static PackageQuestArrowGuide EnsureForStage(LevelManager lm = null)
    {
        if (Instance != null)
        {
            Instance._levelManager = lm != null ? lm : FindAnyObjectByType<LevelManager>();
            Instance.ScanTargets();
            return Instance;
        }

        var go = new GameObject("PackageQuestArrowGuide");
        Instance = go.AddComponent<PackageQuestArrowGuide>();
        Instance._levelManager = lm != null ? lm : FindAnyObjectByType<LevelManager>();
        Instance.ScanTargets();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildVisualHierarchy();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildVisualHierarchy()
    {
        // 1. Orbiting Pointer around Player
        var orbitGo = new GameObject("PlayerOrbitArrow");
        orbitGo.transform.SetParent(transform, false);
        _orbitPivot = orbitGo.transform;

        var glowGo = new GameObject("ArrowGlow", typeof(SpriteRenderer));
        glowGo.transform.SetParent(_orbitPivot, false);
        glowGo.transform.localPosition = new Vector3(0.05f, -0.04f, 0f);
        _arrowGlowRenderer = glowGo.GetComponent<SpriteRenderer>();
        _arrowGlowRenderer.sprite = GetChevronArrowSprite();
        _arrowGlowRenderer.sortingOrder = 46;
        _arrowGlowRenderer.color = new Color(0f, 0.1f, 0.2f, 0.75f);

        var headGo = new GameObject("ArrowHead", typeof(SpriteRenderer));
        headGo.transform.SetParent(_orbitPivot, false);
        headGo.transform.localPosition = Vector3.zero;
        _arrowHeadRenderer = headGo.GetComponent<SpriteRenderer>();
        _arrowHeadRenderer.sprite = GetChevronArrowSprite();
        _arrowHeadRenderer.sortingOrder = 47;

        var lblGo = new GameObject("OrbitLabel", typeof(TextMesh));
        lblGo.transform.SetParent(transform, false);
        _orbitLabelTransform = lblGo.transform;
        _orbitLabelText = lblGo.GetComponent<TextMesh>();
        _orbitLabelText.font = PixelUIKit.GetDisplayFont();
        _orbitLabelText.fontSize = 32;
        _orbitLabelText.characterSize = 0.036f;
        _orbitLabelText.anchor = TextAnchor.MiddleCenter;
        _orbitLabelText.alignment = TextAlignment.Center;
        _orbitLabelMr = lblGo.GetComponent<MeshRenderer>();
        if (_orbitLabelMr != null)
        {
            if (_orbitLabelText.font != null && _orbitLabelText.font.material != null)
                _orbitLabelMr.sharedMaterial = _orbitLabelText.font.material;
            _orbitLabelMr.sortingOrder = 48;
        }

        // 2. Floating World Beacon above Target NPC
        var beaconGo = new GameObject("NPCObjectiveBeacon");
        beaconGo.transform.SetParent(transform, false);
        _npcBeaconRoot = beaconGo.transform;

        var plateGo = new GameObject("BeaconPlate", typeof(SpriteRenderer));
        plateGo.transform.SetParent(_npcBeaconRoot, false);
        plateGo.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        _npcBeaconPlateSr = plateGo.GetComponent<SpriteRenderer>();
        _npcBeaconPlateSr.sprite = GetBeaconPlateSprite();
        _npcBeaconPlateSr.sortingOrder = 44;

        var bTextGo = new GameObject("BeaconText", typeof(TextMesh));
        bTextGo.transform.SetParent(_npcBeaconRoot, false);
        bTextGo.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        _npcBeaconText = bTextGo.GetComponent<TextMesh>();
        _npcBeaconText.font = PixelUIKit.GetDisplayFont();
        _npcBeaconText.fontSize = 36;
        _npcBeaconText.characterSize = 0.042f;
        _npcBeaconText.anchor = TextAnchor.MiddleCenter;
        _npcBeaconText.alignment = TextAlignment.Center;
        _npcBeaconMr = bTextGo.GetComponent<MeshRenderer>();
        if (_npcBeaconMr != null)
        {
            if (_npcBeaconText.font != null && _npcBeaconText.font.material != null)
                _npcBeaconMr.sharedMaterial = _npcBeaconText.font.material;
            _npcBeaconMr.sortingOrder = 46;
        }

        var bArrowGo = new GameObject("BeaconDownArrow", typeof(SpriteRenderer));
        bArrowGo.transform.SetParent(_npcBeaconRoot, false);
        bArrowGo.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        bArrowGo.transform.localRotation = Quaternion.Euler(0f, 0f, -90f); // Pointing straight down at NPC!
        _npcBeaconArrowSr = bArrowGo.GetComponent<SpriteRenderer>();
        _npcBeaconArrowSr.sprite = GetChevronArrowSprite();
        _npcBeaconArrowSr.sortingOrder = 46;
    }

    public void ScanTargets()
    {
        if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        if (_levelManager == null) _levelManager = FindAnyObjectByType<LevelManager>();
        if (_giverNpc == null) _giverNpc = FindAnyObjectByType<PackageGiverNPC>();
        if (_receiverNpc == null) _receiverNpc = FindAnyObjectByType<PackageReceiverNPC>();
        if (_goalPortal == null) _goalPortal = FindAnyObjectByType<ServerB_Goal>();
    }

    private void LateUpdate()
    {
        _rescanTimer -= Time.unscaledDeltaTime;
        if (_rescanTimer <= 0f || _player == null)
        {
            _rescanTimer = 0.8f;
            ScanTargets();
        }

        // Hide arrows while fullscreen modals or opening prologue cutscene are open
        bool hideForModal = CyberOpeningAndTutorialUI.IsPrologueOpen ||
                            ShopUI.IsShopOpen ||
                            OverclockUI.IsOverclockOpen ||
                            PauseMenuUI.IsPaused;

        if (_player == null || hideForModal)
        {
            SetGuidesVisible(false, false);
            return;
        }

        var gm = GameManager.Instance;
        int worldIdx = gm != null ? gm.currentWorldIndex : (_levelManager != null ? _levelManager.worldIndex : 0);
        PackageState pkgState = (gm != null && gm.packageStates != null && worldIdx >= 0 && worldIdx < gm.packageStates.Length)
            ? gm.packageStates[worldIdx]
            : PackageState.Offered;

        // Play subtle chime when package state transitions from Offered -> Active (picked up!)
        if (_lastState != pkgState)
        {
            _lastState = pkgState;
        }

        float t = Time.unscaledTime;
        Vector3 playerPos = _player.transform.position + new Vector3(0f, 0.55f, 0f);

        // =====================================================================
        // STATE 1: BEFORE PICKING UP PACKAGE (None or Offered)
        // Prominent Arrow pointing to PackageGiverNPC!
        // =====================================================================
        if (pkgState < PackageState.Active)
        {
            if (_giverNpc == null)
            {
                SetGuidesVisible(false, false);
                return;
            }

            Vector3 targetPos = _giverNpc.transform.position + new Vector3(0f, 1.0f, 0f);
            Vector2 delta = (Vector2)(targetPos - playerPos);
            float dist = delta.magnitude;
            Vector2 dir = dist > 0.01f ? (delta / dist) : Vector2.right;

            Color cyanPulse = Color.Lerp(new Color(0f, 0.94f, 1f, 1f), new Color(1f, 0.85f, 0.25f, 1f), 0.35f + 0.35f * Mathf.Sin(t * 7f));

            // 1A. Prominent Orbiting Arrow around Player (radius 1.48m, larger scale 0.95x)
            bool showOrbit = dist > 1.65f;
            _orbitPivot.gameObject.SetActive(showOrbit);
            _orbitLabelTransform.gameObject.SetActive(showOrbit);
            if (showOrbit)
            {
                float radius = 1.48f + 0.10f * Mathf.Sin(t * 8f);
                Vector3 arrowPos = playerPos + (Vector3)(dir * radius);
                _orbitPivot.position = arrowPos;
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _orbitPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                _orbitPivot.localScale = Vector3.one * 0.95f;

                _arrowHeadRenderer.color = cyanPulse;
                _orbitLabelTransform.position = arrowPos + new Vector3(0f, 0.42f, 0f);
                _orbitLabelText.text = $"WAJIB: AMBIL PAKET [E] ({Mathf.RoundToInt(dist)}m)";
                _orbitLabelText.color = cyanPulse;
            }

            // 1B. Floating World Beacon right above PackageGiverNPC
            _npcBeaconRoot.gameObject.SetActive(!DialogUI.IsOpen);
            float bob = Mathf.Sin(t * 7.5f) * 0.18f;
            _npcBeaconRoot.position = _giverNpc.transform.position + new Vector3(0f, 2.35f + bob, 0f);
            _npcBeaconArrowSr.transform.localScale = Vector3.one * (1.15f + 0.12f * Mathf.Sin(t * 9f));
            _npcBeaconArrowSr.color = cyanPulse;
            _npcBeaconPlateSr.color = new Color(0.03f, 0.09f, 0.18f, 0.94f);
            _npcBeaconText.text = "WAJIB: AMBIL PAKET [E]";
            _npcBeaconText.color = cyanPulse;
            return;
        }

        // =====================================================================
        // STATE 2: AFTER PICKING UP PACKAGE (PackageState.Active)
        // Small Tactical Arrow pointing toward PackageReceiverNPC (or ServerB_Goal)!
        // =====================================================================
        if (pkgState == PackageState.Active)
        {
            Transform destTr = _receiverNpc != null
                ? _receiverNpc.transform
                : (_goalPortal != null ? _goalPortal.transform : null);

            if (destTr == null)
            {
                SetGuidesVisible(false, false);
                return;
            }

            Vector3 targetPos = destTr.position + new Vector3(0f, 1.1f, 0f);
            Vector2 delta = (Vector2)(targetPos - playerPos);
            float dist = delta.magnitude;
            Vector2 dir = dist > 0.01f ? (delta / dist) : Vector2.right;

            Color mintColor = new Color(0.22f, 0.98f, 0.65f, 0.95f);

            // 2A. Small Tactical Pointer Arrow orbiting Player (scale 0.62x, radius 1.25m)
            bool showSmallArrow = dist > 1.8f;
            _orbitPivot.gameObject.SetActive(showSmallArrow);
            _orbitLabelTransform.gameObject.SetActive(showSmallArrow);
            if (showSmallArrow)
            {
                float radius = 1.22f + 0.06f * Mathf.Sin(t * 6f);
                Vector3 arrowPos = playerPos + (Vector3)(dir * radius);
                _orbitPivot.position = arrowPos;
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _orbitPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                _orbitPivot.localScale = Vector3.one * 0.64f; // Smaller arrow as requested!

                _arrowHeadRenderer.color = mintColor;
                _orbitLabelTransform.position = arrowPos + new Vector3(0f, 0.34f, 0f);
                string targetLabel = _receiverNpc != null ? "PENERIMA PAKET" : "SERVER B";
                _orbitLabelText.text = $"{targetLabel} >> {Mathf.RoundToInt(dist)}m";
                _orbitLabelText.color = mintColor;
            }

            // 2B. Floating Beacon above PackageReceiverNPC (or ServerB_Goal when within 30m)
            bool showReceiverBeacon = !DialogUI.IsOpen && dist < 35f;
            _npcBeaconRoot.gameObject.SetActive(showReceiverBeacon);
            if (showReceiverBeacon)
            {
                float bob = Mathf.Sin(t * 7f) * 0.16f;
                _npcBeaconRoot.position = destTr.position + new Vector3(0f, 2.55f + bob, 0f);
                _npcBeaconArrowSr.transform.localScale = Vector3.one * 1.0f;
                _npcBeaconArrowSr.color = mintColor;
                _npcBeaconPlateSr.color = new Color(0.03f, 0.14f, 0.10f, 0.94f);
                _npcBeaconText.text = _receiverNpc != null ? "SERAHKAN PAKET [E]" : "MENUJU STAGE BERIKUTNYA >>";
                _npcBeaconText.color = mintColor;
            }
            return;
        }

        // =====================================================================
        // STATE 3: DELIVERED / CLAIMED -> Point small arrow to ServerB_Goal Portal
        // =====================================================================
        if (_goalPortal != null)
        {
            Vector3 targetPos = _goalPortal.transform.position + new Vector3(0f, 1.0f, 0f);
            Vector2 delta = (Vector2)(targetPos - playerPos);
            float dist = delta.magnitude;
            Vector2 dir = dist > 0.01f ? (delta / dist) : Vector2.right;
            Color goldCol = new Color(1f, 0.86f, 0.25f, 0.95f);

            bool showGoalArrow = dist > 1.5f;
            _orbitPivot.gameObject.SetActive(showGoalArrow);
            _orbitLabelTransform.gameObject.SetActive(showGoalArrow);
            if (showGoalArrow)
            {
                Vector3 arrowPos = playerPos + (Vector3)(dir * 1.20f);
                _orbitPivot.position = arrowPos;
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _orbitPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                _orbitPivot.localScale = Vector3.one * 0.62f;
                _arrowHeadRenderer.color = goldCol;

                _orbitLabelTransform.position = arrowPos + new Vector3(0f, 0.34f, 0f);
                _orbitLabelText.text = $"PORTAL TERBUKA >> {Mathf.RoundToInt(dist)}m";
                _orbitLabelText.color = goldCol;
            }

            _npcBeaconRoot.gameObject.SetActive(dist < 25f);
            if (dist < 25f)
            {
                float bob = Mathf.Sin(t * 7f) * 0.15f;
                _npcBeaconRoot.position = _goalPortal.transform.position + new Vector3(0f, 2.5f + bob, 0f);
                _npcBeaconArrowSr.transform.localScale = Vector3.one * 0.95f;
                _npcBeaconArrowSr.color = goldCol;
                _npcBeaconPlateSr.color = new Color(0.14f, 0.10f, 0.02f, 0.94f);
                _npcBeaconText.text = "MASUK GATEWAY SERVER B";
                _npcBeaconText.color = goldCol;
            }
        }
        else
        {
            SetGuidesVisible(false, false);
        }
    }

    private void SetGuidesVisible(bool orbit, bool beacon)
    {
        if (_orbitPivot != null && _orbitPivot.gameObject.activeSelf != orbit)
            _orbitPivot.gameObject.SetActive(orbit);
        if (_orbitLabelTransform != null && _orbitLabelTransform.gameObject.activeSelf != orbit)
            _orbitLabelTransform.gameObject.SetActive(orbit);
        if (_npcBeaconRoot != null && _npcBeaconRoot.gameObject.activeSelf != beacon)
            _npcBeaconRoot.gameObject.SetActive(beacon);
    }

    // -------------------------------------------------------------------------
    // Procedural Crisp Arrow & Plate Sprites (Pointing +X by default)
    // -------------------------------------------------------------------------
    private static Sprite GetChevronArrowSprite()
    {
        if (_cachedArrowSprite != null) return _cachedArrowSprite;

        const int w = 32;
        const int h = 28;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        float midY = (h - 1) * 0.5f;
        for (int y = 0; y < h; y++)
        {
            float dy = Mathf.Abs(y - midY);
            for (int x = 0; x < w; x++)
            {
                // Sharp double-wing tactical chevron pointing right (+X)
                int tipX = w - 2 - Mathf.RoundToInt(dy * 1.15f);
                int tailX = 10 - Mathf.RoundToInt(dy * 0.85f);
                bool inHead = x <= tipX && x >= tailX;
                bool inStem = dy <= 3.2f && x >= 2 && x <= tailX + 2;

                if (inHead || inStem)
                {
                    bool isEdge = (x == tipX || x == tailX || Mathf.Abs(dy - 3.2f) < 1f && x < tailX);
                    tex.SetPixel(x, y, isEdge ? Color.white : new Color(0.92f, 0.98f, 1f, 0.96f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        _cachedArrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 40f);
        return _cachedArrowSprite;
    }

    private static Sprite GetBeaconPlateSprite()
    {
        if (_cachedPlateSprite != null) return _cachedPlateSprite;

        const int w = 140;
        const int h = 28;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < h; y++)
        {
            int slant = (h - 1 - y) / 3;
            for (int x = 0; x < w; x++)
            {
                if (x < slant || x >= w - slant)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }
                bool border = (y < 2 || y >= h - 2 || x < slant + 2 || x >= w - slant - 2);
                tex.SetPixel(x, y, border ? new Color(0f, 0.92f, 1f, 1f) : Color.white);
            }
        }
        tex.Apply();
        _cachedPlateSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 42f);
        return _cachedPlateSprite;
    }
}
