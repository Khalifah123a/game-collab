using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central audio manager for Packet Courier.
/// Handles SFX for dialogue, interaction, jump, dash, combat, damage, collectibles, and UI.
/// Manages dynamic background music (BGM) with adaptive crossfading and low-pass muffled filter.
/// Survives scene transitions and supports organic pitch variation and procedural fallback.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum BGMTrack
    {
        None,
        Menu,
        Tutorial,
        SafeZone,
        Traversal,
        World1,
        World2,
        World3,
        World4,
        World5,
        Boss
    }

    public static AudioManager Instance { get; private set; }

    public static float MasterVolume = 1.0f;
    public static float BGMVolume = 0.70f;
    public static float SFXVolume = 0.85f;

    private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
    private AudioSource _sfxSource;
    private AudioSource _dialogueSource;
    private AudioSource _uiSource;

    [Header("BGM Channels")]
    private AudioSource _bgmSource;
    private AudioLowPassFilter _bgmFilter;
    private Coroutine _bgmCrossfade;
    private Coroutine _filterLerp;
    private float _targetBgmVolume = 0.55f;
    private bool _isMuffled;
    private AudioClip _silentDummyClip;

    private static bool _isQuitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetQuitState()
    {
        _isQuitting = false;
    }

    public BGMTrack CurrentBGMTrack { get; private set; } = BGMTrack.None;

    public static AudioManager Ensure()
    {
        if (_isQuitting) return null;
        if (Instance != null) return Instance;
        if (!Application.isPlaying) return null;
        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;

        _dialogueSource = gameObject.AddComponent<AudioSource>();
        _dialogueSource.playOnAwake = false;
        _dialogueSource.spatialBlend = 0f;

        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.playOnAwake = false;
        _uiSource.spatialBlend = 0f;

        // Isolate BGM and its LowPassFilter onto a dedicated child to eliminate
        // "Only custom filters can be played" warning from Unity DSP pipeline
        var bgmGo = new GameObject("BGM_Channel");
        bgmGo.transform.SetParent(transform, false);

        _bgmSource = bgmGo.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.volume = _targetBgmVolume;
        _silentDummyClip = AudioClip.Create("SilentDummyBGM", 1, 1, 44100, false);
        _bgmSource.clip = _silentDummyClip;

        _bgmFilter = bgmGo.AddComponent<AudioLowPassFilter>();
        _bgmFilter.cutoffFrequency = 22000f;
        _bgmFilter.enabled = true;

        // Ensure there is always a master AudioListener active across the whole game
        if (GetComponent<AudioListener>() == null && FindAnyObjectByType<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }

        PreloadClips();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            _isQuitting = true;
            Instance = null;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        var listener = GetComponent<AudioListener>();
        var allListeners = FindObjectsByType<AudioListener>();
        if (allListeners == null || allListeners.Length == 0)
        {
            if (listener == null) gameObject.AddComponent<AudioListener>();
            else listener.enabled = true;
        }
        else if (allListeners.Length > 1 && listener != null)
        {
            bool hasOther = false;
            foreach (var l in allListeners)
            {
                if (l != listener && l.isActiveAndEnabled) { hasOther = true; break; }
            }
            listener.enabled = !hasOther;
        }
    }

    private void PreloadClips()
    {
        string[] clipNames = {
            "sfx_npc_talk", "sfx_interact", "sfx_jump", "sfx_jump_air",
            "sfx_dash", "sfx_hurt", "sfx_attack", "sfx_shoot",
            "sfx_parry", "sfx_collect", "sfx_death", "sfx_ui_open", "sfx_ui_click",
            "sfx_footstep", "sfx_land", "sfx_enemy_dissolve",
            "bgm_menu", "bgm_tutorial", "bgm_world1", "bgm_world2", "bgm_world3", "bgm_world4", "bgm_world5",
            "bgm_safezone", "bgm_traversal", "bgm_boss"
        };

        foreach (var name in clipNames)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip != null)
            {
                _clipCache[name] = clip;
            }
        }
    }

    private AudioClip GetClip(string name)
    {
        if (_clipCache.TryGetValue(name, out var cached) && cached != null)
            return cached;

        var loaded = Resources.Load<AudioClip>("Audio/" + name);
        if (loaded != null)
        {
            _clipCache[name] = loaded;
            return loaded;
        }

        // Procedural synthetic fallback so audio NEVER silently fails
        var fallback = CreateProceduralFallback(name);
        _clipCache[name] = fallback;
        return fallback;
    }

    public static void Play(string clipName, float volume = 1f, float pitch = 1f)
    {
        Ensure()?.PlayInternal(clipName, volume, pitch);
    }

    public void PlayInternal(string clipName, float volume, float pitch)
    {
        var clip = GetClip(clipName);
        if (clip == null) return;

        if (_sfxSource != null)
        {
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, volume);
        }
    }

    public static void SetVolumes(float master, float bgm, float sfx)
    {
        MasterVolume = Mathf.Clamp01(master);
        BGMVolume = Mathf.Clamp01(bgm);
        SFXVolume = Mathf.Clamp01(sfx);
        Ensure()?.ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        _targetBgmVolume = BGMVolume * MasterVolume * 0.75f;
        if (_bgmSource != null) _bgmSource.volume = _isMuffled ? _targetBgmVolume * 0.55f : _targetBgmVolume;
        if (_sfxSource != null) _sfxSource.volume = SFXVolume * MasterVolume;
        if (_dialogueSource != null) _dialogueSource.volume = SFXVolume * MasterVolume;
        if (_uiSource != null) _uiSource.volume = SFXVolume * MasterVolume;
    }

    // -------------------------------------------------------------------------
    // BGM Control & Dynamic Crossfade
    // -------------------------------------------------------------------------
    public static void PlayBGM(BGMTrack track, float crossfade = 0.8f)
    {
        Ensure()?.SetBGMTrack(track, crossfade);
    }

    public static string ResolveClipName(BGMTrack track)
    {
        switch (track)
        {
            case BGMTrack.Menu:
                return "bgm_menu";
            case BGMTrack.Tutorial:
                return "bgm_tutorial";
            case BGMTrack.SafeZone:
                return "bgm_safezone";
            case BGMTrack.World1:
                return "bgm_world1";
            case BGMTrack.World2:
                return "bgm_world2";
            case BGMTrack.World3:
                return "bgm_world3";
            case BGMTrack.World4:
                return "bgm_world4";
            case BGMTrack.World5:
                return "bgm_world5";
            case BGMTrack.Traversal:
                return "bgm_traversal";
            case BGMTrack.Boss:
                return "bgm_boss";
            case BGMTrack.None:
            default:
                return null;
        }
    }

    public static BGMTrack GetWorldBGMTrack(int worldIndex)
    {
        switch (worldIndex)
        {
            case 0: return BGMTrack.World1;
            case 1: return BGMTrack.World2;
            case 2: return BGMTrack.World3;
            case 3: return BGMTrack.World4;
            case 4: return BGMTrack.World5;
            default: return BGMTrack.Traversal;
        }
    }

    /// <summary>
    /// Starts the appropriate background music for a world stage, accounting for safe zone and boss states.
    /// In the same world, continuous playback is preserved without restarting.
    /// </summary>
    public static void PlayWorldBGM(int worldIndex, bool inSafeZone, bool bossActive, float swellDuration = 2.4f)
    {
        BGMTrack track;
        if (bossActive) track = BGMTrack.Boss;
        else track = GetWorldBGMTrack(worldIndex);

        Ensure()?.ApplySafeZoneMuffle(inSafeZone);
        PlayStageOpeningBGM(track, swellDuration);
    }

    private void ApplySafeZoneMuffle(bool inSafeZone)
    {
        // Keep music crisp and melodic in Safe Zones; heavy low-pass muffle is reserved for modals/pause via SetMuffled
        _isMuffled = false;
        if (_bgmFilter != null)
        {
            _bgmFilter.cutoffFrequency = inSafeZone ? 18500f : 22000f;
        }
        if (_bgmSource != null && _bgmCrossfade == null)
        {
            _bgmSource.volume = inSafeZone ? _targetBgmVolume * 0.90f : _targetBgmVolume;
        }
    }

    /// <summary>
    /// Smoothly ducks BGM down and muffles it when entering a warp portal or loading screen.
    /// </summary>
    public static void DuckBGMForWarp(float targetVolMult = 0.15f, float duration = 0.45f)
    {
        Ensure()?.StartDuckBGMForWarp(targetVolMult, duration);
    }

    public void StartDuckBGMForWarp(float targetVolMult, float duration)
    {
        if (_bgmCrossfade != null) StopCoroutine(_bgmCrossfade);
        if (_filterLerp != null) StopCoroutine(_filterLerp);
        _bgmCrossfade = StartCoroutine(DuckBGMForWarpRoutine(targetVolMult, duration));
    }

    private IEnumerator DuckBGMForWarpRoutine(float targetVolMult, float duration)
    {
        if (_bgmSource == null) yield break;
        float startVol = _bgmSource.volume;
        float targetVol = _targetBgmVolume * targetVolMult;
        float startCutoff = _bgmFilter != null ? _bgmFilter.cutoffFrequency : 22000f;
        float targetCutoff = 800f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _bgmSource.volume = Mathf.Lerp(startVol, targetVol, t);
            if (_bgmFilter != null)
            {
                _bgmFilter.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, t);
            }
            yield return null;
        }
        _bgmSource.volume = targetVol;
        if (_bgmFilter != null) _bgmFilter.cutoffFrequency = targetCutoff;
        _bgmCrossfade = null;
    }

    /// <summary>
    /// Smoothly restores BGM back to full volume and crisp fidelity after warp loading completes.
    /// </summary>
    public static void RestoreBGMFromWarp(float duration = 0.8f)
    {
        Ensure()?.StartRestoreBGMFromWarp(duration);
    }

    public void StartRestoreBGMFromWarp(float duration)
    {
        if (_bgmCrossfade != null) StopCoroutine(_bgmCrossfade);
        if (_filterLerp != null) StopCoroutine(_filterLerp);
        _bgmCrossfade = StartCoroutine(RestoreBGMFromWarpRoutine(duration));
    }

    private IEnumerator RestoreBGMFromWarpRoutine(float duration)
    {
        if (_bgmSource == null) yield break;
        float startVol = _bgmSource.volume;
        float targetVol = _isMuffled ? _targetBgmVolume * 0.55f : _targetBgmVolume;
        float startCutoff = _bgmFilter != null ? _bgmFilter.cutoffFrequency : 800f;
        float targetCutoff = _isMuffled ? 750f : 22000f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            _bgmSource.volume = Mathf.Lerp(startVol, targetVol, smoothT);
            if (_bgmFilter != null && !_isMuffled)
            {
                _bgmFilter.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, smoothT);
            }
            yield return null;
        }
        _bgmSource.volume = targetVol;
        if (_bgmFilter != null && !_isMuffled) _bgmFilter.cutoffFrequency = targetCutoff;
        _bgmCrossfade = null;
    }

    /// <summary>
    /// Smoothly fades out BGM to 0 over duration.
    /// </summary>
    public static void FadeOutBGM(float duration = 0.5f)
    {
        Ensure()?.StartFadeOutBGM(duration);
    }

    public void StartFadeOutBGM(float duration)
    {
        if (_bgmCrossfade != null) StopCoroutine(_bgmCrossfade);
        if (_filterLerp != null) StopCoroutine(_filterLerp);
        _bgmCrossfade = StartCoroutine(FadeOutBGMRoutine(duration));
    }

    private IEnumerator FadeOutBGMRoutine(float duration)
    {
        if (_bgmSource == null) yield break;
        float startVol = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _bgmSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        _bgmSource.volume = 0f;
        _bgmCrossfade = null;
    }

    /// <summary>
    /// Plays background music with a smooth cinematic opening swell (volume crescendo from 0
    /// and resonant lowpass filter sweep opening from 800Hz to 22kHz), ideal for world and stage entry.
    /// If the same world track is already playing, continuous playback is seamlessly maintained.
    /// </summary>
    public static void PlayStageOpeningBGM(BGMTrack track, float swellDuration = 2.4f)
    {
        Ensure()?.StartStageOpeningBGM(track, swellDuration);
    }

    public void StartStageOpeningBGM(BGMTrack track, float swellDuration)
    {
        // Continuous playback inside the same world: keep playing without resetting time!
        if (CurrentBGMTrack == track && _bgmSource != null && _bgmSource.isPlaying && _bgmSource.clip != null)
        {
            StartRestoreBGMFromWarp(0.8f);
            return;
        }

        CurrentBGMTrack = track;
        string clipName = ResolveClipName(track);
        AudioClip nextClip = clipName != null ? GetClip(clipName) : null;
        if (nextClip == null) return;

        if (_bgmCrossfade != null) StopCoroutine(_bgmCrossfade);
        if (_filterLerp != null) StopCoroutine(_filterLerp);
        _bgmCrossfade = StartCoroutine(StageOpeningSwellRoutine(nextClip, swellDuration));
    }

    private IEnumerator StageOpeningSwellRoutine(AudioClip clip, float duration)
    {
        if (_bgmSource == null) yield break;

        float targetVol = _isMuffled ? _targetBgmVolume * 0.55f : _targetBgmVolume;
        float targetCutoff = _isMuffled ? 950f : 22000f;
        float startCutoff = _isMuffled ? 950f : 17500f;

        _bgmSource.clip = clip;
        _bgmSource.time = 0f;
        _bgmSource.volume = targetVol * 0.72f;
        _bgmSource.Play();

        if (_bgmFilter != null)
        {
            _bgmFilter.cutoffFrequency = startCutoff;
            _bgmFilter.enabled = true;
        }

        float fastSwell = Mathf.Min(duration, 0.65f);
        float elapsed = 0f;

        while (elapsed < fastSwell)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, fastSwell));
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            _bgmSource.volume = Mathf.Lerp(targetVol * 0.72f, targetVol, smoothT);

            if (_bgmFilter != null && !_isMuffled)
            {
                _bgmFilter.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, smoothT);
            }

            yield return null;
        }

        _bgmSource.volume = targetVol;
        if (_bgmFilter != null && !_isMuffled)
        {
            _bgmFilter.cutoffFrequency = targetCutoff;
        }

        _bgmCrossfade = null;
    }

    public void SetBGMTrack(BGMTrack track, float crossfade)
    {
        if (CurrentBGMTrack == track && _bgmSource != null && _bgmSource.isPlaying) return;
        CurrentBGMTrack = track;

        string clipName = ResolveClipName(track);
        AudioClip nextClip = clipName != null ? GetClip(clipName) : null;

        if (_bgmCrossfade != null) StopCoroutine(_bgmCrossfade);
        _bgmCrossfade = StartCoroutine(CrossfadeRoutine(nextClip, crossfade));
    }

    private IEnumerator CrossfadeRoutine(AudioClip nextClip, float duration)
    {
        if (_bgmSource == null) yield break;

        float startVol = _bgmSource.volume;
        float elapsed = 0f;

        // Fade out
        if (_bgmSource.isPlaying && duration > 0f)
        {
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (duration * 0.5f));
                yield return null;
            }
        }

        if (nextClip != null)
        {
            _bgmSource.clip = nextClip;
            _bgmSource.Play();
            elapsed = 0f;
            float targetVol = _isMuffled ? _targetBgmVolume * 0.55f : _targetBgmVolume;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVol, elapsed / (duration * 0.5f));
                yield return null;
            }
            _bgmSource.volume = targetVol;
        }
        else
        {
            _bgmSource.Stop();
            _bgmSource.clip = _silentDummyClip;
        }

        _bgmCrossfade = null;
    }

    // -------------------------------------------------------------------------
    // Audio Filter: Muffled Low-Pass during Dialog, Overclock, Shop & Death
    // -------------------------------------------------------------------------
    public static void SetMuffled(bool muffled, float volumeScale = 0.55f)
    {
        Ensure()?.ApplyMuffled(muffled, volumeScale);
    }

    public void ApplyMuffled(bool muffled, float volumeScale = 0.55f)
    {
        _isMuffled = muffled;
        if (_bgmFilter == null) return;

        float clampedScale = Mathf.Clamp01(volumeScale);
        if (_filterLerp != null) StopCoroutine(_filterLerp);
        _filterLerp = StartCoroutine(FilterLerpRoutine(muffled ? 750f : 22000f, muffled ? _targetBgmVolume * clampedScale : _targetBgmVolume));
    }

    private IEnumerator FilterLerpRoutine(float targetFreq, float targetVol)
    {
        float startFreq = _bgmFilter.cutoffFrequency;
        float startVol = _bgmSource != null ? _bgmSource.volume : targetVol;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            _bgmFilter.cutoffFrequency = Mathf.Lerp(startFreq, targetFreq, t);
            if (_bgmSource != null) _bgmSource.volume = Mathf.Lerp(startVol, targetVol, t);
            yield return null;
        }

        _bgmFilter.cutoffFrequency = targetFreq;
        if (_bgmSource != null) _bgmSource.volume = targetVol;
        _filterLerp = null;
    }

    // -------------------------------------------------------------------------
    // Specific Action Methods (SFX)
    // -------------------------------------------------------------------------
    public static void PlayNPCTalk()
    {
        var am = Ensure();
        if (am == null) return;
        var clip = am.GetClip("sfx_npc_talk");
        if (clip != null && am._dialogueSource != null)
        {
            am._dialogueSource.pitch = Random.Range(0.92f, 1.08f);
            am._dialogueSource.PlayOneShot(clip, 0.7f);
        }
    }

    public static void PlayInteract()
    {
        Play("sfx_interact", 0.9f, Random.Range(0.95f, 1.05f));
    }

    public static void PlayJump()
    {
        Play("sfx_jump", 0.75f, Random.Range(0.97f, 1.03f));
    }

    public static void PlayDoubleJump()
    {
        Play("sfx_jump_air", 0.8f, Random.Range(0.96f, 1.04f));
    }

    public static void PlayDash()
    {
        Play("sfx_dash", 0.85f, Random.Range(0.95f, 1.05f));
    }

    public static void PlayHurt()
    {
        Play("sfx_hurt", 1.0f, Random.Range(0.95f, 1.05f));
    }

    public static void PlayHitImpact()
    {
        Play("sfx_hurt", 1.0f, Random.Range(1.10f, 1.25f));
    }

    public static void PlayAttack()
    {
        Play("sfx_attack", 0.8f, Random.Range(0.94f, 1.06f));
    }

    public static void PlayShoot()
    {
        Play("sfx_shoot", 0.85f, Random.Range(0.96f, 1.04f));
    }

    public static void PlayParry()
    {
        Play("sfx_parry", 1.0f, 1.0f);
    }

    public static void PlayCollect()
    {
        Play("sfx_collect", 0.8f, Random.Range(0.98f, 1.04f));
    }

    public static void PlayItemPickup() => PlayCollect();
    public static void PlaySafeZoneChime() => PlayUIOpen();

    public static void PlayLevelClear()
    {
        PlayCollect();
    }

    public static void PlayDeath()
    {
        var am = Ensure();
        if (am == null) return;
        var clip = am.GetClip("sfx_death");
        if (clip != null && am._sfxSource != null)
        {
            am._sfxSource.pitch = 1.0f;
            am._sfxSource.PlayOneShot(clip, 1.0f);
        }
    }

    public static void PlayUIOpen()
    {
        var am = Ensure();
        if (am == null) return;
        var clip = am.GetClip("sfx_ui_open");
        if (clip != null && am._uiSource != null)
        {
            am._uiSource.pitch = 1.0f;
            am._uiSource.PlayOneShot(clip, 0.75f);
        }
    }

    public static void PlayUIClick()
    {
        var am = Ensure();
        if (am == null) return;
        var clip = am.GetClip("sfx_ui_click");
        if (clip != null && am._uiSource != null)
        {
            am._uiSource.pitch = Random.Range(0.97f, 1.03f);
            am._uiSource.PlayOneShot(clip, 0.75f);
        }
    }

    public static void PlayFootstep(bool isSprint)
    {
        float pitch = isSprint ? Random.Range(1.15f, 1.25f) : Random.Range(0.95f, 1.05f);
        float vol = isSprint ? 0.40f : 0.30f;
        Play("sfx_footstep", vol, pitch);
    }

    public static void PlayLand()
    {
        Play("sfx_land", 0.65f, Random.Range(0.95f, 1.05f));
    }

    public static void PlayEnemyDissolve()
    {
        Play("sfx_enemy_dissolve", 0.85f, Random.Range(0.95f, 1.08f));
    }

    private static AudioClip CreateProceduralFallback(string name)
    {
        int sampleRate = 44100;

        if (name == "sfx_footstep")
        {
            float duration = 0.045f;
            int count = (int)(sampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float tone = Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.6f;
                data[i] = (tone + noise) * Mathf.Exp(-t * 90f);
            }
            var clip = AudioClip.Create("Fallback_" + name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        if (name == "sfx_land")
        {
            float duration = 0.08f;
            int count = (int)(sampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float sweepFreq = Mathf.Lerp(160f, 40f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * sweepFreq * t);
                data[i] = tone * Mathf.Exp(-t * 40f);
            }
            var clip = AudioClip.Create("Fallback_" + name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        if (name == "bgm_tutorial")
        {
            float duration = 4.0f;
            int count = (int)(sampleRate * duration);
            float[] data = new float[count];
            float[] notes = { 293.66f, 349.23f, 440.00f, 523.25f, 392.00f, 349.23f, 329.63f, 440.00f };
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                int step = ((int)(t * 2f)) % notes.Length;
                float f = notes[step];
                float pad = Mathf.Sin(2f * Mathf.PI * (f * 0.5f) * t) * 0.25f;
                float arp = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-(t % 0.5f) * 4.5f) * 0.35f;
                data[i] = pad + arp;
            }
            var clip = AudioClip.Create("Fallback_" + name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        if (name == "sfx_enemy_dissolve")
        {
            float duration = 0.18f;
            int count = (int)(sampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float sweepFreq = Mathf.Lerp(1400f, 260f, t / duration);
                float mod = Mathf.Sin(2f * Mathf.PI * 60f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * (sweepFreq + mod * 200f) * t);
                float noise = (Random.value * 2f - 1f) * 0.25f;
                data[i] = (tone + noise) * Mathf.Exp(-t * 14f);
            }
            var clip = AudioClip.Create("Fallback_" + name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        {
            float duration = 0.1f;
            int count = (int)(sampleRate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 440f + 200f * Mathf.Sin(t * 50f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 15f);
            }

            var clip = AudioClip.Create("Fallback_" + name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
