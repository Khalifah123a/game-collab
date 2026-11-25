using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a locked Boss Arena encounter flow:
/// Pre-Arena Checkpoint -> Enter Arena Trigger -> Entrance & Exit Laser Gates Lock ->
/// CameraFollow frames & zooms out onto Arena -> Grounded Boss wakes + Boss BGM ->
/// Victory unlocks Exit Gate, restores Camera & World BGM.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BossArenaController : MonoBehaviour
{
    private static readonly List<BossArenaController> ActiveArenas = new List<BossArenaController>();

    [Header("Arena Bounds")]
    public float arenaMinX;
    public float arenaMaxX;
    public float floorY;
    public float arenaHeight = 8.0f;
    public float arenaCameraOrthoSize = 6.2f;
    public int worldIndex = 0;

    [Header("References")]
    public Boss_Grounded targetBoss;

    private BoxCollider2D _entryTrigger;
    private GameObject _leftGateGo;
    private BoxCollider2D _leftGateCol;
    private SpriteRenderer _leftGateSr;

    private GameObject _rightGateGo;
    private BoxCollider2D _rightGateCol;
    private SpriteRenderer _rightGateSr;

    private bool _engaged;
    private bool _cleared;

    private static Sprite _cachedGateSprite;

    public static bool HasUnfinishedArena
    {
        get
        {
            for (int i = ActiveArenas.Count - 1; i >= 0; i--)
            {
                var arena = ActiveArenas[i];
                if (arena == null)
                {
                    ActiveArenas.RemoveAt(i);
                    continue;
                }
                if (!arena._cleared) return true;
            }
            return false;
        }
    }

    public static BossArenaController CreateArena(
        Transform parent,
        string name,
        float minX,
        float maxX,
        float floorSurfaceY,
        int worldIdx,
        Boss_Grounded boss,
        float cameraOrthoSize = 6.2f)
    {
        var go = new GameObject(name, typeof(BoxCollider2D), typeof(BossArenaController));
        if (parent != null) go.transform.SetParent(parent, false);

        // Place entry trigger slightly inside the arena entrance so player has crossed left gate safely
        float entryX = Mathf.Lerp(minX, maxX, 0.22f);
        go.transform.position = new Vector3(entryX, floorSurfaceY + 2.5f, 0f);

        var arena = go.GetComponent<BossArenaController>();
        arena.arenaMinX = Mathf.Min(minX, maxX);
        arena.arenaMaxX = Mathf.Max(minX, maxX);
        arena.floorY = floorSurfaceY;
        arena.worldIndex = worldIdx;
        arena.targetBoss = boss;
        arena.arenaCameraOrthoSize = cameraOrthoSize;
        arena.SetupGeometry();
        return arena;
    }

    private void Awake()
    {
        if (!ActiveArenas.Contains(this)) ActiveArenas.Add(this);
        _entryTrigger = GetComponent<BoxCollider2D>();
        _entryTrigger.isTrigger = true;
    }

    private void SetupGeometry()
    {
        if (_entryTrigger == null) _entryTrigger = GetComponent<BoxCollider2D>();
        _entryTrigger.isTrigger = true;
        _entryTrigger.size = new Vector2(3.0f, 6.0f);

        // Left Entrance Gate: open while approaching, locks behind player when fight starts
        BuildLaserGate(
            "ArenaGate_Left",
            new Vector3(arenaMinX + 0.35f, floorY + arenaHeight * 0.5f, 0f),
            lockedInitially: false,
            out _leftGateGo,
            out _leftGateCol,
            out _leftGateSr);

        // Right Exit Gate: locked until Boss defeated
        BuildLaserGate(
            "ArenaGate_Right",
            new Vector3(arenaMaxX - 0.35f, floorY + arenaHeight * 0.5f, 0f),
            lockedInitially: true,
            out _rightGateGo,
            out _rightGateCol,
            out _rightGateSr);
    }

    private void BuildLaserGate(
        string gateName,
        Vector3 worldPos,
        bool lockedInitially,
        out GameObject gateGo,
        out BoxCollider2D gateCol,
        out SpriteRenderer gateSr)
    {
        gateGo = new GameObject(gateName);
        gateGo.transform.SetParent(transform, false);
        gateGo.transform.position = worldPos;

        gateCol = gateGo.AddComponent<BoxCollider2D>();
        gateCol.isTrigger = false;
        gateCol.size = new Vector2(0.55f, arenaHeight);
        gateCol.enabled = lockedInitially;

        gateSr = gateGo.AddComponent<SpriteRenderer>();
        gateSr.sprite = GetGateSprite();
        gateSr.sortingOrder = 16;
        gateGo.transform.localScale = new Vector3(0.55f, arenaHeight, 1f);
        gateSr.color = lockedInitially ? new Color(1f, 0.22f, 0.32f, 0.88f) : new Color(0.15f, 0.95f, 0.85f, 0.14f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_engaged || _cleared || other == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        Engage();
    }

    private void Update()
    {
        if (_cleared) return;

        // If player died in-scene or is dead, clear camera lock cleanly
        var ph = PlayerHealth.Instance;
        if (ph != null && ph.isDead && _engaged)
        {
            ClearCameraLock();
        }

        // Pulse locked gates
        float pulse = 0.72f + 0.22f * Mathf.Sin(Time.time * 9f);
        if (_engaged && _leftGateSr != null)
        {
            _leftGateSr.color = new Color(1f, 0.2f, 0.3f, pulse);
        }
        if (!_cleared && _rightGateSr != null)
        {
            _rightGateSr.color = new Color(1f, 0.2f, 0.3f, pulse);
        }

        // Failsafe if boss was destroyed externally
        if (_engaged && targetBoss == null)
        {
            OnBossDefeated();
        }
    }

    public void Engage()
    {
        if (_engaged || _cleared) return;
        _engaged = true;

        // Slam Entrance Laser Gate shut behind player
        if (_leftGateCol != null) _leftGateCol.enabled = true;
        if (_leftGateSr != null) _leftGateSr.color = new Color(1f, 0.22f, 0.32f, 0.9f);
        JuiceManager.Shake(0.28f);
        AudioManager.Play("sfx_laser", 0.7f, 0.75f);

        // Smoothly zoom camera out and frame Arena
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.SetArenaBounds(arenaMinX, arenaMaxX, floorY, arenaCameraOrthoSize);
        }

        // Switch BGM to Boss battle track
        AudioManager.PlayBGM(AudioManager.BGMTrack.Boss, 0.75f);

        if (targetBoss != null && !targetBoss.IsDead)
        {
            targetBoss.EngageArena(arenaMinX, arenaMaxX, floorY, OnBossDefeated);
        }
        else
        {
            OnBossDefeated();
        }
    }

    private void OnBossDefeated()
    {
        if (_cleared) return;
        _cleared = true;
        _engaged = false;

        if (_leftGateCol != null) _leftGateCol.enabled = false;
        if (_leftGateSr != null) _leftGateSr.color = new Color(0.15f, 1f, 0.65f, 0.16f);

        if (_rightGateCol != null) _rightGateCol.enabled = false;
        if (_rightGateSr != null) _rightGateSr.color = new Color(0.15f, 1f, 0.65f, 0.16f);

        ClearCameraLock();
        AudioManager.Play("sfx_powerup", 0.9f, 1.05f);
        AudioManager.PlayWorldBGM(worldIndex, SafeZone.IsPlayerSafe, false, 1.8f);
    }

    private static void ClearCameraLock()
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.ClearArenaBounds();
    }

    private void OnDestroy()
    {
        ActiveArenas.Remove(this);
        ClearCameraLock();
    }

    private static Sprite GetGateSprite()
    {
        if (_cachedGateSprite == null)
        {
            var tex = new Texture2D(8, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 32; y++)
            {
                bool scan = (y % 4 == 0);
                for (int x = 0; x < 8; x++)
                {
                    bool edge = (x == 0 || x == 7);
                    float a = edge ? 1f : (scan ? 0.85f : 0.45f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _cachedGateSprite = Sprite.Create(tex, new Rect(0, 0, 8, 32), new Vector2(0.5f, 0.5f), 8f);
        }
        return _cachedGateSprite;
    }
}
