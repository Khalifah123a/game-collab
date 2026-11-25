using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rasterizes crisp FilterMode.Point pixel-polygon HUD chassis plates, skewed -14 deg energy cells,
/// octagonal ability sockets, clamp teeth, and fallback pixel icons matching the FigCraft v2 HUD spec.
/// </summary>
public static class TacticalHUDGraphics
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    // Core v2 Tactical Cyber Palette
    public static readonly Color32 ColChassisDark    = new Color32(7, 15, 28, 242);     // #070F1CF2
    public static readonly Color32 ColBevelShadow    = new Color32(2, 4, 8, 225);       // #020408E1
    public static readonly Color32 ColBorderSteel    = new Color32(30, 58, 95, 255);    // #1E3A5F
    public static readonly Color32 ColNeonMint       = new Color32(0, 255, 163, 255);   // #00FFA3
    public static readonly Color32 ColElectricCyan   = new Color32(0, 229, 255, 255);   // #00E5FF
    public static readonly Color32 ColSkyDash        = new Color32(56, 189, 248, 255);  // #38BDF8
    public static readonly Color32 ColDashRecharging = new Color32(2, 132, 199, 255);   // #0284C7
    public static readonly Color32 ColGold           = new Color32(255, 215, 0, 255);   // #FFD700
    public static readonly Color32 ColAmber          = new Color32(255, 179, 0, 255);   // #FFB300
    public static readonly Color32 ColGhostCream     = new Color32(254, 249, 195, 255); // #FEF9C3
    public static readonly Color32 ColGhostGold      = new Color32(250, 204, 21, 255);  // #FACC15
    public static readonly Color32 ColCrimson        = new Color32(255, 51, 102, 255);  // #FF3366
    public static readonly Color32 ColMagenta        = new Color32(217, 70, 239, 255);  // #D946EF
    public static readonly Color32 ColVioletQuiz     = new Color32(232, 121, 249, 255); // #E879F9
    public static readonly Color32 ColCellEmptyFill  = new Color32(12, 22, 38, 235);    // #0C1626
    public static readonly Color32 ColCellEmptyRim   = new Color32(22, 40, 66, 255);    // #162842

    public static Sprite LoadSprite(string resourcePath)
    {
        if (Cache.TryGetValue("res:" + resourcePath, out Sprite cached) && cached != null)
            return cached;

        Sprite spr = Resources.Load<Sprite>(resourcePath);
        if (spr != null && spr.texture != null)
        {
            spr.texture.filterMode = FilterMode.Point;
            Cache["res:" + resourcePath] = spr;
            return spr;
        }

        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            var created = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Cache["res:" + resourcePath] = created;
            return created;
        }

        return null;
    }

    public static Sprite GetWhitePixel()
    {
        const string key = "white_1x1";
        if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(new[]
        {
            new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
            new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255)
        });
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = s;
        return s;
    }

    /// <summary>
    /// Rasterizes a convex/concave polygon defined in top-left origin coordinates (matching FigCraft SVG paths)
    /// into a crisp FilterMode.Point Sprite with pixel-exact border and optional top specular strip.
    /// </summary>
    public static Sprite GetPolygonSprite(
        string key,
        int width,
        int height,
        Vector2[] topDownPoints,
        Color32 fill,
        Color32 border,
        int borderPx = 2,
        Color32 topSpecular = default,
        int topSpecPx = 0)
    {
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        bool[] insideMask = new bool[width * height];
        for (int py = 0; py < height; py++)
        {
            float figY = (height - 1 - py) + 0.5f;
            for (int px = 0; px < width; px++)
            {
                float figX = px + 0.5f;
                insideMask[py * width + px] = PointInPolygon(figX, figY, topDownPoints);
            }
        }

        var pixels = new Color32[width * height];
        for (int py = 0; py < height; py++)
        {
            int figYInt = height - 1 - py;
            for (int px = 0; px < width; px++)
            {
                int idx = py * width + px;
                if (!insideMask[idx])
                {
                    pixels[idx] = new Color32(0, 0, 0, 0);
                    continue;
                }

                bool isBorder = false;
                if (borderPx > 0 && border.a > 0)
                {
                    for (int dy = -borderPx; dy <= borderPx && !isBorder; dy++)
                    {
                        for (int dx = -borderPx; dx <= borderPx; dx++)
                        {
                            if (dx * dx + dy * dy > borderPx * borderPx + 1) continue;
                            int nx = px + dx;
                            int ny = py + dy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height || !insideMask[ny * width + nx])
                            {
                                isBorder = true;
                                break;
                            }
                        }
                    }
                }

                if (isBorder)
                {
                    pixels[idx] = border;
                }
                else if (topSpecPx > 0 && topSpecular.a > 0 && figYInt < topSpecPx + borderPx)
                {
                    pixels[idx] = topSpecular;
                }
                else
                {
                    pixels[idx] = fill;
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = sprite;
        return sprite;
    }

    public static Sprite GetCirclePipSprite(string key, int size, Color32 fill)
    {
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        float r = size * 0.5f - 0.3f;
        float cx = (size - 1) * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cx;
                pixels[y * size + x] = (dx * dx + dy * dy <= r * r) ? fill : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = spr;
        return spr;
    }

    public static Sprite GetTTLEmblemCrest()
    {
        Sprite res = LoadSprite("UI/Sprites/ttl_emblem_crest");
        if (res != null) return res;

        // Crisp procedural fallback matching ttl_emblem_crest.png
        const string key = "proc_ttl_crest";
        if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;

        int n = 25;
        int c = 12;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                int d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                Color32 col = new Color32(0, 0, 0, 0);
                if (d == 12) col = new Color32(5, 14, 24, 255);
                else if (d == 11 || d == 10)
                {
                    bool cardinal = Mathf.Abs(x - c) <= 1 || Mathf.Abs(y - c) <= 1;
                    col = cardinal ? ColGold : ColNeonMint;
                }
                else if (d == 9 || d == 8) col = new Color32(9, 25, 38, 255);
                else if (d == 7 || d == 6) col = ColElectricCyan;
                else if (d <= 5)
                {
                    if (d <= 2) col = new Color32(255, 255, 255, 255);
                    else if (x == c || y == c) col = new Color32(209, 250, 229, 255);
                    else col = ColNeonMint;
                }
                px[y * n + x] = col;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = s;
        return s;
    }

    public static Sprite GetPhaseDashIcon()
    {
        Sprite res = LoadSprite("UI/Sprites/icon_phase_dash");
        if (res != null) return res;
        return PixelUIKit.GetIcon("bolt");
    }

    public static Sprite GetEmergencyBufferIcon()
    {
        Sprite res = LoadSprite("UI/Sprites/icon_emergency_buffer");
        if (res != null) return res;
        return PixelUIKit.GetIcon("shield");
    }

    private static bool PointInPolygon(float x, float y, Vector2[] poly)
    {
        bool inside = false;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = poly[i].x, yi = poly[i].y;
            float xj = poly[j].x, yj = poly[j].y;

            bool intersect = ((yi > y) != (yj > y)) &&
                             (x < (xj - xi) * (y - yi) / ((yj - yi) + 1e-6f) + xi);
            if (intersect) inside = !inside;
        }
        return inside;
    }
}
