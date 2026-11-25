using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for Game Feel / Combat Juice (Silksong & Hollow Knight standards).
/// Provides Hitstop (micro freeze frames), Trauma-based Camera Shake, Screen Damage Glitch,
/// and pre-warmed object-pooled 2D VFX particle bursts (sparks, dust puffs, wall cling friction, and data dissolve).
/// </summary>
public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    [Header("Hitstop Config")]
    public float defaultHitstopDuration = 0.05f;

    [Header("Screen Shake Config")]
    public float traumaDecay = 1.6f;
    public float maxShakeOffset = 0.45f;
    public float maxShakeAngle = 2.5f;

    public static bool EnableScreenShake = true;

    private float _trauma;
    private Coroutine _hitstopCoroutine;
    private float _timeScaleBeforeHitstop = 1f;
    private bool _isHitstopping = false;
    private float _hitstopEndTime = 0f;
    private float _perlinSeedX;
    private float _perlinSeedY;

    [Header("Damage Glitch Screen Feedback")]
    private Canvas _glitchCanvas;
    private UnityEngine.UI.Image _glitchImage;
    private Coroutine _glitchCoroutine;

    // Fast static object pool for zero GC allocation in particle bursts
    private static readonly Queue<SparkMover> _sparkPool = new Queue<SparkMover>();
    private static Transform _sparkPoolContainer;
    private const int INITIAL_SPARK_POOL_SIZE = 36;

    public float CurrentTrauma => _trauma;

    public Vector3 CurrentShakeOffset
    {
        get
        {
            if (!EnableScreenShake || _trauma <= 0.001f) return Vector3.zero;
            float shake = _trauma * _trauma; // Quadratic falloff
            float time = Time.unscaledTime * 35f;
            float x = (Mathf.PerlinNoise(_perlinSeedX, time) * 2f - 1f) * maxShakeOffset * shake;
            float y = (Mathf.PerlinNoise(_perlinSeedY, time) * 2f - 1f) * maxShakeOffset * shake;
            return new Vector3(x, y, 0f);
        }
    }

    public static JuiceManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("JuiceManager");
        return go.AddComponent<JuiceManager>();
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
        _perlinSeedX = Random.Range(0f, 100f);
        _perlinSeedY = Random.Range(100f, 200f);

        EnsureSparkPoolPrewarmed();
        EnsureGlitchCanvasCreated();
    }

    private void Update()
    {
        if (_trauma > 0f)
        {
            _trauma = Mathf.Max(0f, _trauma - traumaDecay * Time.unscaledDeltaTime);
        }

        // Safety watchdog: Automatically restore Time.timeScale when hitstop duration expires
        if (_isHitstopping)
        {
            if (Time.unscaledTime >= _hitstopEndTime)
            {
                EndHitstop();
            }
        }
        else
        {
            // Failsafe watchdog: If timeScale is 0 but nobody paused the game, restore it immediately!
            var gm = GameManager.Instance;
            bool isPausedOrOver = (gm != null && (gm.state == GameManager.GameState.GameOver || gm.state == GameManager.GameState.Paused)) || PauseMenuUI.IsPaused;
            if (!isPausedOrOver && Time.timeScale == 0f)
            {
                Time.timeScale = _timeScaleBeforeHitstop > 0f ? _timeScaleBeforeHitstop : 1f;
            }
        }
    }

    // -------------------------------------------------------------------------
    // 1. Camera Shake (Trauma Model)
    // -------------------------------------------------------------------------
    public static void Shake(float amount)
    {
        var jm = Ensure();
        if (jm != null)
        {
            jm._trauma = Mathf.Clamp01(jm._trauma + amount);
        }
    }

    // -------------------------------------------------------------------------
    // 2. Hitstop (Micro Freeze Frame)
    // -------------------------------------------------------------------------
    public static void Hitstop(float duration = 0.05f)
    {
        var jm = Ensure();
        if (jm != null)
        {
            jm.DoHitstop(duration);
        }
    }

    public void DoHitstop(float duration)
    {
        var gm = GameManager.Instance;
        if (gm != null && (gm.state == GameManager.GameState.GameOver || gm.state == GameManager.GameState.Paused)) return;
        if (PauseMenuUI.IsPaused) return;

        // If not already hitstopping, capture the active timescale (typically 1f)
        if (!_isHitstopping)
        {
            _timeScaleBeforeHitstop = Time.timeScale > 0f ? Time.timeScale : 1f;
            _isHitstopping = true;
            Time.timeScale = 0f;
        }

        _hitstopEndTime = Time.unscaledTime + duration;

        if (_hitstopCoroutine != null)
            StopCoroutine(_hitstopCoroutine);

        _hitstopCoroutine = StartCoroutine(HitstopRoutine(duration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        EndHitstop();
    }

    public void EndHitstop()
    {
        if (!_isHitstopping) return;
        _isHitstopping = false;

        if (_hitstopCoroutine != null)
        {
            StopCoroutine(_hitstopCoroutine);
            _hitstopCoroutine = null;
        }

        var gm = GameManager.Instance;
        bool isPausedOrOver = (gm != null && (gm.state == GameManager.GameState.GameOver || gm.state == GameManager.GameState.Paused)) || PauseMenuUI.IsPaused;
        if (!isPausedOrOver)
        {
            Time.timeScale = _timeScaleBeforeHitstop > 0f ? _timeScaleBeforeHitstop : 1f;
        }
    }

    private void OnDisable()
    {
        EndHitstop();
    }

    private void OnDestroy()
    {
        EndHitstop();
    }

    // -------------------------------------------------------------------------
    // 3. Screen Glitch Damage Feedback
    // -------------------------------------------------------------------------
    private void EnsureGlitchCanvasCreated()
    {
        if (_glitchCanvas != null) return;

        var canvasGO = new GameObject("DamageGlitchCanvas");
        canvasGO.transform.SetParent(transform);
        _glitchCanvas = canvasGO.AddComponent<Canvas>();
        _glitchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _glitchCanvas.sortingOrder = 999; // Topmost visual effect

        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var imgGO = new GameObject("GlitchVignette");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _glitchImage = imgGO.AddComponent<UnityEngine.UI.Image>();
        _glitchImage.color = new Color(1f, 0.1f, 0.2f, 0f);
        _glitchImage.raycastTarget = false;

        var rt = _glitchImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void TriggerDamageGlitch(float duration = 0.14f, Color? flashColor = null)
    {
        var jm = Ensure();
        if (jm != null)
        {
            jm.DoDamageGlitch(duration, flashColor ?? new Color(1f, 0.12f, 0.25f, 0.38f));
        }
    }

    public void DoDamageGlitch(float duration, Color flashColor)
    {
        EnsureGlitchCanvasCreated();
        if (_glitchImage == null) return;

        if (_glitchCoroutine != null) StopCoroutine(_glitchCoroutine);
        _glitchCoroutine = StartCoroutine(DamageGlitchRoutine(duration, flashColor));
    }

    private IEnumerator DamageGlitchRoutine(float duration, Color targetColor)
    {
        _glitchImage.color = targetColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Color c = targetColor;
            c.a = Mathf.Lerp(targetColor.a, 0f, t);
            _glitchImage.color = c;
            yield return null;
        }

        _glitchImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        _glitchCoroutine = null;
    }

    // -------------------------------------------------------------------------
    // 4. Object-Pooled 2D VFX Particle Bursts
    // -------------------------------------------------------------------------
    private static void EnsureSparkPoolPrewarmed()
    {
        if (_sparkPoolContainer == null)
        {
            var go = new GameObject("SparkPoolContainer");
            Object.DontDestroyOnLoad(go);
            _sparkPoolContainer = go.transform;
            _sparkPool.Clear();
        }

        while (_sparkPool.Count < INITIAL_SPARK_POOL_SIZE)
        {
            var spark = CreateNewPooledSpark();
            if (spark != null) _sparkPool.Enqueue(spark);
        }
    }

    private static SparkMover CreateNewPooledSpark()
    {
        if (_sparkPoolContainer == null)
        {
            var containerGO = new GameObject("SparkPoolContainer");
            Object.DontDestroyOnLoad(containerGO);
            _sparkPoolContainer = containerGO.transform;
        }

        var go = new GameObject("PooledSpark");
        go.transform.SetParent(_sparkPoolContainer);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSparkSprite();
        sr.sortingOrder = 50;
        var mover = go.AddComponent<SparkMover>();
        go.SetActive(false);
        return mover;
    }

    public static SparkMover GetPooledSpark()
    {
        EnsureSparkPoolPrewarmed();
        SparkMover mover = null;

        while (_sparkPool.Count > 0)
        {
            var candidate = _sparkPool.Dequeue();
            if (candidate != null && candidate.gameObject != null)
            {
                mover = candidate;
                break;
            }
        }

        if (mover == null)
        {
            mover = CreateNewPooledSpark();
        }

        if (mover != null && mover.gameObject != null)
        {
            mover.gameObject.SetActive(true);
        }
        return mover;
    }

    public static void ReturnSparkToPool(SparkMover mover)
    {
        if (mover == null || mover.gameObject == null) return;
        mover.gameObject.SetActive(false);
        if (_sparkPoolContainer != null) mover.transform.SetParent(_sparkPoolContainer);
        _sparkPool.Enqueue(mover);
    }

    public static void SpawnImpactSparks(Vector3 pos, Color? color = null, float facing = 0f)
    {
        Color c = color ?? new Color(0.2f, 0.95f, 1f, 1f);
        int sparkCount = 7;
        for (int i = 0; i < sparkCount; i++)
        {
            var mover = GetPooledSpark();
            mover.transform.position = pos;
            mover.SetColor(c);
            mover.transform.localScale = Vector3.one * Random.Range(0.18f, 0.38f);

            Vector2 vel;
            if (facing != 0f)
            {
                float baseAngle = facing > 0f ? 0f : 180f;
                float angle = (baseAngle + Random.Range(-40f, 40f)) * Mathf.Deg2Rad;
                float speed = Random.Range(5f, 12f);
                vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            }
            else
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(4f, 10f);
                vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            }
            mover.Activate(vel, Random.Range(0.12f, 0.22f));
        }
    }

    public static void SpawnDustPuff(Vector3 pos)
    {
        int dustCount = 4;
        Color dustCol = new Color(0.7f, 0.9f, 1f, 0.6f);
        for (int i = 0; i < dustCount; i++)
        {
            var mover = GetPooledSpark();
            mover.transform.position = pos + new Vector3(Random.Range(-0.35f, 0.35f), -0.4f, 0f);
            mover.SetColor(dustCol);
            mover.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);

            Vector2 vel = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.4f, 1.6f));
            mover.Activate(vel, 0.18f);
        }
    }

    public static void SpawnWallSparks(Vector3 pos, int wallSide)
    {
        Color c = new Color(0.35f, 0.85f, 1f, 0.85f);
        int count = 2;
        for (int i = 0; i < count; i++)
        {
            var mover = GetPooledSpark();
            mover.transform.position = pos + new Vector3(wallSide * 0.08f, Random.Range(-0.08f, 0.08f), 0f);
            mover.SetColor(c);
            mover.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

            // Spray diagonally downward away from the wall
            float angle = (-wallSide * 40f + Random.Range(-20f, 20f) - 90f) * Mathf.Deg2Rad;
            float speed = Random.Range(2.5f, 5.5f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            mover.Activate(vel, Random.Range(0.10f, 0.16f));
        }
    }

    public static void SpawnDataDissolve(Vector3 pos, int count = 12, Color? color = null)
    {
        Color c = color ?? new Color(0.15f, 1f, 0.75f, 1f); // Cyber neon matrix green/cyan
        for (int i = 0; i < count; i++)
        {
            var mover = GetPooledSpark();
            mover.transform.position = pos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0f);
            mover.SetColor(c);
            mover.transform.localScale = Vector3.one * Random.Range(0.20f, 0.42f);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(3.5f, 8.5f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            mover.Activate(vel, Random.Range(0.20f, 0.35f), true); // true for digital flicker
        }
    }

    private static Sprite _sparkSprite;
    private static Sprite GetSparkSprite()
    {
        if (_sparkSprite != null) return _sparkSprite;
        _sparkSprite = Resources.Load<Sprite>("GeneratedArt/key_e_ring");
        if (_sparkSprite != null) return _sparkSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        _sparkSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _sparkSprite;
    }
}

/// <summary>
/// Reusable pooled particle mover with velocity, drag, alpha fade, and optional digital flicker.
/// </summary>
public class SparkMover : MonoBehaviour
{
    public Vector2 velocity;
    public float duration = 0.2f;
    private float _timer;
    private SpriteRenderer _sr;
    private Color _initColor;
    private bool _digitalFlicker;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _initColor = _sr.color;
    }

    public void SetColor(Color c)
    {
        _initColor = c;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _sr.color = c;
    }

    public void Activate(Vector2 initVelocity, float lifeDuration, bool digitalFlicker = false)
    {
        velocity = initVelocity;
        duration = lifeDuration;
        _timer = 0f;
        _digitalFlicker = digitalFlicker;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _sr.color = _initColor;
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
        velocity *= (1f - 4f * Time.unscaledDeltaTime); // Drag

        if (_sr != null)
        {
            float t = Mathf.Clamp01(_timer / duration);
            Color c = _initColor;
            c.a = Mathf.Lerp(_initColor.a, 0f, t);

            if (_digitalFlicker && Random.value < 0.25f)
            {
                c.a *= 0.35f; // Electronic matrix flicker
            }

            _sr.color = c;
        }

        if (_timer >= duration)
        {
            JuiceManager.ReturnSparkToPool(this);
        }
    }
}
