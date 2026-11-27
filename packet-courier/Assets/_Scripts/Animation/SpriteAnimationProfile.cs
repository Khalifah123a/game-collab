using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reusable ScriptableObject profile for custom Boss and Enemy sprites & frame animations.
/// Creators can drag-and-drop a single Default Sprite or full multi-frame animation states
/// directly in the Inspector, then either assign this profile to ModularSpriteAnimator
/// or place it in Assets/Resources/CustomAnimations/ for automatic runtime loading.
/// </summary>
[CreateAssetMenu(fileName = "NewSpriteAnimProfile", menuName = "PacketCourier/Sprite & Animation Profile", order = 1)]
public class SpriteAnimationProfile : ScriptableObject
{
    [Header("Sprite Tunggal (Opsional / Fallback)")]
    [Tooltip("Jika baru punya 1 gambar diam, cukup drag sprite ke sini. Juga dipakai sebagai fallback jika state animasi belum diisi.")]
    public Sprite defaultSprite;

    [Header("Pengaturan Visual")]
    [Tooltip("Centang jika gambar sprite aslinya menghadap ke KIRI.")]
    public bool invertFacing = false;

    [Tooltip("Pengali ukuran gambar tanpa mengubah hitbox gameplay.")]
    [Range(0.25f, 4.0f)]
    public float visualScaleMultiplier = 1.0f;

    [Tooltip("Geser posisi gambar (X, Y) agar kaki pas menyentuh tanah.")]
    public Vector2 visualOffset = Vector2.zero;

    [Tooltip("Pengali kecepatan animasi global.")]
    [Range(0.1f, 5.0f)]
    public float playbackSpeed = 1.0f;

    [Tooltip("Aktifkan jika ingin warna State Tint per animasi menimpa warna SpriteRenderer.")]
    public bool applyStateTint = false;

    [Header("Daftar State Animasi (Drag & Drop Frame Sprite per State)")]
    public List<SpriteAnimationState> states = new List<SpriteAnimationState>();

    /// <summary>
    /// Returns true only when the creator has assigned a defaultSprite OR at least one valid animation frame.
    /// When false, built-in fallback art remains active seamlessly.
    /// </summary>
    public bool HasAnyCustomArt()
    {
        if (defaultSprite != null) return true;
        if (states == null) return false;
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].HasValidFrames)
                return true;
        }
        return false;
    }

    public bool HasValidState(string stateName)
    {
        var state = GetState(stateName);
        return state != null && state.HasValidFrames;
    }

    public SpriteAnimationState GetState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || states == null) return null;
        for (int i = 0; i < states.Count; i++)
        {
            var s = states[i];
            if (s != null && string.Equals(s.stateName, stateName, StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }

    /// <summary>
    /// Returns the best fallback sprite (defaultSprite -> Idle[0] -> Walk[0] -> first non-null frame in any state).
    /// </summary>
    public Sprite GetFirstAvailableSprite()
    {
        if (defaultSprite != null) return defaultSprite;

        Sprite fromIdle = GetFirstFrameOf("Idle");
        if (fromIdle != null) return fromIdle;

        Sprite fromWalk = GetFirstFrameOf("Walk");
        if (fromWalk != null) return fromWalk;

        if (states != null)
        {
            for (int i = 0; i < states.Count; i++)
            {
                Sprite s = states[i]?.GetFirstValidFrame();
                if (s != null) return s;
            }
        }
        return null;
    }

    private Sprite GetFirstFrameOf(string stateName)
    {
        var st = GetState(stateName);
        return st != null ? st.GetFirstValidFrame() : null;
    }

    public void EnsurePresetState(string stateName, float fps, bool loop, int triggerFrame = -1, string eventTag = "")
    {
        if (states == null) states = new List<SpriteAnimationState>();
        if (GetState(stateName) != null) return;

        states.Add(new SpriteAnimationState
        {
            stateName = stateName,
            frames = new Sprite[0],
            framesPerSecond = fps,
            loop = loop,
            triggerFrame = triggerFrame,
            eventTag = eventTag,
            localOffset = Vector2.zero,
            stateTint = Color.white,
            unscaledTime = false
        });
    }

    public void PopulateEnemyPresets()
    {
        EnsurePresetState("Idle", 6f, true);
        EnsurePresetState("Walk", 10f, true);
        EnsurePresetState("Chase", 12f, true);
        EnsurePresetState("Windup", 10f, false);
        EnsurePresetState("Attack", 12f, false, 1, "hit");
        EnsurePresetState("Hurt", 12f, false);
        EnsurePresetState("Death", 8f, false);
    }

    public void PopulateBossPresets()
    {
        EnsurePresetState("Idle", 6f, true);
        EnsurePresetState("Walk", 9f, true);
        EnsurePresetState("Windup", 10f, false);
        EnsurePresetState("Slash", 14f, false, 2, "hit");
        EnsurePresetState("Attack", 12f, false, 2, "hit");
        EnsurePresetState("Dash", 14f, true);
        EnsurePresetState("Jump", 10f, false);
        EnsurePresetState("Slam", 12f, false, 1, "slam");
        EnsurePresetState("PhaseShift", 10f, false);
        EnsurePresetState("Hurt", 12f, false);
        EnsurePresetState("Death", 8f, false);
    }
}
