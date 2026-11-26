using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cinematic Cyber Opening Prologue + Concise 5-Step Interactive Field Tutorial.
/// Features its own dedicated synthwave BGM track (BGMTrack.Tutorial / bgm_tutorial.wav),
/// holographic speaker portrait (ARIA-7 // Operator Dispatch), visual story slides,
/// live action-detection tutorial steps, and world-space objective beacon.
/// </summary>
public class CyberOpeningAndTutorialUI : MonoBehaviour
{
    public enum SequenceMode
    {
        Inactive,
        OpeningPrologue,
        InteractiveTutorial,
        CompletionBanner
    }

    private struct PrologueSlide
    {
        public string ChapterTag;
        public stringHeadline Headline;
        public string Speaker;
        public string Body;
        public Color AccentColor;
        public int VisualType; // 0=CorruptRootkit, 1=CourierBoot, 2=TTLProtocol, 3=RouteMission
    }

    private struct stringHeadline
    {
        public string Title;
        public string Subtitle;
        public stringHeadline(string title, string subtitle) { Title = title; Subtitle = subtitle; }
    }

    private struct TutorialStepDef
    {
        public string StepTag;
        public string Title;
        public string Dialogue;
        public string KeyHint;
        public string ObjectiveHint;
    }

    public static CyberOpeningAndTutorialUI Instance { get; private set; }
    public static bool PendingOnboarding;
    public static bool HasCompletedOnboardingThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticOnboardingFlags()
    {
        Instance = null;
        PendingOnboarding = false;
        HasCompletedOnboardingThisSession = false;
    }

    public static bool IsActive => Instance != null && Instance._mode != SequenceMode.Inactive;
    public static bool IsPrologueOpen => Instance != null && Instance._mode == SequenceMode.OpeningPrologue;
    public static bool IsTutorialRunning => Instance != null &&
        (Instance._mode == SequenceMode.OpeningPrologue || Instance._mode == SequenceMode.InteractiveTutorial);

    private SequenceMode _mode = SequenceMode.Inactive;

    private Canvas _canvas;
    private CanvasGroup _rootGroup;

    // --- Prologue UI References ---
    private GameObject _prologueRoot;
    private Image _slideCardBorder;
    private Image _slideVisualIcon;
    private Image _slideSecondaryIcon;
    private Text _prologueChapterText;
    private Text _prologueHeadlineText;
    private Text _prologueSubheadText;
    private Text _prologueSpeakerText;
    private Text _prologueBodyText;
    private Text _prologueContinueText;
    private Text _prologueStageCaptionText;
    private Image _prologuePortraitImg;
    private RectTransform _prologueScanlineRt;
    private readonly Image[] _prologuePips = new Image[4];

    // --- Interactive Tutorial UI References ---
    private GameObject _tutorialRoot;
    private Image _tutPortraitImg;
    private Text _tutStepTagText;
    private Text _tutTitleText;
    private Text _tutDialogueText;
    private Text _tutKeyCapText;
    private Text _tutObjectiveText;
    private RectTransform _tutProgressFillRt;
    private readonly Image[] _tutStepPips = new Image[5];

    // --- World-Space Waypoint Beacon ---
    private GameObject _worldBeaconGo;
    private TextMesh _worldBeaconText;
    private SpriteRenderer _worldBeaconDiamond;

    // --- State Tracking ---
    private int _slideIndex;
    private float _slideAnimTimer;
    private string _currentFullSlideText = string.Empty;
    private int _typedChars;
    private float _typeTimer;
    private bool _slideFullyTyped;

    private int _tutStepIndex;
    private float _tutStepProgress;
    private Vector3 _lastPlayerPos;
    private bool _lastPosInitialized;
    private float _accumMoveDist;
    private int _jumpCount;
    private float _stepHoldTimer;
    private float _bannerTimer;

    private PlayerController _player;
    private PlayerHealth _health;
    private PlayerCombat _combat;
    private PlayerAnimator _animator;

    private static Sprite _operatorPortraitSpr;
    private static Sprite _archivePortraitSpr;
    private static Sprite _ttlDiagramSpr;

    private static readonly PrologueSlide[] Slides =
    {
        new PrologueSlide
        {
            ChapterTag = "PROLOG 1 / 4",
            Headline = new stringHeadline("JARINGAN RUNTUH", "Tahun 2099 — Rootkit Prime"),
            Speaker = "ARSIP 2099",
            Body = "Rootkit Prime membajak jalur utama jaringan dunia. Server antar-kota terputus, dan jutaan paket data penting terjebak di balik firewall.",
            AccentColor = new Color(1f, 0.22f, 0.38f, 1f),
            VisualType = 0
        },
        new PrologueSlide
        {
            ChapterTag = "PROLOG 2 / 4",
            Headline = new stringHeadline("KURIR K-01 BANGUN", "Bunker Localhost"),
            Speaker = "ARIA-7",
            Body = "Syukurlah kamu aktif, K-01. Kamu kurir terakhir yang tersisa di Localhost. Tanpamu, seluruh jaringan bakal padam selamanya.",
            AccentColor = new Color(0f, 0.94f, 1f, 1f),
            VisualType = 1
        },
        new PrologueSlide
        {
            ChapterTag = "PROLOG 3 / 4",
            Headline = new stringHeadline("ATURAN TTL", "Aman di Bunker, Terkuras di Luar"),
            Speaker = "ARIA-7",
            Body = "Di dalam Safe Zone hijau, TTL-mu aman. Begitu melangkah keluar, energi TTL terus berkurang setiap detik. Jangan sampai habis.",
            AccentColor = new Color(1f, 0.65f, 0.18f, 1f),
            VisualType = 2
        },
        new PrologueSlide
        {
            ChapterTag = "PROLOG 4 / 4",
            Headline = new stringHeadline("ANTAR PAKETNYA", "Server A ke Server B"),
            Speaker = "ARIA-7",
            Body = "Ambil paket dari kurir di Safe Zone, lewati rintangan, lalu serahkan ke Server B di ujung kanan. Ayo tes gerakmu dulu sebentar!",
            AccentColor = new Color(0.21f, 0.96f, 0.61f, 1f),
            VisualType = 3
        }
    };

    private static readonly TutorialStepDef[] TutorialSteps =
    {
        new TutorialStepDef
        {
            StepTag = "LATIHAN 1 / 5",
            Title = "GERAK & DOUBLE JUMP",
            Dialogue = "ARIA-7: \"Jalan pakai [A]/[D], lalu tekan [SPACE] 2x di udara untuk Double Jump.\"",
            KeyHint = "[A / D] Gerak  •  [SPACE 2x] Double Jump",
            ObjectiveHint = "Bergerak & lompat"
        },
        new TutorialStepDef
        {
            StepTag = "LATIHAN 2 / 5",
            Title = "DASH (KEBAL SESAAT)",
            Dialogue = "ARIA-7: \"Bagus! Tekan [LEFT SHIFT] untuk Dash. Saat melesat, kamu kebal sesaat dari serangan.\"",
            KeyHint = "[LEFT SHIFT] Dash",
            ObjectiveHint = "Tekan [LEFT SHIFT] 1x"
        },
        new TutorialStepDef
        {
            StepTag = "LATIHAN 3 / 5",
            Title = "SERANG & PARRY",
            Dialogue = "ARIA-7: \"Tebas musuh dengan [KLIK KIRI / J], atau tangkis serangan dengan [Q].\"",
            KeyHint = "[KLIK KIRI / J] Tebas  •  [Q] Parry",
            ObjectiveHint = "Coba tebas atau parry"
        },
        new TutorialStepDef
        {
            StepTag = "LATIHAN 4 / 5",
            Title = "AMBIL PAKET",
            Dialogue = "ARIA-7: \"Dekati NPC Kurir di Safe Zone dan tekan [E] untuk mengambil paket.\"",
            KeyHint = "[E] Ambil Paket",
            ObjectiveHint = "Tekan [E] di NPC Kurir"
        },
        new TutorialStepDef
        {
            StepTag = "LATIHAN 5 / 5",
            Title = "KELUAR SAFE ZONE",
            Dialogue = "ARIA-7: \"Paket sudah siap! Jalan ke kanan melewati Pintu Hijau menuju Server B.\"",
            KeyHint = "Jalan ke kanan -> Pintu Hijau",
            ObjectiveHint = "Lewati Pintu Hijau"
        }
    };

    public static void ArmForNextWorld1Entry()
    {
        PendingOnboarding = true;
    }

    public static CyberOpeningAndTutorialUI StartSequence(bool includeOpeningPrologue = true)
    {
        if (Instance == null)
        {
            var go = new GameObject("CyberOpeningAndTutorial_Canvas");
            Instance = go.AddComponent<CyberOpeningAndTutorialUI>();
        }
        Instance.BeginSequenceInternal(includeOpeningPrologue);
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
        BuildCanvasAndPanels();
    }

    private void OnDestroy()
    {
        if (_worldBeaconGo != null) Destroy(_worldBeaconGo);
        if (Instance == this) Instance = null;
    }

    private void BeginSequenceInternal(bool includeOpening)
    {
        PendingOnboarding = false;
        CachePlayerReferences();

        if (_canvas == null) BuildCanvasAndPanels();
        _canvas.gameObject.SetActive(true);
        if (_rootGroup != null)
        {
            _rootGroup.alpha = 1f;
            _rootGroup.blocksRaycasts = true;
        }

        // Start dedicated Tutorial & Prologue BGM track
        AudioManager.SetMuffled(false);
        AudioManager.PlayStageOpeningBGM(AudioManager.BGMTrack.Tutorial, 1.4f);

        if (includeOpening)
        {
            _mode = SequenceMode.OpeningPrologue;
            _canvas.sortingOrder = 55;
            _prologueRoot.SetActive(true);
            _tutorialRoot.SetActive(false);
            ShowPrologueSlide(0);
        }
        else
        {
            StartInteractiveTutorialPhase();
        }
    }

    private void ShowPrologueSlide(int index)
    {
        _slideIndex = Mathf.Clamp(index, 0, Slides.Length - 1);
        _slideAnimTimer = 0f;
        PrologueSlide slide = Slides[_slideIndex];

        _prologueChapterText.text = slide.ChapterTag;
        _prologueChapterText.color = slide.AccentColor;
        _prologueHeadlineText.text = slide.Headline.Title;
        _prologueSubheadText.text = slide.Headline.Subtitle;
        _prologueSubheadText.color = slide.AccentColor;
        _prologueSpeakerText.text = slide.Speaker;
        _prologueSpeakerText.color = slide.AccentColor;
        _slideCardBorder.color = new Color(slide.AccentColor.r, slide.AccentColor.g, slide.AccentColor.b, 0.90f);

        _currentFullSlideText = slide.Body;
        _typedChars = 0;
        _typeTimer = 0f;
        _slideFullyTyped = false;
        _prologueBodyText.text = string.Empty;

        _prologuePortraitImg.sprite = slide.VisualType == 0 ? GetArchivePortraitSprite() : GetOperatorPortraitSprite();

        // Update slide centerpiece artwork & stage caption
        if (slide.VisualType == 0)
        {
            _slideVisualIcon.sprite = PixelUIKit.GetCorruptLogo();
            _slideVisualIcon.color = Color.white;
            _slideSecondaryIcon.sprite = PixelUIKit.GetIcon("skull");
            _slideSecondaryIcon.color = slide.AccentColor;
            if (_prologueStageCaptionText != null)
                _prologueStageCaptionText.text = "Prolog 1/4  •  Bunker Safe Zone";
        }
        else if (slide.VisualType == 1)
        {
            Sprite courierSpr = Resources.Load<Sprite>("PlayerSprites/idle1") ?? PixelUIKit.GetParcelLogo();
            _slideVisualIcon.sprite = courierSpr;
            _slideVisualIcon.color = Color.white;
            _slideSecondaryIcon.sprite = PixelUIKit.GetIcon("bolt");
            _slideSecondaryIcon.color = slide.AccentColor;
            if (_prologueStageCaptionText != null)
                _prologueStageCaptionText.text = "Prolog 2/4  •  Tes Jalan Kurir K-01";
        }
        else if (slide.VisualType == 2)
        {
            _slideVisualIcon.sprite = GetTTLDiagramSprite();
            _slideVisualIcon.color = Color.white;
            _slideSecondaryIcon.sprite = PixelUIKit.GetIcon("shield");
            _slideSecondaryIcon.color = slide.AccentColor;
            if (_prologueStageCaptionText != null)
                _prologueStageCaptionText.text = "Prolog 3/4  •  Tes Dash & Lompat";
        }
        else
        {
            _slideVisualIcon.sprite = PixelUIKit.GetParcelLogo();
            _slideVisualIcon.color = Color.white;
            _slideSecondaryIcon.sprite = PixelUIKit.GetIcon("route");
            _slideSecondaryIcon.color = slide.AccentColor;
            if (_prologueStageCaptionText != null)
                _prologueStageCaptionText.text = "Prolog 4/4  •  Tes Tebasan Pedang";
        }

        for (int i = 0; i < _prologuePips.Length; i++)
        {
            _prologuePips[i].color = i == _slideIndex
                ? slide.AccentColor
                : (i < _slideIndex ? new Color(slide.AccentColor.r, slide.AccentColor.g, slide.AccentColor.b, 0.45f) : PixelUIKit.BorderSlate);
        }

        AudioManager.PlayUIClick();
    }

    public void AdvanceOrSkipPrologueSlide()
    {
        if (_mode != SequenceMode.OpeningPrologue) return;

        if (!_slideFullyTyped)
        {
            _typedChars = _currentFullSlideText.Length;
            _prologueBodyText.text = _currentFullSlideText;
            _slideFullyTyped = true;
            return;
        }

        if (_slideIndex + 1 < Slides.Length)
        {
            ShowPrologueSlide(_slideIndex + 1);
        }
        else
        {
            StartInteractiveTutorialPhase();
        }
    }

    public void SkipCurrentMode()
    {
        AudioManager.PlayUIClick();
        if (_mode == SequenceMode.OpeningPrologue)
        {
            StartInteractiveTutorialPhase();
        }
        else if (_mode == SequenceMode.InteractiveTutorial)
        {
            CompleteTutorial(awardBonus: false);
        }
    }

    private void StartInteractiveTutorialPhase()
    {
        _mode = SequenceMode.InteractiveTutorial;
        if (_canvas != null) _canvas.sortingOrder = 25; // Above HUD (20), below DialogUI (40) & Shop/Overclock (82)
        _prologueRoot.SetActive(false);
        _tutorialRoot.SetActive(true);

        CachePlayerReferences();
        if (_animator != null)
        {
            _animator.ClearCutsceneOverride();
        }
        if (_player != null)
        {
            _player.isFrozen = false;
            _lastPlayerPos = _player.transform.position;
            _lastPosInitialized = true;
        }

        // Ensure Tutorial BGM stays active
        if (AudioManager.Instance != null && AudioManager.Instance.CurrentBGMTrack != AudioManager.BGMTrack.Tutorial)
        {
            AudioManager.PlayBGM(AudioManager.BGMTrack.Tutorial, 0.8f);
        }

        _accumMoveDist = 0f;
        _jumpCount = 0;
        SetTutorialStep(0);
    }

    private void SetTutorialStep(int index)
    {
        _tutStepIndex = Mathf.Clamp(index, 0, TutorialSteps.Length - 1);
        _tutStepProgress = 0f;
        _stepHoldTimer = 0f;

        TutorialStepDef def = TutorialSteps[_tutStepIndex];
        _tutStepTagText.text = def.StepTag;
        _tutTitleText.text = def.Title;
        _tutDialogueText.text = def.Dialogue;
        _tutKeyCapText.text = def.KeyHint;
        _tutObjectiveText.text = def.ObjectiveHint;
        _tutProgressFillRt.anchorMax = new Vector2(0.02f, 1f);

        for (int i = 0; i < _tutStepPips.Length; i++)
        {
            _tutStepPips[i].color = i < _tutStepIndex
                ? PixelUIKit.Mint
                : (i == _tutStepIndex ? PixelUIKit.Cyan : PixelUIKit.BorderSlate);
        }

        AudioManager.PlayNPCTalk();
        UpdateWorldBeaconTarget();
    }

    private void Update()
    {
        if (_mode == SequenceMode.Inactive) return;

        CachePlayerReferences();

        // Safety guard for automated smoke tests: if a test disables InputManager or teleports player far away,
        // dismiss immediately so smoke tests run unobstructed.
        var input = InputManager.Instance;
        if ((input != null && !input.enabled) ||
            (_player != null && Mathf.Abs(_player.transform.position.x) > 350f))
        {
            ForceCloseImmediate();
            return;
        }

        // Auto-hide tutorial overlay whenever DialogUI, ShopUI, OverclockUI, or PauseMenuUI is open
        // so it NEVER blocks mouse clicks or covers NPC dialogue / upgrade modals!
        if (_mode != SequenceMode.OpeningPrologue && _rootGroup != null)
        {
            bool modalOpen = DialogUI.IsOpen || ShopUI.IsShopOpen || OverclockUI.IsOverclockOpen || PauseMenuUI.IsPaused;
            _rootGroup.alpha = modalOpen ? 0f : 1f;
            _rootGroup.blocksRaycasts = !modalOpen;
        }
        else if (_rootGroup != null)
        {
            _rootGroup.alpha = 1f;
            _rootGroup.blocksRaycasts = true;
        }

        if (_mode == SequenceMode.OpeningPrologue)
        {
            UpdateOpeningPrologue();
        }
        else if (_mode == SequenceMode.InteractiveTutorial)
        {
            UpdateInteractiveTutorial();
        }
        else if (_mode == SequenceMode.CompletionBanner)
        {
            _bannerTimer -= Time.unscaledDeltaTime;
            if (_bannerTimer <= 0f)
            {
                ForceCloseImmediate();
            }
        }
    }

    private void UpdateOpeningPrologue()
    {
        _slideAnimTimer += Time.unscaledDeltaTime;

        // Live 2D Player Cutscene Animation Choreography during Opening Dialogue!
        if (_player != null && !DialogUI.IsOpen)
        {
            var rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (_animator != null)
            {
                int vType = Slides[_slideIndex].VisualType;
                if (vType == 0)
                {
                    // Slide 0: Boot-up crouch ("Land") -> alert "Idle" scanning left & right
                    if (_slideAnimTimer < 0.75f)
                        _animator.SetCutsceneOverride("Land", 1f);
                    else
                        _animator.SetCutsceneOverride("Idle", Mathf.Sin(_slideAnimTimer * 1.9f) >= -0.2f ? 1f : -1f);
                }
                else if (vType == 1)
                {
                    // Slide 1: Unit K-01 motor calibration ("Walk" -> "Run" -> "Idle")
                    float cycle = Mathf.Repeat(_slideAnimTimer, 3.2f);
                    float dir = cycle < 1.6f ? 1f : -1f;
                    string st = (cycle % 1.6f) < 0.75f ? "Walk" : ((cycle % 1.6f) < 1.25f ? "Run" : "Idle");
                    _animator.SetCutsceneOverride(st, dir);
                }
                else if (vType == 2)
                {
                    // Slide 2: Phase Dash & Double Jump demonstration ("Dash" -> "Jump" -> "Land" -> "Idle")
                    float cycle = Mathf.Repeat(_slideAnimTimer, 2.8f);
                    if (cycle < 0.65f)
                        _animator.SetCutsceneOverride("Dash", 1f);
                    else if (cycle < 1.35f)
                        _animator.SetCutsceneOverride("Jump", 1f);
                    else if (cycle < 1.75f)
                        _animator.SetCutsceneOverride("Land", 1f);
                    else
                        _animator.SetCutsceneOverride("Idle", 1f);
                }
                else
                {
                    // Slide 3: Full 4-Frame Cyber Blade Attack demonstration ("Attack" -> "Idle")
                    float cycle = Mathf.Repeat(_slideAnimTimer, 2.2f);
                    if (cycle < 0.85f)
                        _animator.SetCutsceneOverride("Attack", 1f);
                    else
                        _animator.SetCutsceneOverride("Idle", 1f);
                }
            }
        }

        // Keep TTL full while watching opening prologue
        if (_health != null)
        {
            _health.ttlCurrent = _health.ttlMax;
        }

        // Animate scanline bar
        if (_prologueScanlineRt != null)
        {
            float y = Mathf.Repeat(Time.unscaledTime * 0.35f, 1f);
            _prologueScanlineRt.anchorMin = new Vector2(0f, y);
            _prologueScanlineRt.anchorMax = new Vector2(1f, Mathf.Min(1f, y + 0.04f));
        }

        // Typewriter reveal
        if (!_slideFullyTyped)
        {
            _typeTimer += Time.unscaledDeltaTime;
            int charsToAdd = Mathf.FloorToInt(_typeTimer * 54f);
            if (charsToAdd > 0)
            {
                _typeTimer -= charsToAdd / 54f;
                int prev = _typedChars;
                _typedChars = Mathf.Min(_currentFullSlideText.Length, _typedChars + charsToAdd);
                _prologueBodyText.text = _currentFullSlideText.Substring(0, _typedChars);
                if (_typedChars / 4 != prev / 4)
                {
                    AudioManager.PlayNPCTalk();
                }
                if (_typedChars >= _currentFullSlideText.Length)
                {
                    _slideFullyTyped = true;
                }
            }
        }

        string nextLabel = _slideIndex + 1 < Slides.Length
            ? (_slideFullyTyped ? "[SPACE / ENTER / KLIK] BABAK BERIKUTNYA >>" : "[SPACE / KLIK] TAMPILKAN SEMUA")
            : (_slideFullyTyped ? "[SPACE / ENTER / KLIK] MULAI TUTORIAL >>" : "[SPACE / KLIK] TAMPILKAN SEMUA");
        _prologueContinueText.text = nextLabel;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SkipCurrentMode();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
        {
            AdvanceOrSkipPrologueSlide();
        }
    }

    private void UpdateInteractiveTutorial()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !ShopUI.IsShopOpen && !OverclockUI.IsOverclockOpen)
        {
            SkipCurrentMode();
            return;
        }

        _stepHoldTimer += Time.deltaTime;
        Vector3 curPos = _player != null ? _player.transform.position : Vector3.zero;
        if (!_lastPosInitialized && _player != null)
        {
            _lastPlayerPos = curPos;
            _lastPosInitialized = true;
        }

        switch (_tutStepIndex)
        {
            case 0: // Step 1: Move + Jump
                if (_player != null)
                {
                    float dx = Mathf.Abs(curPos.x - _lastPlayerPos.x);
                    if (dx > 0.001f && dx < 3f) _accumMoveDist += dx;
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                        _jumpCount++;
                }
                float movePct = Mathf.Clamp01(_accumMoveDist / 2.2f);
                float jumpPct = Mathf.Clamp01(_jumpCount / 1f);
                _tutStepProgress = Mathf.Clamp01(movePct * 0.6f + jumpPct * 0.4f);
                break;

            case 1: // Step 2: Phase Dash
                if ((_player != null && _player.isDashing) ||
                    Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                {
                    _tutStepProgress = 1f;
                }
                break;

            case 2: // Step 3: Attack or Parry
                if ((_combat != null && (_combat.IsAttacking || _combat.IsParrying)) ||
                    Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
                    Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Q))
                {
                    _tutStepProgress = 1f;
                }
                break;

            case 3: // Step 4: Pick up Package at Server A in Safe Zone (must become PackageState.Active!)
                var gm = GameManager.Instance;
                bool hasPackage = gm != null && gm.packageStates != null &&
                                  gm.currentWorldIndex >= 0 && gm.currentWorldIndex < gm.packageStates.Length &&
                                  gm.packageStates[gm.currentWorldIndex] >= PackageState.Active;
                if (hasPackage)
                {
                    _tutStepProgress = 1f;
                }
                else
                {
                    var giver = FindAnyObjectByType<PackageGiverNPC>();
                    if (giver != null && _player != null)
                    {
                        float dist = Vector2.Distance(_player.transform.position, giver.transform.position);
                        _tutStepProgress = Mathf.Clamp01(1f - (dist - 1.2f) / 8f) * 0.70f;
                    }
                }
                break;

            case 4: // Step 5: Exit Safe Zone toward Server B
                if (!SafeZone.IsPlayerSafe || (_player != null && _player.transform.position.x > 1.2f))
                {
                    _tutStepProgress = 1f;
                }
                else if (_player != null)
                {
                    _tutStepProgress = Mathf.Clamp01((_player.transform.position.x + 8f) / 9f) * 0.85f;
                }
                break;
        }

        _lastPlayerPos = curPos;
        _tutProgressFillRt.anchorMax = new Vector2(Mathf.Clamp(_tutStepProgress, 0.03f, 1f), 1f);
        UpdateWorldBeaconPosition();

        if (_tutStepProgress >= 0.99f && _stepHoldTimer >= 0.35f)
        {
            if (_tutStepIndex + 1 < TutorialSteps.Length)
            {
                AudioManager.Play("sfx_collect", 0.75f, 1.15f);
                SetTutorialStep(_tutStepIndex + 1);
            }
            else
            {
                CompleteTutorial(awardBonus: true);
            }
        }
    }

    private void CompleteTutorial(bool awardBonus)
    {
        HasCompletedOnboardingThisSession = true;
        if (_worldBeaconGo != null) _worldBeaconGo.SetActive(false);

        if (awardBonus && GameManager.Instance != null)
        {
            GameManager.Instance.AddDataBits(50);
        }

        AudioManager.Play("sfx_collect", 0.9f, 1.25f);
        if (!TutorialWorldBuilder.IsTutorialWorldActive)
        {
            // Smoothly hand off from Tutorial BGM to World 1 BGM when not in dedicated World 00
            int wIdx = GameManager.Instance != null ? GameManager.Instance.currentWorldIndex : 0;
            bool bossActive = BossTracker.ActiveBoss != null && !BossTracker.ActiveBoss.IsDead;
            AudioManager.PlayWorldBGM(wIdx, SafeZone.IsPlayerSafe, bossActive, 2.0f);
        }

        _mode = SequenceMode.CompletionBanner;
        _bannerTimer = 3.2f;
        _prologueRoot.SetActive(false);
        _tutorialRoot.SetActive(true);

        _tutStepTagText.text = TutorialWorldBuilder.IsTutorialWorldActive
            ? "WORLD 00 — ARENA LATIHAN"
            : "TUTORIAL SELESAI";
        _tutStepTagText.color = PixelUIKit.Mint;
        _tutTitleText.text = awardBonus ? "SIAP BERANGKAT! (+50 DB)" : "SIAP BERANGKAT!";
        _tutDialogueText.text = TutorialWorldBuilder.IsTutorialWorldActive
            ? "ARIA-7: \"Lewati 4 zona latihan, serahkan paket ke NPC Penerima, lalu masuk Portal menuju World 1!\""
            : "ARIA-7: \"Semua kontrol sudah aktif. Antar paket ke Server B sebelum TTL habis!\"";
        _tutKeyCapText.text = TutorialWorldBuilder.IsTutorialWorldActive
            ? "Ikuti panah -> Serahkan Paket -> Portal World 1"
            : "Antar paket ke Server B di ujung kanan";
        _tutProgressFillRt.anchorMax = Vector2.one;
        for (int i = 0; i < _tutStepPips.Length; i++) _tutStepPips[i].color = PixelUIKit.Mint;
    }

    public void ForceCloseImmediate()
    {
        _mode = SequenceMode.Inactive;
        if (_animator != null) _animator.ClearCutsceneOverride();
        if (_prologueRoot != null) _prologueRoot.SetActive(false);
        if (_tutorialRoot != null) _tutorialRoot.SetActive(false);
        if (_worldBeaconGo != null) _worldBeaconGo.SetActive(false);
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    private void CachePlayerReferences()
    {
        if (_health == null) _health = PlayerHealth.Instance;
        if (_health != null && _player == null)
        {
            _player = _health.GetComponent<PlayerController>();
            _combat = _health.GetComponent<PlayerCombat>();
            _animator = _health.GetComponent<PlayerAnimator>();
        }
        else if (_player == null)
        {
            _player = FindAnyObjectByType<PlayerController>();
            if (_player != null)
            {
                _combat = _player.GetComponent<PlayerCombat>();
                _animator = _player.GetComponent<PlayerAnimator>();
            }
        }
        if (_player != null && _animator == null)
        {
            _animator = _player.GetComponent<PlayerAnimator>();
        }
    }

    private void UpdateWorldBeaconTarget()
    {
        if (_worldBeaconGo == null)
        {
            _worldBeaconGo = new GameObject("TutorialWorldBeacon");
            _worldBeaconText = _worldBeaconGo.AddComponent<TextMesh>();
            _worldBeaconText.font = PixelUIKit.GetDisplayFont();
            _worldBeaconText.fontSize = 36;
            _worldBeaconText.characterSize = 0.055f;
            _worldBeaconText.anchor = TextAnchor.LowerCenter;
            _worldBeaconText.alignment = TextAlignment.Center;
            _worldBeaconText.color = PixelUIKit.Mint;
            var mr = _worldBeaconGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 45;
        }

        if (_tutStepIndex == 3)
        {
            _worldBeaconGo.SetActive(true);
            _worldBeaconText.text = "AMBIL PAKET [E]\n▼";
            _worldBeaconText.color = PixelUIKit.Cyan;
        }
        else if (_tutStepIndex == 4)
        {
            _worldBeaconGo.SetActive(true);
            _worldBeaconText.text = "KELUAR SAFE ZONE >>\n▼";
            _worldBeaconText.color = PixelUIKit.Mint;
        }
        else
        {
            _worldBeaconGo.SetActive(false);
        }
    }

    private void UpdateWorldBeaconPosition()
    {
        if (_worldBeaconGo == null || !_worldBeaconGo.activeSelf) return;

        float bob = Mathf.Sin(Time.time * 6f) * 0.22f;
        if (_tutStepIndex == 3)
        {
            var giver = FindAnyObjectByType<PackageGiverNPC>();
            if (giver != null)
            {
                _worldBeaconGo.transform.position = giver.transform.position + new Vector3(0f, 2.1f + bob, 0f);
            }
        }
        else if (_tutStepIndex == 4)
        {
            var startChamber = GameObject.Find("SafeChamber_Start");
            Vector3 doorPos = startChamber != null
                ? startChamber.transform.position + new Vector3(11.2f, 2.6f + bob, 0f)
                : new Vector3(0f, -14.2f + bob, 0f);
            _worldBeaconGo.transform.position = doorPos;
        }
    }

    // -------------------------------------------------------------------------
    // UI Hierarchy Construction
    // -------------------------------------------------------------------------
    private void BuildCanvasAndPanels()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 68; // Above HUD (20) & DialogUI (40), below PauseMenu (95)

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        _rootGroup = gameObject.GetComponent<CanvasGroup>();
        if (_rootGroup == null) _rootGroup = gameObject.AddComponent<CanvasGroup>();

        BuildPrologueUI();
        BuildInteractiveTutorialUI();
    }

    private void BuildPrologueUI()
    {
        _prologueRoot = new GameObject("Prologue_Root", typeof(RectTransform));
        _prologueRoot.transform.SetParent(transform, false);
        StretchFull(_prologueRoot.GetComponent<RectTransform>());

        // Semi-transparent cinematic backdrop (alpha 0.18f) so the live 2D Safe Zone & Player Animation are clearly visible!
        var backdrop = CreatePanel(_prologueRoot.transform, "Backdrop", Vector2.zero, Vector2.one, new Color(0.015f, 0.03f, 0.065f, 0.18f));
        backdrop.raycastTarget = true;
        var backdropBtn = backdrop.gameObject.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(AdvanceOrSkipPrologueSlide);

        // Top Cinema Letterbox Bar
        CreatePanel(_prologueRoot.transform, "TopLetterbox", new Vector2(0f, 0.91f), Vector2.one, new Color(0.01f, 0.015f, 0.035f, 0.96f));
        CreatePanel(_prologueRoot.transform, "TopAccentLine", new Vector2(0f, 0.907f), new Vector2(1f, 0.91f), PixelUIKit.Cyan);

        PixelUIKit.CreatePixelLabel(
            _prologueRoot.transform,
            "TelemetryHeader",
            "PROLOG — TAHUN 2099",
            new Vector2(0.04f, 0.92f),
            new Vector2(0.70f, 0.985f),
            16,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            true);

        // Skip Opening Button in Top-Right
        PixelUIKit.CreateTactileButton(
            _prologueRoot.transform,
            "play",
            "LEWATI",
            "Langsung latihan",
            "TAB",
            new Vector2(0.76f, 0.92f),
            new Vector2(0.96f, 0.985f),
            PixelUIKit.ButtonTheme.SecondarySlate,
            SkipCurrentMode);

        // Top-Center Persona 3 Reload Chapter Banner (0.14..0.86 X, 0.70..0.89 Y)
        // Leaves Y = 0.36..0.70 completely clear so the player sees Unit K-01 animating live in the Safe Zone!
        var theaterCard = PixelUIKit.CreateSteppedPanel(
            _prologueRoot.transform,
            "TheaterViewportCard",
            new Vector2(0.14f, 0.70f),
            new Vector2(0.86f, 0.89f),
            new Color32(8, 16, 30, 240),
            new Color32(0, 240, 255, 255),
            new Color32(6, 10, 18, 255),
            4);
        theaterCard.gameObject.AddComponent<RectMask2D>();
        _slideCardBorder = theaterCard;

        // Animated horizontal scanline sweep inside chapter banner
        var scanline = CreatePanel(theaterCard.transform, "HoloScanline", new Vector2(0f, 0.5f), new Vector2(1f, 0.54f), new Color(0f, 0.95f, 1f, 0.14f));
        _prologueScanlineRt = scanline.rectTransform;

        // Left visual emblem inside chapter banner
        var emblemFrame = PixelUIKit.CreateSteppedPanel(
            theaterCard.transform,
            "VisualEmblemFrame",
            new Vector2(0.03f, 0.12f),
            new Vector2(0.20f, 0.88f),
            new Color32(16, 28, 48, 255),
            new Color32(35, 53, 84, 255),
            new Color32(8, 12, 22, 255),
            3);

        var mainIconGo = new GameObject("SlideMainIcon", typeof(RectTransform), typeof(Image));
        mainIconGo.transform.SetParent(emblemFrame.transform, false);
        var mainIconRt = mainIconGo.GetComponent<RectTransform>();
        mainIconRt.anchorMin = new Vector2(0.12f, 0.12f);
        mainIconRt.anchorMax = new Vector2(0.88f, 0.88f);
        mainIconRt.offsetMin = Vector2.zero;
        mainIconRt.offsetMax = Vector2.zero;
        _slideVisualIcon = mainIconGo.GetComponent<Image>();
        _slideVisualIcon.preserveAspect = true;

        var secIconGo = new GameObject("SlideSecondaryIcon", typeof(RectTransform), typeof(Image));
        secIconGo.transform.SetParent(emblemFrame.transform, false);
        var secIconRt = secIconGo.GetComponent<RectTransform>();
        secIconRt.anchorMin = new Vector2(0.70f, 0.06f);
        secIconRt.anchorMax = new Vector2(0.95f, 0.34f);
        secIconRt.offsetMin = Vector2.zero;
        secIconRt.offsetMax = Vector2.zero;
        _slideSecondaryIcon = secIconGo.GetComponent<Image>();
        _slideSecondaryIcon.preserveAspect = true;

        // Right chapter title & headline inside chapter banner
        _prologueChapterText = PixelUIKit.CreatePixelLabel(
            theaterCard.transform,
            "ChapterTag",
            "BABAK I",
            new Vector2(0.23f, 0.72f),
            new Vector2(0.96f, 0.90f),
            16,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            true);

        _prologueHeadlineText = PixelUIKit.CreatePixelLabel(
            theaterCard.transform,
            "Headline",
            "HEADLINE",
            new Vector2(0.23f, 0.38f),
            new Vector2(0.96f, 0.72f),
            28,
            Color.white,
            TextAnchor.MiddleLeft,
            false);

        _prologueSubheadText = PixelUIKit.CreatePixelLabel(
            theaterCard.transform,
            "Subheadline",
            "SUBHEADLINE",
            new Vector2(0.23f, 0.16f),
            new Vector2(0.75f, 0.38f),
            14,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            true);

        // Slide Progress Pips (4 chapters) on bottom-right of chapter banner
        for (int i = 0; i < 4; i++)
        {
            float x0 = 0.77f + i * 0.048f;
            _prologuePips[i] = CreatePanel(theaterCard.transform, "SlidePip_" + i, new Vector2(x0, 0.20f), new Vector2(x0 + 0.038f, 0.30f), PixelUIKit.BorderSlate);
        }

        // Live 2D Stage Telemetry Pill floating just above the Story Dialogue Deck
        var stagePill = PixelUIKit.CreateSteppedPanel(
            _prologueRoot.transform,
            "LiveStageCaptionPill",
            new Vector2(0.14f, 0.355f),
            new Vector2(0.58f, 0.398f),
            new Color32(6, 20, 34, 235),
            new Color32(0, 240, 255, 255),
            new Color32(0, 0, 0, 0),
            2);
        stagePill.raycastTarget = false;
        _prologueStageCaptionText = PixelUIKit.CreatePixelLabel(
            stagePill.transform,
            "StageCaptionText",
            "Prolog 1/4  •  Bunker Safe Zone",
            new Vector2(0.04f, 0.08f),
            new Vector2(0.96f, 0.92f),
            12,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            true);

        // Bottom Story Dialogue Deck (0.14..0.86 X, 0.04..0.35 Y) with RectMask2D so text never overflows
        var dialogDeck = PixelUIKit.CreateSteppedPanel(
            _prologueRoot.transform,
            "StoryDialogDeck",
            new Vector2(0.14f, 0.04f),
            new Vector2(0.86f, 0.35f),
            new Color32(10, 18, 34, 246),
            new Color32(0, 225, 255, 255),
            new Color32(6, 10, 18, 255),
            5);
        dialogDeck.gameObject.AddComponent<RectMask2D>();

        // Click anywhere on dialog deck to advance
        var deckBtn = dialogDeck.gameObject.AddComponent<Button>();
        deckBtn.transition = Selectable.Transition.None;
        deckBtn.onClick.AddListener(AdvanceOrSkipPrologueSlide);

        // Operator Portrait Box inside Dialog Deck
        var portraitBox = PixelUIKit.CreateSteppedPanel(
            dialogDeck.transform,
            "PortraitBox",
            new Vector2(0.03f, 0.14f),
            new Vector2(0.19f, 0.86f),
            new Color32(16, 30, 52, 255),
            new Color32(0, 240, 255, 255),
            new Color32(8, 14, 24, 255),
            3);

        var portGo = new GameObject("SpeakerPortrait", typeof(RectTransform), typeof(Image));
        portGo.transform.SetParent(portraitBox.transform, false);
        var portRt = portGo.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0.08f, 0.08f);
        portRt.anchorMax = new Vector2(0.92f, 0.92f);
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;
        _prologuePortraitImg = portGo.GetComponent<Image>();
        _prologuePortraitImg.sprite = GetOperatorPortraitSprite();
        _prologuePortraitImg.preserveAspect = true;

        _prologueSpeakerText = PixelUIKit.CreatePixelLabel(
            dialogDeck.transform,
            "SpeakerName",
            "ARIA-7",
            new Vector2(0.22f, 0.74f),
            new Vector2(0.96f, 0.91f),
            20,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            false);

        _prologueBodyText = PixelUIKit.CreatePixelLabel(
            dialogDeck.transform,
            "BodyText",
            "",
            new Vector2(0.22f, 0.24f),
            new Vector2(0.95f, 0.72f),
            18,
            PixelUIKit.TextWhite,
            TextAnchor.UpperLeft,
            true);
        _prologueBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _prologueBodyText.verticalOverflow = VerticalWrapMode.Truncate;
        _prologueBodyText.resizeTextForBestFit = true;
        _prologueBodyText.resizeTextMinSize = 12;
        _prologueBodyText.resizeTextMaxSize = 18;
        _prologueBodyText.lineSpacing = 1.16f;

        _prologueContinueText = PixelUIKit.CreatePixelLabel(
            dialogDeck.transform,
            "ContinuePrompt",
            "[SPACE / ENTER / KLIK] LANJUT >>",
            new Vector2(0.45f, 0.06f),
            new Vector2(0.96f, 0.22f),
            15,
            PixelUIKit.Mint,
            TextAnchor.MiddleRight,
            false);
    }

    private void BuildInteractiveTutorialUI()
    {
        _tutorialRoot = new GameObject("InteractiveTutorial_Root", typeof(RectTransform));
        _tutorialRoot.transform.SetParent(transform, false);
        StretchFull(_tutorialRoot.GetComponent<RectTransform>());

        // Compact Top-Center Tactical Comms Card (non-raycast-blocking) with RectMask2D so dialogue never overflows outside
        var card = PixelUIKit.CreateSteppedPanel(
            _tutorialRoot.transform,
            "TutorialCommsCard",
            new Vector2(0.20f, 0.72f),
            new Vector2(0.80f, 0.94f),
            new Color32(10, 18, 32, 238),
            new Color32(0, 240, 255, 255),
            new Color32(6, 12, 22, 255),
            4);
        card.raycastTarget = false;
        card.gameObject.AddComponent<RectMask2D>();

        // Operator Portrait on Left
        var portBox = PixelUIKit.CreateSteppedPanel(
            card.transform,
            "OperatorHoloBox",
            new Vector2(0.02f, 0.14f),
            new Vector2(0.15f, 0.88f),
            new Color32(16, 30, 52, 255),
            new Color32(54, 245, 155, 255),
            new Color32(8, 14, 24, 255),
            3);

        var portGo = new GameObject("OperatorAvatar", typeof(RectTransform), typeof(Image));
        portGo.transform.SetParent(portBox.transform, false);
        var portRt = portGo.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0.08f, 0.08f);
        portRt.anchorMax = new Vector2(0.92f, 0.92f);
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;
        _tutPortraitImg = portGo.GetComponent<Image>();
        _tutPortraitImg.sprite = GetOperatorPortraitSprite();
        _tutPortraitImg.preserveAspect = true;

        // 5 Step Pips in top-right of card
        for (int i = 0; i < 5; i++)
        {
            float x0 = 0.62f + i * 0.042f;
            _tutStepPips[i] = CreatePanel(card.transform, "TutPip_" + i, new Vector2(x0, 0.82f), new Vector2(x0 + 0.034f, 0.90f), PixelUIKit.BorderSlate);
        }

        // Skip Tutorial small button
        var skipBtnGo = PixelUIKit.CreateSteppedPanel(
            card.transform,
            "SkipTutBtn",
            new Vector2(0.845f, 0.76f),
            new Vector2(0.985f, 0.94f),
            new Color32(22, 34, 56, 255),
            new Color32(80, 110, 150, 255),
            new Color32(10, 16, 28, 255),
            2);
        var skipBtn = skipBtnGo.gameObject.AddComponent<Button>();
        skipBtn.onClick.AddListener(SkipCurrentMode);
        PixelUIKit.CreatePixelLabel(skipBtnGo.transform, "Lbl", "[TAB] LEWATI", Vector2.zero, Vector2.one, 12, PixelUIKit.TextMuted, TextAnchor.MiddleCenter, true);

        _tutStepTagText = PixelUIKit.CreatePixelLabel(
            card.transform,
            "StepTag",
            "LATIHAN 1 / 5",
            new Vector2(0.17f, 0.76f),
            new Vector2(0.61f, 0.93f),
            14,
            PixelUIKit.Cyan,
            TextAnchor.MiddleLeft,
            true);

        _tutTitleText = PixelUIKit.CreatePixelLabel(
            card.transform,
            "StepTitle",
            "GERAK & DOUBLE JUMP",
            new Vector2(0.17f, 0.56f),
            new Vector2(0.83f, 0.77f),
            22,
            Color.white,
            TextAnchor.MiddleLeft,
            false);

        _tutDialogueText = PixelUIKit.CreatePixelLabel(
            card.transform,
            "StepDialogue",
            "",
            new Vector2(0.17f, 0.26f),
            new Vector2(0.96f, 0.56f),
            15,
            PixelUIKit.TextWhite,
            TextAnchor.MiddleLeft,
            true);
        _tutDialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _tutDialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        _tutDialogueText.resizeTextForBestFit = true;
        _tutDialogueText.resizeTextMinSize = 11;
        _tutDialogueText.resizeTextMaxSize = 15;
        _tutDialogueText.lineSpacing = 1.15f;

        // Keycap highlight bar at bottom of card
        var keyBadge = CreatePanel(card.transform, "KeyBadgeBg", new Vector2(0.17f, 0.07f), new Vector2(0.64f, 0.23f), new Color(0f, 0.94f, 1f, 0.14f));
        _tutKeyCapText = PixelUIKit.CreatePixelLabel(
            keyBadge.transform,
            "KeyCapText",
            "[A / D] GERAK  +  [SPACE 2x] JUMP",
            new Vector2(0.03f, 0f),
            new Vector2(0.97f, 1f),
            14,
            PixelUIKit.Mint,
            TextAnchor.MiddleLeft,
            false);

        // Progress Bar on bottom right
        var progBg = CreatePanel(card.transform, "ProgBg", new Vector2(0.66f, 0.09f), new Vector2(0.97f, 0.21f), new Color(0.05f, 0.09f, 0.16f, 1f));
        var progFill = CreatePanel(progBg.transform, "ProgFill", Vector2.zero, new Vector2(0.1f, 1f), PixelUIKit.Mint);
        _tutProgressFillRt = progFill.rectTransform;

        _tutObjectiveText = PixelUIKit.CreatePixelLabel(
            progBg.transform,
            "ObjText",
            "PROGRES LATIHAN",
            new Vector2(0.04f, 0f),
            new Vector2(0.96f, 1f),
            11,
            Color.white,
            TextAnchor.MiddleCenter,
            true);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // -------------------------------------------------------------------------
    // Procedural Pixel Portraits & Diagrams
    // -------------------------------------------------------------------------
    public static Sprite GetOperatorPortraitSprite()
    {
        if (_operatorPortraitSpr != null) return _operatorPortraitSpr;

        const int w = 32;
        const int h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0.05f, 0.10f, 0.18f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, (y % 2 == 0) ? clear : new Color(0.07f, 0.13f, 0.22f, 1f));

        void Fill(int x0, int y0, int rw, int rh, Color c)
        {
            for (int y = y0; y < y0 + rh; y++)
                for (int x = x0; x < x0 + rw; x++)
                    if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
        }

        Color hairCyan = new Color(0.12f, 0.85f, 0.96f, 1f);
        Color hairDark = new Color(0.06f, 0.42f, 0.62f, 1f);
        Color skin = new Color(0.92f, 0.84f, 0.80f, 1f);
        Color visor = new Color(0.22f, 1.0f, 0.68f, 1f);
        Color jacket = new Color(0.14f, 0.22f, 0.36f, 1f);
        Color collar = new Color(1.0f, 0.55f, 0.18f, 1f);

        // Shoulders & Jacket
        Fill(5, 0, 22, 9, jacket);
        Fill(9, 6, 14, 3, collar);
        // Neck & Face
        Fill(13, 9, 6, 3, skin);
        Fill(10, 11, 12, 12, skin);
        // Cyber Hair & Headset
        Fill(8, 18, 16, 8, hairCyan);
        Fill(7, 13, 3, 10, hairDark);
        Fill(22, 13, 3, 10, hairDark);
        // Glowing Holo Visor & Mic
        Fill(10, 16, 12, 3, new Color(0.05f, 0.12f, 0.20f, 1f));
        Fill(11, 17, 10, 2, visor);
        Fill(7, 14, 2, 5, collar);
        Fill(23, 14, 2, 5, collar);

        tex.Apply();
        _operatorPortraitSpr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return _operatorPortraitSpr;
    }

    private static Sprite GetArchivePortraitSprite()
    {
        if (_archivePortraitSpr != null) return _archivePortraitSpr;
        _archivePortraitSpr = PixelUIKit.GetIcon("terminal");
        return _archivePortraitSpr;
    }

    private static Sprite GetTTLDiagramSprite()
    {
        if (_ttlDiagramSpr != null) return _ttlDiagramSpr;

        const int w = 48;
        const int h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));

        // Draw 6 glowing battery cells
        for (int c = 0; c < 6; c++)
        {
            int x0 = 4 + c * 7;
            Color cellCol = c < 4 ? new Color(0f, 0.94f, 1f, 1f) : new Color(1f, 0.62f, 0.2f, 1f);
            for (int y = 8; y < 24; y++)
                for (int x = x0; x < x0 + 5; x++)
                    tex.SetPixel(x, y, cellCol);
        }
        tex.Apply();
        _ttlDiagramSpr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return _ttlDiagramSpr;
    }
}
