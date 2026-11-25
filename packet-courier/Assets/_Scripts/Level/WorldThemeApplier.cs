using UnityEngine;

/// <summary>
/// Dynamically skins Ground, OneWay Catwalks, and Moving Platforms to match the specific
/// theme of the current world:
/// World 1: Localhost (Green PCB Motherboard, Silicon & Emerald Conduits)
/// World 2: LAN Party (Electric Blue & Violet Ethernet Cables, Router Hubs)
/// World 3: Fiber Deep (Luminous Aquamarine Fiber Optics, Undersea Trench)
/// World 4: Satellite Relay (Gold Solar Foil, Space Carbon Truss & Ion Drives)
/// World 5: Rootkit Core (Corrupted Obsidian Malware, Crimson Veins & Glitch)
/// </summary>
public static class WorldThemeApplier
{
    private static Sprite[] _groundSprites;
    private static Sprite[] _groundSubSprites;
    private static Sprite[] _oneWaySprites;
    private static Sprite[] _movingSprites;
    private static Sprite[] _hazardSprites;
    private static Sprite _safePadSprite;
    private static Sprite _safePadSubSprite;
    private static bool _initialized;

    public static void Initialize(bool force = false)
    {
        if (_initialized && !force && _groundSprites != null && _groundSprites[0] != null &&
            _groundSubSprites != null && _groundSubSprites[0] != null) return;
        _initialized = true;

        if (_groundSprites == null) _groundSprites = new Sprite[5];
        if (_groundSubSprites == null) _groundSubSprites = new Sprite[5];
        if (_oneWaySprites == null) _oneWaySprites = new Sprite[5];
        if (_movingSprites == null) _movingSprites = new Sprite[5];
        if (_hazardSprites == null) _hazardSprites = new Sprite[5];

        for (int i = 0; i < 5; i++)
        {
            int w = i + 1;
            if (_groundSprites[i] == null || force) _groundSprites[i] = Resources.Load<Sprite>($"GeneratedArt/ground_w{w}");
            if (_groundSubSprites[i] == null || force) _groundSubSprites[i] = Resources.Load<Sprite>($"GeneratedArt/ground_w{w}_sub");
            if (_oneWaySprites[i] == null || force) _oneWaySprites[i] = Resources.Load<Sprite>($"GeneratedArt/platform_oneway_w{w}");
            if (_movingSprites[i] == null || force) _movingSprites[i] = Resources.Load<Sprite>($"GeneratedArt/platform_moving_w{w}");
            if (_hazardSprites[i] == null || force) _hazardSprites[i] = Resources.Load<Sprite>($"GeneratedArt/hazard_spikes_w{w}");
        }

        if (_safePadSprite == null || force) _safePadSprite = Resources.Load<Sprite>("GeneratedArt/safepad");
        if (_safePadSubSprite == null || force) _safePadSubSprite = Resources.Load<Sprite>("GeneratedArt/safepad_sub");
    }

    public static Sprite GetSafePadSprite(bool subsurface = false)
    {
        Initialize();
        return subsurface ? (_safePadSubSprite != null ? _safePadSubSprite : _safePadSprite) : _safePadSprite;
    }

    public static Sprite GetGroundSprite(int worldIndex, bool subsurface = false)
    {
        Initialize();
        int idx = Mathf.Clamp(worldIndex, 0, 4);
        if (subsurface && _groundSubSprites[idx] != null) return _groundSubSprites[idx];
        return _groundSprites[idx];
    }

    public static void ApplyTheme(int worldIndex)
    {
        Initialize();
        int idx = Mathf.Clamp(worldIndex, 0, 4);

        // Ensure camera background clear flags and atmospheric color match the current cyber world
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            switch (idx)
            {
                case 0: cam.backgroundColor = new Color(0.015f, 0.035f, 0.025f, 1f); break; // World 1: Localhost Greenish
                case 1: cam.backgroundColor = new Color(0.020f, 0.035f, 0.080f, 1f); break; // World 2: LAN Party Deep Indigo
                case 2: cam.backgroundColor = new Color(0.010f, 0.040f, 0.050f, 1f); break; // World 3: Fiber Deep Abyss Teal
                case 3: cam.backgroundColor = new Color(0.035f, 0.030f, 0.050f, 1f); break; // World 4: Satellite Relay Space Void
                case 4: cam.backgroundColor = new Color(0.050f, 0.010f, 0.020f, 1f); break; // World 5: Rootkit Core Dark Crimson
                default: cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f); break;
            }
        }

        // Ensure dynamic 3-layer parallax background and cyber dust particle system is running
        ParallaxBackgroundSystem.Ensure(idx);

        var groundSprite = _groundSprites[idx];
        var groundSubSprite = _groundSubSprites[idx] != null ? _groundSubSprites[idx] : groundSprite;
        var oneWaySprite = _oneWaySprites[idx];
        var movingSprite = _movingSprites[idx];
        var hazardSprite = _hazardSprites[idx];
        var safePadSprite = _safePadSprite;
        var safePadSubSprite = _safePadSubSprite != null ? _safePadSubSprite : _safePadSprite;

        var renderers = Object.FindObjectsByType<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (sr == null || sr.gameObject == null) continue;
            string objName = sr.gameObject.name.ToLowerInvariant();
            if (objName == "_surfacecap") continue;

            string spriteName = sr.sprite != null ? sr.sprite.name.ToLowerInvariant() : "";

            bool isBridge = objName.StartsWith("bridgeconnector_");
            bool isSafeBackdrop = objName.StartsWith("safechamber_") && objName.EndsWith("_backdrop");
            bool isSafeBackWall = objName.StartsWith("safechamber_") && objName.EndsWith("_backwall");
            bool isSafePad = !isBridge && !isSafeBackdrop && !isSafeBackWall && (
                (objName.StartsWith("safechamber_") && (objName.EndsWith("_floor") || objName.EndsWith("_ceiling"))) ||
                objName.StartsWith("safepad_") ||
                spriteName.Contains("safepad"));

            if (isSafeBackdrop)
            {
                if (safePadSubSprite != null)
                    ApplyRimlessTiled(sr, safePadSubSprite, new Color(0.26f, 0.32f, 0.42f, 0.96f));
                continue;
            }

            if (isSafeBackWall)
            {
                if (safePadSubSprite != null)
                    ApplyRimlessTiled(sr, safePadSubSprite, Color.white);
                continue;
            }

            if (isSafePad)
            {
                if (safePadSprite != null)
                {
                    sr.color = Color.white;
                    ApplySegmentedGround(sr, safePadSprite, safePadSubSprite, 2.0f);
                }
                continue;
            }

            if (objName.Contains("_support_"))
            {
                if (groundSubSprite != null)
                    ApplyRimlessTiled(sr, groundSubSprite, Color.white);
                continue;
            }

            bool isGround = objName.Contains("ground") || spriteName.Contains("ground") ||
                            isBridge || objName.StartsWith("pillar_") ||
                            objName.StartsWith("tower_") || objName.EndsWith("_floor");
            if (isGround)
            {
                if (groundSprite != null)
                {
                    sr.color = Color.white;
                    ApplySegmentedGround(sr, groundSprite, groundSubSprite, 1.0f);
                }
            }
            else if (objName.Contains("platform_oneway") || spriteName.Contains("platform_oneway") || objName.Contains("oneway"))
            {
                if (oneWaySprite != null) ApplyTiledSprite(sr, oneWaySprite);
            }
            else if (objName.Contains("platform_moving") || spriteName.Contains("platform_moving") || objName.Contains("moving"))
            {
                if (movingSprite != null) ApplyTiledSprite(sr, movingSprite);
            }
            else if (objName.Contains("spike") || objName.Contains("hazard") || spriteName.Contains("spike") || spriteName.Contains("hazard"))
            {
                if (hazardSprite != null) ApplyTiledSprite(sr, hazardSprite);
            }
        }
    }

    public static void ApplyRimlessTiled(SpriteRenderer sr, Sprite rimlessSprite, Color tint)
    {
        if (sr == null || rimlessSprite == null) return;
        RemoveSurfaceCap(sr.transform);
        Vector2 savedSize = sr.size;
        Vector3 savedScale = sr.transform.localScale;
        var savedDrawMode = sr.drawMode;

        sr.sprite = rimlessSprite;
        sr.color = tint;

        if (savedDrawMode == SpriteDrawMode.Sliced || savedDrawMode == SpriteDrawMode.Tiled)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = savedSize;
            sr.transform.localScale = savedScale;
        }
        else
        {
            sr.drawMode = SpriteDrawMode.Simple;
            sr.transform.localScale = savedScale;
        }
    }

    public static void ApplySegmentedGround(SpriteRenderer sr, Sprite topSprite, Sprite subSprite, float capHeight)
    {
        if (sr == null || topSprite == null) return;
        if (subSprite == null) subSprite = topSprite;

        var savedDrawMode = sr.drawMode;
        Vector2 savedSize = sr.size;
        Vector3 savedScale = sr.transform.localScale;

        if (savedDrawMode == SpriteDrawMode.Sliced || savedDrawMode == SpriteDrawMode.Tiled)
        {
            if (savedSize.y > capHeight + 0.1f)
            {
                Transform existingCap = sr.transform.Find("_SurfaceCap");
                SpriteRenderer existingCapSr = existingCap != null ? existingCap.GetComponent<SpriteRenderer>() : null;
                int capOrder = existingCapSr != null ? existingCapSr.sortingOrder : sr.sortingOrder;
                int subOrder = Mathf.Min(sr.sortingOrder, capOrder - 1);

                sr.sprite = subSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = savedSize;
                sr.transform.localScale = savedScale;
                sr.sortingOrder = subOrder;

                GameObject capGo;
                SpriteRenderer capSr;
                if (existingCap != null)
                {
                    capGo = existingCap.gameObject;
                    capGo.SetActive(true);
                    capSr = existingCapSr ?? capGo.AddComponent<SpriteRenderer>();
                }
                else
                {
                    capGo = new GameObject("_SurfaceCap");
                    capGo.transform.SetParent(sr.transform, false);
                    capSr = capGo.AddComponent<SpriteRenderer>();
                }

                capGo.layer = sr.gameObject.layer;
                capGo.transform.localRotation = Quaternion.identity;
                capGo.transform.localScale = Vector3.one;
                capGo.transform.localPosition = new Vector3(0f, savedSize.y * 0.5f - capHeight * 0.5f, 0f);

                capSr.sprite = topSprite;
                capSr.drawMode = SpriteDrawMode.Tiled;
                capSr.tileMode = SpriteTileMode.Continuous;
                capSr.size = new Vector2(savedSize.x, capHeight);
                capSr.color = sr.color;
                capSr.sortingLayerID = sr.sortingLayerID;
                capSr.sortingOrder = capOrder;
            }
            else
            {
                RemoveSurfaceCap(sr.transform);
                sr.sprite = topSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = savedSize;
                sr.transform.localScale = savedScale;
            }
        }
        else
        {
            RemoveSurfaceCap(sr.transform);
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sprite = sr.bounds.size.y > capHeight + 0.1f ? subSprite : topSprite;
            sr.transform.localScale = savedScale;
        }
    }

    public static void SyncSurfaceCapPosition(GameObject go)
    {
        if (go == null) return;
        Transform cap = go.transform.Find("_SurfaceCap");
        if (cap == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        var capSr = cap.GetComponent<SpriteRenderer>();
        if (sr == null || capSr == null) return;

        float capHeight = Mathf.Max(0.25f, capSr.size.y);
        cap.localPosition = new Vector3(0f, sr.size.y * 0.5f - capHeight * 0.5f, 0f);
        capSr.size = new Vector2(sr.size.x, capHeight);
    }

    private static void RemoveSurfaceCap(Transform parent)
    {
        if (parent == null) return;
        Transform existing = parent.Find("_SurfaceCap");
        if (existing == null) return;
        existing.gameObject.SetActive(false);
        if (Application.isPlaying)
            Object.Destroy(existing.gameObject);
        else
            Object.DestroyImmediate(existing.gameObject);
    }

    private static void ApplyTiledSprite(SpriteRenderer sr, Sprite sprite)
    {
        Vector2 savedSize = sr.size;
        var savedDrawMode = sr.drawMode;
        Vector3 savedScale = sr.transform.localScale;

        sr.sprite = sprite;
        sr.color = Color.white;

        if (savedDrawMode == SpriteDrawMode.Sliced || savedDrawMode == SpriteDrawMode.Tiled)
        {
            sr.drawMode = savedDrawMode;
            sr.size = savedSize;
            sr.transform.localScale = savedScale;
        }
        else
        {
            sr.drawMode = SpriteDrawMode.Simple;
            sr.transform.localScale = savedScale;
        }
    }
}
