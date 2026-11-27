using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Serializable configuration for an individual animation state (e.g. Idle, Walk, Attack).
/// Supports drag-and-drop Sprite arrays, custom FPS, loop flags, frame triggers, and positional offsets.
/// </summary>
[System.Serializable]
public class SpriteAnimationState
{
    [Tooltip("Unique name of this animation state (e.g. Idle, Walk, Attack, Hurt, Death).")]
    public string stateName = "Idle";

    [Tooltip("Ordered frames of this animation state.")]
    public Sprite[] frames = new Sprite[0];

    [Tooltip("Frames played per second.")]
    [Range(1f, 60f)]
    public float framesPerSecond = 10f;

    [Tooltip("Whether this animation should loop indefinitely or stop on the last frame.")]
    public bool loop = true;

    [Tooltip("Optional frame index (0-based) where a special event fires (e.g. attack hitbox release). -1 disables.")]
    public int triggerFrame = -1;

    [Tooltip("Custom identifier or payload string passed to the onFrameTrigger event.")]
    public string eventTag = "";

    [Tooltip("Optional local position offset applied to the sprite renderer during this animation.")]
    public Vector2 localOffset = Vector2.zero;

    [Tooltip("Base color tint applied to the renderer for this state.")]
    public Color stateTint = Color.white;

    [Tooltip("If true, animation updates using Time.unscaledDeltaTime (continues during hitstop/pause).")]
    public bool unscaledTime = false;

    public int FrameCount => frames != null ? frames.Length : 0;
    public float Duration => (frames != null && frames.Length > 0 && framesPerSecond > 0f) ? frames.Length / framesPerSecond : 0f;
    public bool IsValid => HasValidFrames;

    public bool HasValidFrames
    {
        get
        {
            if (frames == null || frames.Length == 0) return false;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) return true;
            }
            return false;
        }
    }

    public Sprite GetFirstValidFrame()
    {
        if (frames == null) return null;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null) return frames[i];
        }
        return null;
    }

    public SpriteAnimationState Clone()
    {
        return new SpriteAnimationState
        {
            stateName = this.stateName,
            frames = this.frames != null ? (Sprite[])this.frames.Clone() : new Sprite[0],
            framesPerSecond = this.framesPerSecond,
            loop = this.loop,
            triggerFrame = this.triggerFrame,
            eventTag = this.eventTag,
            localOffset = this.localOffset,
            stateTint = this.stateTint,
            unscaledTime = this.unscaledTime
        };
    }
}

/// <summary>
/// Modular, Inspector-friendly Sprite Animator component for Bosses, Enemies, and custom entities.
/// Allows creators to drop in custom sprite frames, configure states, handle facing/flips,
/// trigger events on specific frames, and manage hurt flashes without requiring complex Mecanim controllers.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("PacketCourier/Animation/Modular Sprite Animator")]
public class ModularSpriteAnimator : MonoBehaviour
{
    [Header("Profile Asset (Opsional — Drag Asset Profile ke Sini)")]
    [Tooltip("Opsional: Drag SpriteAnimationProfile dari Assets/Resources/CustomAnimations/ atau buat baru. Jika dikosongkan, isi langsung Sprite & State di bawah.")]
    public SpriteAnimationProfile profile;

    [Header("Sprite Tunggal (Opsional / Fallback)")]
    [Tooltip("Jika hanya ingin ganti 1 sprite diam tanpa animasi, cukup drag sprite ke sini.")]
    public Sprite defaultSprite;

    [Header("Renderer Target")]
    [Tooltip("Target SpriteRenderer. If left empty, automatically finds one on this GameObject or children.")]
    public SpriteRenderer targetRenderer;

    [Header("Default Playback & Visual")]
    [Tooltip("State to play automatically on Start.")]
    public string defaultState = "Idle";

    [Tooltip("Whether to automatically begin playing the default state when enabled.")]
    public bool playOnStart = true;

    [Tooltip("Inverts the facing direction logic if original sprites face Left by default.")]
    public bool invertFacing = false;

    [Tooltip("Aktifkan jika parent transform (seperti Boss_Grounded VisualRoot) sudah membalik localScale.x agar tidak double-flip.")]
    public bool useExternalFacing = false;

    [Tooltip("Pengali ukuran gambar visual (tanpa mengubah collider/hitbox).")]
    [Range(0.25f, 4.0f)]
    public float visualScaleMultiplier = 1.0f;

    [Tooltip("Geser posisi gambar (X, Y) terhadap titik tengah/kaki.")]
    public Vector2 visualOffset = Vector2.zero;

    [Tooltip("Global playback speed multiplier.")]
    [Range(0.1f, 5f)]
    public float playbackSpeed = 1.0f;

    [Tooltip("Jika aktif, stateTint per state akan menimpa warna SpriteRenderer.")]
    public bool applyStateTint = false;

    [Header("Animation States")]
    [Tooltip("List of animation states with their respective sprite frames.")]
    public List<SpriteAnimationState> states = new List<SpriteAnimationState>();

    [Header("Animation Events")]
    public UnityEvent<string> onAnimationStarted;
    public UnityEvent<string> onAnimationCompleted;
    public UnityEvent<string> onFrameTrigger;
    public UnityEvent<int> onFrameChanged;

    // Direct C# events for performance-critical systems
    public event Action<string> OnStateChanged;
    public event Action<string> OnStateFinished;
    public event Action<string, int> OnFrameTriggered;

    // Runtime state
    private Dictionary<string, SpriteAnimationState> _stateMap = new Dictionary<string, SpriteAnimationState>(StringComparer.OrdinalIgnoreCase);
    private SpriteAnimationState _currentState;
    private int _currentFrameIndex = 0;
    private float _frameTimer = 0f;
    private bool _isPlaying = false;
    private bool _isPaused = false;
    private bool _hasTriggeredThisCycle = false;
    private int _facingDirection = 1; // 1 = Right, -1 = Left
    private Action _oneShotCallback;
    private Vector3 _originalLocalPosition = Vector3.zero;
    private bool _capturedOriginalPos = false;
    private bool _profileApplied = false;

    // Juice & Effects
    private Coroutine _flashCoroutine;
    private Coroutine _squashCoroutine;
    private Color _baseColor = Color.white;
    private bool _isFlashing = false;

    public string CurrentStateName => _currentState != null ? _currentState.stateName : string.Empty;
    public int CurrentFrameIndex => _currentFrameIndex;
    public bool IsPlaying => _isPlaying && !_isPaused;
    public bool IsPaused => _isPaused;
    public bool IsComplete => _currentState != null && !_currentState.loop && _currentFrameIndex >= _currentState.FrameCount - 1;
    public int FacingDirection => _facingDirection;

    private void Awake()
    {
        EnsureRenderer();
        CaptureOriginalLocalPosition();
        if (profile != null && !_profileApplied)
        {
            ApplyProfile(profile, overwriteExisting: false);
        }
        BuildStateMap();
        ApplyFallbackSpriteIfIdle();
    }

    private void Start()
    {
        EnsureRenderer();
        CaptureOriginalLocalPosition();
        if (profile != null && !_profileApplied)
        {
            ApplyProfile(profile, overwriteExisting: false);
        }
        BuildStateMap();

        if (playOnStart && !string.IsNullOrEmpty(defaultState))
        {
            if (!Play(defaultState))
            {
                PlayFirstValidStateOrFallback();
            }
        }
        else
        {
            ApplyFallbackSpriteIfIdle();
        }
    }

    private void CaptureOriginalLocalPosition()
    {
        if (targetRenderer != null && !_capturedOriginalPos)
        {
            _originalLocalPosition = targetRenderer.transform.localPosition;
            _baseColor = targetRenderer.color;
            _capturedOriginalPos = true;
        }
    }

    /// <summary>
    /// Applies a reusable SpriteAnimationProfile asset to this animator.
    /// Inline states/sprites already filled on the component take priority unless overwriteExisting is true.
    /// </summary>
    public void ApplyProfile(SpriteAnimationProfile sourceProfile, bool overwriteExisting = false)
    {
        if (sourceProfile == null) return;
        profile = sourceProfile;
        _profileApplied = true;

        if (overwriteExisting || defaultSprite == null)
            defaultSprite = sourceProfile.defaultSprite;

        if (overwriteExisting || !invertFacing)
            invertFacing = sourceProfile.invertFacing;

        if (overwriteExisting || Mathf.Approximately(visualScaleMultiplier, 1f))
            visualScaleMultiplier = Mathf.Clamp(sourceProfile.visualScaleMultiplier, 0.25f, 4f);

        if (overwriteExisting || visualOffset == Vector2.zero)
            visualOffset = sourceProfile.visualOffset;

        if (overwriteExisting || Mathf.Approximately(playbackSpeed, 1f))
            playbackSpeed = Mathf.Clamp(sourceProfile.playbackSpeed, 0.1f, 5f);

        if (overwriteExisting || !applyStateTint)
            applyStateTint = sourceProfile.applyStateTint;

        if (sourceProfile.states != null && sourceProfile.states.Count > 0)
        {
            if (states == null) states = new List<SpriteAnimationState>();
            if (overwriteExisting) states.Clear();

            for (int i = 0; i < sourceProfile.states.Count; i++)
            {
                var pState = sourceProfile.states[i];
                if (pState == null || string.IsNullOrEmpty(pState.stateName)) continue;

                int existingIdx = states.FindIndex(s => s != null && string.Equals(s.stateName, pState.stateName, StringComparison.OrdinalIgnoreCase));
                if (existingIdx >= 0)
                {
                    // Replace only if inline state has no frames yet
                    if (!states[existingIdx].HasValidFrames && pState.HasValidFrames)
                    {
                        states[existingIdx] = pState.Clone();
                    }
                }
                else
                {
                    states.Add(pState.Clone());
                }
            }
        }

        BuildStateMap();
        ApplyFallbackSpriteIfIdle();
        UpdateRendererFlip();
    }

    /// <summary>
    /// Returns true when this component (or its assigned profile) has at least 1 custom sprite assigned.
    /// </summary>
    public bool HasAnyCustomArt()
    {
        if (defaultSprite != null) return true;
        if (states != null)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] != null && states[i].HasValidFrames) return true;
            }
        }
        return profile != null && profile.HasAnyCustomArt();
    }

    /// <summary>
    /// Returns the best fallback sprite if no animated state can play.
    /// </summary>
    public Sprite GetFallbackSprite()
    {
        if (defaultSprite != null) return defaultSprite;

        var idle = GetState("Idle");
        if (idle != null && idle.HasValidFrames) return idle.GetFirstValidFrame();

        var walk = GetState("Walk");
        if (walk != null && walk.HasValidFrames) return walk.GetFirstValidFrame();

        if (states != null)
        {
            for (int i = 0; i < states.Count; i++)
            {
                Sprite s = states[i]?.GetFirstValidFrame();
                if (s != null) return s;
            }
        }
        return profile != null ? profile.GetFirstAvailableSprite() : null;
    }

    private void ApplyFallbackSpriteIfIdle()
    {
        if (_currentState != null && _currentState.HasValidFrames) return;
        EnsureRenderer();
        if (targetRenderer == null) return;
        Sprite fallback = GetFallbackSprite();
        if (fallback != null)
        {
            targetRenderer.sprite = fallback;
            ApplyVisualOffsetOnly();
        }
    }

    private void PlayFirstValidStateOrFallback()
    {
        if (HasValidState("Idle") && Play("Idle")) return;
        if (HasValidState("Walk") && Play("Walk")) return;
        if (states != null)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] != null && states[i].HasValidFrames)
                {
                    Play(states[i].stateName);
                    return;
                }
            }
        }
        ApplyFallbackSpriteIfIdle();
    }

    private void ApplyVisualOffsetOnly()
    {
        if (targetRenderer == null || !_capturedOriginalPos) return;
        if (visualOffset != Vector2.zero)
        {
            float flipSign = useExternalFacing ? 1f : _facingDirection;
            targetRenderer.transform.localPosition = _originalLocalPosition + new Vector3(visualOffset.x * flipSign, visualOffset.y, 0f);
        }
        else
        {
            targetRenderer.transform.localPosition = _originalLocalPosition;
        }
    }

    private void OnEnable()
    {
        if (_isPlaying && _currentState != null && targetRenderer != null)
        {
            ApplyCurrentFrame();
        }
    }

    private void Update()
    {
        if (!_isPlaying || _isPaused || _currentState == null || _currentState.FrameCount == 0)
        {
            return;
        }

        float dt = (_currentState.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * Mathf.Max(0.01f, playbackSpeed);
        _frameTimer += dt;

        float frameInterval = 1f / Mathf.Max(1f, _currentState.framesPerSecond);
        if (_frameTimer >= frameInterval)
        {
            AdvanceFrame(frameInterval);
        }
    }

    private void AdvanceFrame(float frameInterval)
    {
        while (_frameTimer >= frameInterval)
        {
            _frameTimer -= frameInterval;

            int nextFrame = _currentFrameIndex + 1;
            int totalFrames = _currentState.FrameCount;

            if (nextFrame >= totalFrames)
            {
                if (_currentState.loop)
                {
                    _currentFrameIndex = 0;
                    _hasTriggeredThisCycle = false;
                }
                else
                {
                    _currentFrameIndex = totalFrames - 1;
                    _isPlaying = false;
                    ApplyCurrentFrame();

                    // Fire completion events
                    string finishedState = _currentState.stateName;
                    onAnimationCompleted?.Invoke(finishedState);
                    OnStateFinished?.Invoke(finishedState);

                    Action cb = _oneShotCallback;
                    _oneShotCallback = null;
                    cb?.Invoke();
                    return;
                }
            }
            else
            {
                _currentFrameIndex = nextFrame;
            }

            CheckTriggerFrame();
            ApplyCurrentFrame();

            onFrameChanged?.Invoke(_currentFrameIndex);
        }
    }

    private void CheckTriggerFrame()
    {
        if (_currentState != null && _currentState.triggerFrame >= 0 && _currentFrameIndex == _currentState.triggerFrame)
        {
            if (!_hasTriggeredThisCycle)
            {
                _hasTriggeredThisCycle = true;
                string tag = !string.IsNullOrEmpty(_currentState.eventTag) ? _currentState.eventTag : _currentState.stateName;
                onFrameTrigger?.Invoke(tag);
                OnFrameTriggered?.Invoke(tag, _currentFrameIndex);
            }
        }
    }

    private void ApplyCurrentFrame()
    {
        if (targetRenderer == null || _currentState == null || _currentState.frames == null)
        {
            return;
        }

        if (_currentFrameIndex >= 0 && _currentFrameIndex < _currentState.frames.Length)
        {
            Sprite s = _currentState.frames[_currentFrameIndex];
            if (s != null)
            {
                targetRenderer.sprite = s;
            }
        }

        // Apply combined profile visualOffset + per-state localOffset
        Vector2 combinedOffset = visualOffset + _currentState.localOffset;
        if (combinedOffset != Vector2.zero && _capturedOriginalPos)
        {
            float flipSign = useExternalFacing ? 1f : _facingDirection;
            Vector3 offset = new Vector3(
                combinedOffset.x * flipSign,
                combinedOffset.y,
                0f
            );
            targetRenderer.transform.localPosition = _originalLocalPosition + offset;
        }
        else if (_capturedOriginalPos)
        {
            targetRenderer.transform.localPosition = _originalLocalPosition;
        }

        // Apply color tint only when explicitly enabled so external hit-flash/telegraph tints remain clean
        if (applyStateTint && !_isFlashing)
        {
            targetRenderer.color = _currentState.stateTint;
        }
    }

    /// <summary>
    /// Play a registered animation state by name.
    /// Returns true if the state exists AND has at least one non-null frame; false otherwise.
    /// </summary>
    public bool Play(string stateName, bool forceRestart = false)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        EnsureRenderer();
        CaptureOriginalLocalPosition();
        BuildStateMap();

        if (!_stateMap.TryGetValue(stateName, out SpriteAnimationState targetState) || targetState == null || !targetState.HasValidFrames)
        {
            return false;
        }

        if (_currentState == targetState && _isPlaying && !forceRestart)
        {
            return true;
        }

        _currentState = targetState;
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _isPlaying = true;
        _isPaused = false;
        _hasTriggeredThisCycle = false;
        _oneShotCallback = null;

        CheckTriggerFrame();
        ApplyCurrentFrame();

        onAnimationStarted?.Invoke(stateName);
        OnStateChanged?.Invoke(stateName);
        onFrameChanged?.Invoke(0);

        return true;
    }

    /// <summary>
    /// Plays the best available animation state, supporting optional boss phase suffixes (e.g. Slash_P2, Attack_P3),
    /// ordered fallback state names, and finally falling back to Idle / Walk / defaultSprite so empty slots never break visuals.
    /// </summary>
    public bool PlaySmart(string preferredState, int phase = 1, params string[] fallbacks)
    {
        EnsureRenderer();
        BuildStateMap();

        if (!string.IsNullOrEmpty(preferredState))
        {
            if (phase > 1 && HasValidState($"{preferredState}_P{phase}"))
                return Play($"{preferredState}_P{phase}");
            if (HasValidState(preferredState))
                return Play(preferredState);
        }

        if (fallbacks != null)
        {
            for (int i = 0; i < fallbacks.Length; i++)
            {
                string fb = fallbacks[i];
                if (string.IsNullOrEmpty(fb)) continue;
                if (phase > 1 && HasValidState($"{fb}_P{phase}"))
                    return Play($"{fb}_P{phase}");
                if (HasValidState(fb))
                    return Play(fb);
            }
        }

        if (phase > 1 && HasValidState($"Idle_P{phase}")) return Play($"Idle_P{phase}");
        if (HasValidState("Idle")) return Play("Idle");
        if (HasValidState("Walk")) return Play("Walk");

        ApplyFallbackSpriteIfIdle();
        return false;
    }

    /// <summary>
    /// Plays an animation state once, calling onComplete when the final frame ends.
    /// </summary>
    public bool PlayOnce(string stateName, Action onComplete = null)
    {
        bool started = Play(stateName, true);
        if (started && _currentState != null)
        {
            _currentState.loop = false;
            _oneShotCallback = onComplete;
        }
        return started;
    }

    /// <summary>
    /// Plays a dynamic custom list of sprites on the fly without pre-registering.
    /// </summary>
    public void PlayCustom(string tempName, Sprite[] customFrames, float fps = 10f, bool loop = false, Action onComplete = null)
    {
        if (customFrames == null || customFrames.Length == 0)
        {
            return;
        }

        EnsureRenderer();

        SpriteAnimationState dynamicState = new SpriteAnimationState
        {
            stateName = string.IsNullOrEmpty(tempName) ? "Custom" : tempName,
            frames = customFrames,
            framesPerSecond = fps,
            loop = loop,
            triggerFrame = -1,
            stateTint = Color.white
        };

        _currentState = dynamicState;
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _isPlaying = true;
        _isPaused = false;
        _hasTriggeredThisCycle = false;
        _oneShotCallback = onComplete;

        ApplyCurrentFrame();

        onAnimationStarted?.Invoke(dynamicState.stateName);
        OnStateChanged?.Invoke(dynamicState.stateName);
        onFrameChanged?.Invoke(0);
    }

    /// <summary>
    /// Pauses animation playback on current frame.
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
    }

    /// <summary>
    /// Resumes animation playback.
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
    }

    /// <summary>
    /// Stops playback and resets state.
    /// </summary>
    public void Stop()
    {
        _isPlaying = false;
        _isPaused = false;
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _oneShotCallback = null;
    }

    /// <summary>
    /// Sets facing direction and adjusts flipX accordingly.
    /// 1 = Right, -1 = Left.
    /// </summary>
    public void SetFacing(int direction)
    {
        if (direction == 0) return;
        _facingDirection = direction > 0 ? 1 : -1;
        UpdateRendererFlip();
    }

    /// <summary>
    /// Sets facing direction from float input (e.g. velocity.x).
    /// </summary>
    public void SetFacing(float direction)
    {
        if (Mathf.Abs(direction) < 0.001f) return;
        SetFacing(direction > 0f ? 1 : -1);
    }

    /// <summary>
    /// Directly sets flipX state.
    /// </summary>
    public void SetFlipX(bool flip)
    {
        _facingDirection = flip ? -1 : 1;
        UpdateRendererFlip();
    }

    private void UpdateRendererFlip()
    {
        if (targetRenderer != null)
        {
            if (useExternalFacing)
            {
                // Parent transform already flips localScale.x; only apply invertFacing here to avoid double-flip!
                targetRenderer.flipX = invertFacing;
                return;
            }
            bool flip = _facingDirection < 0;
            if (invertFacing) flip = !flip;
            targetRenderer.flipX = flip;
        }
    }

    /// <summary>
    /// Sets global playback speed.
    /// </summary>
    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Max(0.01f, speed);
    }

    /// <summary>
    /// Checks if a named state exists in the configuration (even if its frames are still empty).
    /// </summary>
    public bool HasState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        BuildStateMap();
        return _stateMap.ContainsKey(stateName);
    }

    /// <summary>
    /// Checks if a named state exists AND has at least one non-null sprite frame assigned.
    /// </summary>
    public bool HasValidState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        BuildStateMap();
        return _stateMap.TryGetValue(stateName, out SpriteAnimationState s) && s != null && s.HasValidFrames;
    }

    /// <summary>
    /// Retrieves the SpriteAnimationState by name, or null if not found.
    /// </summary>
    public SpriteAnimationState GetState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return null;
        BuildStateMap();
        _stateMap.TryGetValue(stateName, out SpriteAnimationState s);
        return s;
    }

    /// <summary>
    /// Adds or replaces an animation state at runtime.
    /// </summary>
    public void AddState(SpriteAnimationState newState)
    {
        if (newState == null || string.IsNullOrEmpty(newState.stateName)) return;

        // Remove existing state with same name if present
        states.RemoveAll(s => string.Equals(s.stateName, newState.stateName, StringComparison.OrdinalIgnoreCase));
        states.Add(newState);
        BuildStateMap();
    }

    /// <summary>
    /// Quick helper to add a state with frames, FPS, and loop setting.
    /// </summary>
    public void AddState(string name, Sprite[] frames, float fps = 10f, bool loop = true)
    {
        AddState(new SpriteAnimationState
        {
            stateName = name,
            frames = frames,
            framesPerSecond = fps,
            loop = loop
        });
    }

    /// <summary>
    /// Checks whether a specific state is currently playing.
    /// </summary>
    public bool IsPlayingState(string stateName)
    {
        return _isPlaying && _currentState != null && string.Equals(_currentState.stateName, stateName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Triggers a brief flash tint (e.g. hurt red flash) without overriding animation frames.
    /// </summary>
    public void FlashColor(Color color, float duration = 0.12f)
    {
        if (!gameObject.activeInHierarchy || targetRenderer == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        _isFlashing = true;
        targetRenderer.color = color;
        yield return new WaitForSeconds(Mathf.Max(0.02f, duration));
        _isFlashing = false;
        if (_currentState != null)
        {
            targetRenderer.color = _currentState.stateTint;
        }
        else
        {
            targetRenderer.color = _baseColor;
        }
        _flashCoroutine = null;
    }

    /// <summary>
    /// Triggers a temporary squash & stretch animation effect on the renderer.
    /// </summary>
    public void ApplySquashAndStretch(Vector2 factor, float duration = 0.15f)
    {
        if (!gameObject.activeInHierarchy || targetRenderer == null) return;
        if (_squashCoroutine != null) StopCoroutine(_squashCoroutine);
        _squashCoroutine = StartCoroutine(SquashRoutine(factor, duration));
    }

    private IEnumerator SquashRoutine(Vector2 factor, float duration)
    {
        Transform t = targetRenderer.transform;
        Vector3 baseScale = Vector3.one;
        Vector3 squashed = new Vector3(baseScale.x * factor.x, baseScale.y * factor.y, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            t.localScale = Vector3.Lerp(squashed, baseScale, p);
            yield return null;
        }
        t.localScale = baseScale;
        _squashCoroutine = null;
    }

    public void EnsureRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
    }

    public void BuildStateMap()
    {
        if (_stateMap == null)
        {
            _stateMap = new Dictionary<string, SpriteAnimationState>(StringComparer.OrdinalIgnoreCase);
        }
        _stateMap.Clear();

        if (states == null) return;

        foreach (var s in states)
        {
            if (s != null && !string.IsNullOrEmpty(s.stateName))
            {
                _stateMap[s.stateName] = s;
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor preview utility for scrubbing frames in Scene/Inspector without entering Play mode.
    /// </summary>
    public void EditorPreviewFrame(string stateName, int frameIndex)
    {
        EnsureRenderer();
        if (targetRenderer == null) return;

        SpriteAnimationState s = GetState(stateName);
        if (s != null && s.frames != null && s.frames.Length > 0)
        {
            int idx = Mathf.Clamp(frameIndex, 0, s.frames.Length - 1);
            if (s.frames[idx] != null)
            {
                targetRenderer.sprite = s.frames[idx];
                UnityEditor.EditorUtility.SetDirty(targetRenderer);
            }
        }
    }
#endif
}
