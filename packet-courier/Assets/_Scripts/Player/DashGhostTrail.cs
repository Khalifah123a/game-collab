using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-performance Dash Ghost Trail (After-image effect) with pre-warmed object pooling.
/// Emits cybernetic ghost silhouettes during Dash and Phase Dash without GC allocations.
/// Supports dynamic tints: Cyber Cyan (default), Phase Magenta (Phase Dash), and Electric Gold (Overclock Buff).
/// </summary>
public class DashGhostTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public float sampleInterval = 0.035f;
    public float ghostFadeDuration = 0.20f;
    public Color normalDashColor = new Color(0f, 0.9f, 1f, 0.65f);      // Cyber Cyan
    public Color phaseDashColor = new Color(1f, 0f, 0.55f, 0.75f);       // Phase Magenta
    public Color overclockColor = new Color(1f, 0.84f, 0f, 0.75f);       // Electric Gold

    private PlayerController _player;
    private SpriteRenderer _spriteRenderer;
    private float _sampleTimer;

    // Fast static circular pool for zero runtime garbage collection
    private static readonly Queue<GhostInstance> _pool = new Queue<GhostInstance>();
    private static readonly List<GhostInstance> _activeGhosts = new List<GhostInstance>();
    private static Transform _poolContainer;

    private const int INITIAL_POOL_SIZE = 14;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        EnsurePoolPrewarmed();
    }

    private static void EnsurePoolPrewarmed()
    {
        if (_poolContainer == null)
        {
            var go = new GameObject("GhostTrailPool");
            Object.DontDestroyOnLoad(go);
            _poolContainer = go.transform;
        }

        while (_pool.Count < INITIAL_POOL_SIZE)
        {
            var ghostGO = new GameObject("DashGhost");
            ghostGO.transform.SetParent(_poolContainer);
            var sr = ghostGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 45; // Render directly behind active player sprite (order 50)
            ghostGO.SetActive(false);
            _pool.Enqueue(new GhostInstance(ghostGO, sr));
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Process and fade active ghost after-images
        for (int i = _activeGhosts.Count - 1; i >= 0; i--)
        {
            var ghost = _activeGhosts[i];
            ghost.elapsed += dt;
            if (ghost.elapsed >= ghostFadeDuration)
            {
                ghost.gameObject.SetActive(false);
                _activeGhosts.RemoveAt(i);
                _pool.Enqueue(ghost);
            }
            else
            {
                float t = ghost.elapsed / ghostFadeDuration;
                Color c = ghost.startColor;
                c.a = Mathf.Lerp(ghost.startColor.a, 0f, t);
                ghost.renderer.color = c;
            }
        }

        if (_player == null || !_player.isDashing || _spriteRenderer == null || _spriteRenderer.sprite == null)
        {
            _sampleTimer = 0f;
            return;
        }

        _sampleTimer -= dt;
        if (_sampleTimer <= 0f)
        {
            _sampleTimer = sampleInterval;
            SpawnGhost();
        }
    }

    private void SpawnGhost()
    {
        EnsurePoolPrewarmed();

        GhostInstance ghost;
        if (_pool.Count > 0)
        {
            ghost = _pool.Dequeue();
        }
        else
        {
            var ghostGO = new GameObject("DashGhost");
            ghostGO.transform.SetParent(_poolContainer);
            var sr = ghostGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 45;
            ghost = new GhostInstance(ghostGO, sr);
        }

        Color tint = normalDashColor;
        if (OverclockManager.HasPhaseDash)
            tint = phaseDashColor;
        else if (OverclockManager.HasBuffActive)
            tint = overclockColor;

        ghost.gameObject.transform.position = _spriteRenderer.transform.position;
        ghost.gameObject.transform.rotation = _spriteRenderer.transform.rotation;
        ghost.gameObject.transform.localScale = _spriteRenderer.transform.lossyScale;

        ghost.renderer.sprite = _spriteRenderer.sprite;
        ghost.renderer.flipX = _spriteRenderer.flipX;
        ghost.renderer.flipY = _spriteRenderer.flipY;
        ghost.renderer.color = tint;

        ghost.startColor = tint;
        ghost.elapsed = 0f;
        ghost.gameObject.SetActive(true);

        _activeGhosts.Add(ghost);
    }

    private class GhostInstance
    {
        public readonly GameObject gameObject;
        public readonly SpriteRenderer renderer;
        public Color startColor;
        public float elapsed;

        public GhostInstance(GameObject go, SpriteRenderer sr)
        {
            gameObject = go;
            renderer = sr;
        }
    }
}
