using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handcrafted pixel-art UI kit for Packet Courier.
/// Provides custom pixel icons, multi-colour parcel &amp; corrupted-skull emblems,
/// stepped-chamfer 9-sliceable frames, dynamic pixel/mono font loading,
/// tactile multi-state buttons (Normal / Hover / Pressed), and unscaled sprite animation.
/// </summary>
public static class PixelUIKit
{
    private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
    private static Font _displayFont;
    private static Font _monoFont;

    // Palette constants matched to Packet Courier pixel sprites
    public static readonly Color BgDeep = new Color(0.031f, 0.047f, 0.078f, 1f);         // #080C14
    public static readonly Color CardDark = new Color(0.055f, 0.086f, 0.149f, 0.96f);    // #0E1626
    public static readonly Color CardCrimson = new Color(0.055f, 0.039f, 0.078f, 0.97f); // #0E0A14
    public static readonly Color BorderSlate = new Color(0.137f, 0.208f, 0.329f, 1f);    // #233554
    public static readonly Color Cyan = new Color(0.0f, 0.941f, 1.0f, 1f);               // #00F0FF
    public static readonly Color CyanDark = new Color(0.039f, 0.161f, 0.212f, 1f);       // #0A2936
    public static readonly Color Orange = new Color(0.941f, 0.478f, 0.165f, 1f);         // #F07A2A
    public static readonly Color OrangeLight = new Color(1.0f, 0.624f, 0.263f, 1f);      // #FF9F43
    public static readonly Color Mint = new Color(0.212f, 0.961f, 0.608f, 1f);           // #36F59B
    public static readonly Color Crimson = new Color(1.0f, 0.165f, 0.333f, 1f);          // #FF2A55
    public static readonly Color CrimsonSoft = new Color(1.0f, 0.42f, 0.545f, 1f);       // #FF6B8B
    public static readonly Color TextWhite = new Color(0.945f, 0.961f, 0.976f, 1f);      // #F1F5F9
    public static readonly Color TextMuted = new Color(0.549f, 0.635f, 0.753f, 1f);      // #8CA2C0
    public static readonly Color TextDim = new Color(0.392f, 0.455f, 0.545f, 1f);        // #64748B

    private static readonly Dictionary<string, string[]> IconBitmaps = new Dictionary<string, string[]>
    {
        ["play"] = new[] {
            "............", "...##.......", "...###......", "...####.....",
            "...#####....", "...######...", "...######...", "...#####....",
            "...####.....", "...###......", "...##.......", "............"
        },
        ["route"] = new[] {
            "###.........", "#.#.........", "###.........", ".#..........",
            ".#####......", ".....#......", ".....#......", ".....#####..",
            ".........#..", "........###.", "........###.", "........###."
        },
        ["keys"] = new[] {
            "............", "............", "############", "#..........#",
            "#.#.#.#.#..#", "#..........#", "#..#.#.#.#.#", "#..........#",
            "#..######..#", "#..........#", "############", "............"
        },
        ["power"] = new[] {
            ".....##.....", "..##.##.##..", ".##..##..##.", "##...##...##",
            "##...##...##", "##........##", "##........##", "##........##",
            ".##......##.", ".###....###.", "..########..", "....####...."
        },
        ["retry"] = new[] {
            "....####....", "..########..", ".###....###.", ".##......##.",
            "##.....#####", "##......###.", "##.......#..", "##..........",
            ".##......##.", ".###....###.", "..########..", "....####...."
        },
        ["restart"] = new[] {
            "............", ".##.....##..", ".##....###..", ".##...####..",
            ".##..#####..", ".##.######..", ".##.######..", ".##..#####..",
            ".##...####..", ".##....###..", ".##.....##..", "............"
        },
        ["home"] = new[] {
            ".....##.....", "....####....", "...######...", "..########..",
            ".##########.", "############", "..########..", "..##....##..",
            "..##....##..", "..##....##..", "..##....##..", "..########.."
        },
        ["lock"] = new[] {
            "............", "....####....", "...##..##...", "...#....#...",
            "...#....#...", "..########..", "..########..", "..###..###..",
            "..###..###..", "..####.###..", "..########..", "............"
        },
        ["check"] = new[] {
            "............", "............", "..........##", ".........##.",
            "........##..", ".##....##...", "..##..##....", "...####.....",
            "....##......", "............", "............", "............"
        },
        ["back"] = new[] {
            "............", ".......##...", "......##....", ".....##.....",
            "....##......", "...##.......", "...##.......", "....##......",
            ".....##.....", "......##....", ".......##...", "............"
        },
        ["bolt"] = new[] {
            "......####..", ".....####...", "....####....", "...#######..",
            "..########..", ".....####...", "....####....", "...####.....",
            "..####......", ".###........", "##..........", "............"
        },
        ["skull"] = new[] {
            "...######...", "..########..", ".##########.", ".##..##..##.",
            ".##..##..##.", ".##########.", "..###..###..", "..########..",
            "...#.##.#...", "...#.##.#...", "............", "............"
        },
        ["shield"] = new[] {
            "..########..", ".##########.", ".##......##.", ".##..##..##.",
            ".##..##..##.", ".##......##.", "..##....##..", "...##..##...",
            "....####....", ".....##.....", "............", "............"
        },
        ["terminal"] = new[] {
            "############", "#..........#", "#.##.......#", "#..##......#",
            "#...##.....#", "#..##......#", "#.##.......#", "#.....####.#",
            "#..........#", "############", "............", "............"
        }
    };

    private static readonly string[] ParcelLogoBitmap = {
        "................",
        "......KKKKKKKKK.",
        ".....KLLLTTLLLKK",
        "....KLLLTTLLLKDK",
        "...KKKKKKKKKKDDK",
        "TT.KCCCCTTCCKDDK",
        "...KCCCCTTCCKDDK",
        ".TTKCCCCTTCCKDDK",
        "...KCCCCTTCCKDDK",
        "TT.KCCCCTTCCKDK.",
        "...KCCCCTTCCKK..",
        "...KKKKKKKKKK...",
        "................",
        "................"
    };

    private static readonly string[] CorruptEmblemBitmap = {
        "....KKKKKKKK....",
        "..KKRRRRRRRRKK..",
        ".KRRLLLLRRRRRRK.",
        ".KRRLKKLRRKKRRK.",
        "KRRRLKKLRRKKRRRK",
        "KRRRRRRRRRRRRRRK",
        "KRRRKKRRRRKKRRRK",
        ".KRRRRKKKKRRRRK.",
        ".KKRRRRRRRRRRKK.",
        "..KRRKRRKRRKRRK.",
        "..KKKKKKKKKKKK..",
        ".TT..K.KK.K..TT.",
        "TT............TT",
        "................"
    };

    public static Font GetDisplayFont()
    {
        if (_displayFont != null) return _displayFont;
        _displayFont = Resources.Load<Font>("UI/Fonts/Silkscreen-Bold");
        if (_displayFont == null) _displayFont = Resources.Load<Font>("UI/Fonts/Silkscreen-Regular");
        if (_displayFont == null)
        {
            _displayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return _displayFont;
    }

    public static Font GetMonoFont()
    {
        if (_monoFont != null) return _monoFont;
        _monoFont = Resources.Load<Font>("UI/Fonts/JetBrainsMono-Bold");
        if (_monoFont == null) _monoFont = Resources.Load<Font>("UI/Fonts/JetBrainsMono-Regular");
        if (_monoFont == null)
        {
            _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return _monoFont;
    }

    private static Sprite LoadResourceSpriteOrTex(string resourcePath, int sliceBorder = 0)
    {
        Sprite spr = Resources.Load<Sprite>(resourcePath);
        if (spr != null && spr.texture != null)
        {
            spr.texture.filterMode = FilterMode.Point;
            if (sliceBorder <= 0 || spr.border.sqrMagnitude > 0f)
                return spr;
        }

        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            Vector4 b = sliceBorder > 0 ? new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }
        return spr;
    }

    public static Sprite GetIcon(string name)
    {
        string key = "icon_" + name;
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Sprite loaded = LoadResourceSpriteOrTex("UI/Sprites/icon_" + name, 0);
        if (loaded != null)
        {
            _spriteCache[key] = loaded;
            return loaded;
        }

        if (!IconBitmaps.TryGetValue(name, out string[] bmp)) bmp = IconBitmaps["play"];
        var palette = new Dictionary<char, Color32> { ['#'] = new Color32(255, 255, 255, 255) };
        Sprite generated = BuildBitmapSprite(bmp, palette, 4);
        _spriteCache[key] = generated;
        return generated;
    }

    public static Sprite GetParcelLogo()
    {
        const string key = "logo_parcel";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Sprite loaded = LoadResourceSpriteOrTex("UI/Sprites/logo_parcel", 0);
        if (loaded != null)
        {
            _spriteCache[key] = loaded;
            return loaded;
        }

        var palette = new Dictionary<char, Color32>
        {
            ['K'] = new Color32(20, 16, 12, 255),
            ['C'] = new Color32(227, 174, 120, 255),
            ['L'] = new Color32(243, 207, 164, 255),
            ['D'] = new Color32(176, 122, 76, 255),
            ['T'] = new Color32(240, 122, 42, 255)
        };
        Sprite generated = BuildBitmapSprite(ParcelLogoBitmap, palette, 6);
        _spriteCache[key] = generated;
        return generated;
    }

    public static Sprite GetCorruptLogo()
    {
        const string key = "logo_corrupt";
        if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Sprite loaded = LoadResourceSpriteOrTex("UI/Sprites/logo_corrupt", 0);
        if (loaded == null) loaded = LoadResourceSpriteOrTex("GeneratedArt/death_icon_corrupt", 0);
        if (loaded != null)
        {
            _spriteCache[key] = loaded;
            return loaded;
        }

        var palette = new Dictionary<char, Color32>
        {
            ['K'] = new Color32(20, 10, 14, 255),
            ['R'] = new Color32(255, 42, 85, 255),
            ['L'] = new Color32(255, 143, 163, 255),
            ['T'] = new Color32(0, 240, 255, 255)
        };
        Sprite generated = BuildBitmapSprite(CorruptEmblemBitmap, palette, 6);
        _spriteCache[key] = generated;
        return generated;
    }

    public static Sprite GetSteppedSprite(string cacheKey, int w, int h, Color32 fill, Color32 border, Color32 bottomLip, int step = 3, int sliceBorder = 10)
    {
        if (_spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null) return cached;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[w * h];
        int k = step * 2;

        bool Inside(int x, int y)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return false;
            int rx = w - 1 - x;
            int ry = h - 1 - y;
            if ((x < step && y < k) || (x < k && y < step)) return false;
            if ((rx < step && y < k) || (rx < k && y < step)) return false;
            if ((x < step && ry < k) || (x < k && ry < step)) return false;
            if ((rx < step && ry < k) || (rx < k && ry < step)) return false;
            return true;
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!Inside(x, y))
                {
                    pixels[y * w + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                bool edge = !Inside(x - 2, y) || !Inside(x + 2, y) || !Inside(x, y - 2) || !Inside(x, y + 2)
                            || !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
                if (edge)
                {
                    pixels[y * w + x] = border;
                }
                else if (bottomLip.a > 0 && y < 6)
                {
                    pixels[y * w + x] = bottomLip;
                }
                else
                {
                    pixels[y * w + x] = fill;
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Vector4 borderVec = sliceBorder > 0 ? new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder) : Vector4.zero;
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borderVec);
        _spriteCache[cacheKey] = sprite;
        return sprite;
    }

    private static Sprite BuildBitmapSprite(string[] rows, Dictionary<char, Color32> palette, int scale)
    {
        int hRows = rows.Length;
        int wCols = rows[0].Length;
        int w = wCols * scale;
        int h = hRows * scale;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[w * h];
        for (int r = 0; r < hRows; r++)
        {
            string row = rows[r];
            int ty = (hRows - 1 - r) * scale;
            for (int c = 0; c < wCols; c++)
            {
                char ch = row[c];
                if (ch == '.' || !palette.TryGetValue(ch, out Color32 col)) continue;
                int tx = c * scale;
                for (int dy = 0; dy < scale; dy++)
                {
                    for (int dx = 0; dx < scale; dx++)
                    {
                        pixels[(ty + dy) * w + (tx + dx)] = col;
                    }
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Image CreateSteppedPanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color32 fill, Color32 border, Color32 lip, int step = 4)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        string key = $"panel_{fill.r}_{fill.g}_{fill.b}_{border.r}_{border.g}_{border.b}_{lip.a}_{step}";
        img.sprite = GetSteppedSprite(key, 64, 64, fill, border, lip, step, 12);
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        return img;
    }

    public static Text CreatePixelLabel(Transform parent, string name, string text, Vector2 aMin, Vector2 aMax, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool mono = false, FontStyle style = FontStyle.Bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = mono ? GetMonoFont() : GetDisplayFont();
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = color;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 6;
        txt.resizeTextMaxSize = fontSize;
        return txt;
    }

    public enum ButtonTheme { PrimaryCyan, SecondarySlate, DangerCrimson }

    public static Button CreateTactileButton(
        Transform parent,
        string iconName,
        string title,
        string subtitle,
        string keycapText,
        Vector2 aMin,
        Vector2 aMax,
        ButtonTheme theme,
        UnityEngine.Events.UnityAction onClick)
    {
        Sprite normalSpr, hoverSpr, pressSpr;
        Color titleNormal, titleHover, subNormal, iconTintNormal, iconBoxBg, keycapTextCol;

        if (theme == ButtonTheme.PrimaryCyan)
        {
            normalSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_primary_normal", 12)
                        ?? GetSteppedSprite("btn_cyan_n", 96, 64, new Color32(0, 216, 240, 255), new Color32(128, 250, 255, 255), new Color32(0, 122, 153, 255), 3, 10);
            hoverSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_primary_hover", 12)
                       ?? GetSteppedSprite("btn_cyan_h", 96, 64, new Color32(77, 248, 255, 255), new Color32(255, 255, 255, 255), new Color32(0, 168, 198, 255), 3, 10);
            pressSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_primary_pressed", 12)
                       ?? GetSteppedSprite("btn_cyan_p", 96, 64, new Color32(0, 158, 184, 255), new Color32(0, 240, 255, 255), new Color32(0, 0, 0, 0), 3, 10);
            titleNormal = new Color(0.024f, 0.063f, 0.102f, 1f);
            titleHover = new Color(0.015f, 0.04f, 0.07f, 1f);
            subNormal = new Color(0.039f, 0.161f, 0.212f, 1f);
            iconTintNormal = Cyan;
            iconBoxBg = new Color(0.027f, 0.075f, 0.118f, 1f);
            keycapTextCol = Cyan;
        }
        else if (theme == ButtonTheme.DangerCrimson)
        {
            normalSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_danger_normal", 12)
                        ?? GetSteppedSprite("btn_crim_n", 96, 64, new Color32(33, 13, 24, 255), new Color32(122, 34, 62, 255), new Color32(12, 4, 8, 255), 3, 10);
            hoverSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_danger_hover", 12)
                       ?? GetSteppedSprite("btn_crim_h", 96, 64, new Color32(58, 18, 36, 255), new Color32(255, 42, 85, 255), new Color32(24, 6, 14, 255), 3, 10);
            pressSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_danger_pressed", 12)
                       ?? GetSteppedSprite("btn_crim_p", 96, 64, new Color32(22, 8, 15, 255), new Color32(255, 107, 139, 255), new Color32(0, 0, 0, 0), 3, 10);
            titleNormal = CrimsonSoft;
            titleHover = Color.white;
            subNormal = new Color(0.72f, 0.42f, 0.51f, 1f);
            iconTintNormal = CrimsonSoft;
            iconBoxBg = new Color(0.21f, 0.08f, 0.15f, 1f);
            keycapTextCol = CrimsonSoft;
        }
        else
        {
            normalSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_secondary_normal", 12)
                        ?? GetSteppedSprite("btn_slate_n", 96, 64, new Color32(16, 26, 44, 255), new Color32(41, 64, 102, 255), new Color32(6, 10, 18, 255), 3, 10);
            hoverSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_secondary_hover", 12)
                       ?? GetSteppedSprite("btn_slate_h", 96, 64, new Color32(26, 44, 74, 255), new Color32(0, 240, 255, 255), new Color32(10, 18, 32, 255), 3, 10);
            pressSpr = LoadResourceSpriteOrTex("UI/Sprites/btn_secondary_pressed", 12)
                       ?? GetSteppedSprite("btn_slate_p", 96, 64, new Color32(10, 17, 30, 255), new Color32(240, 122, 42, 255), new Color32(0, 0, 0, 0), 3, 10);
            titleNormal = TextWhite;
            titleHover = Color.white;
            subNormal = TextMuted;
            iconTintNormal = Cyan;
            iconBoxBg = new Color(0.094f, 0.153f, 0.259f, 1f);
            keycapTextCol = TextMuted;
        }

        var rootGo = new GameObject("Btn_" + title.Replace(" ", "_"), typeof(RectTransform), typeof(Image), typeof(Button));
        if (!Application.isPlaying) rootGo.hideFlags = HideFlags.DontSave;
        rootGo.transform.SetParent(parent, false);
        var rt = rootGo.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bgImg = rootGo.GetComponent<Image>();
        bgImg.sprite = normalSpr;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        var btn = rootGo.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = bgImg;
        if (onClick != null)
        {
            btn.onClick.AddListener(() =>
            {
                AudioManager.PlayUIClick();
                onClick.Invoke();
            });
        }

        // Content container (shifts down on press)
        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(rootGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        // Icon Box on Left
        var iconBoxGo = new GameObject("IconBox", typeof(RectTransform), typeof(Image));
        iconBoxGo.transform.SetParent(contentGo.transform, false);
        var iconBoxRt = iconBoxGo.GetComponent<RectTransform>();
        iconBoxRt.anchorMin = new Vector2(0.025f, 0.18f);
        iconBoxRt.anchorMax = new Vector2(0.115f, 0.84f);
        iconBoxRt.offsetMin = Vector2.zero;
        iconBoxRt.offsetMax = Vector2.zero;
        var iconBoxImg = iconBoxGo.GetComponent<Image>();
        iconBoxImg.color = iconBoxBg;
        iconBoxImg.raycastTarget = false;

        var iconGo = new GameObject("IconSprite", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(iconBoxGo.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.2f, 0.2f);
        iconRt.anchorMax = new Vector2(0.8f, 0.8f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = GetIcon(iconName);
        iconImg.preserveAspect = true;
        iconImg.color = iconTintNormal;
        iconImg.raycastTarget = false;

        // Title & Subtitle
        bool hasSub = !string.IsNullOrEmpty(subtitle);
        bool hasKey = !string.IsNullOrEmpty(keycapText);
        float textRight = hasKey ? 0.75f : 0.95f;
        var titleTxt = CreatePixelLabel(
            contentGo.transform,
            "Title",
            title,
            new Vector2(0.14f, hasSub ? 0.46f : 0.14f),
            new Vector2(textRight, hasSub ? 0.90f : 0.88f),
            hasSub ? 17 : 18,
            titleNormal,
            TextAnchor.MiddleLeft,
            false,
            FontStyle.Bold);

        Text subTxt = null;
        if (hasSub)
        {
            subTxt = CreatePixelLabel(
                contentGo.transform,
                "Subtitle",
                subtitle,
                new Vector2(0.14f, 0.10f),
                new Vector2(textRight, 0.44f),
                11,
                subNormal,
                TextAnchor.MiddleLeft,
                true,
                FontStyle.Bold);
        }

        // Keycap Badge on Right
        Image keycapImg = null;
        Text keycapLbl = null;
        if (hasKey)
        {
            var keyGo = new GameObject("Keycap", typeof(RectTransform), typeof(Image));
            keyGo.transform.SetParent(contentGo.transform, false);
            var keyRt = keyGo.GetComponent<RectTransform>();
            keyRt.anchorMin = new Vector2(0.77f, 0.22f);
            keyRt.anchorMax = new Vector2(0.965f, 0.80f);
            keyRt.offsetMin = Vector2.zero;
            keyRt.offsetMax = Vector2.zero;
            keycapImg = keyGo.GetComponent<Image>();
            keycapImg.color = new Color(0.03f, 0.055f, 0.095f, 0.92f);
            keycapImg.raycastTarget = false;

            keycapLbl = CreatePixelLabel(
                keyGo.transform,
                "KeyText",
                keycapText,
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.92f),
                11,
                keycapTextCol,
                TextAnchor.MiddleCenter,
                true,
                FontStyle.Bold);
        }

        if (Application.isPlaying)
        {
            var tactile = rootGo.AddComponent<TactilePixelButton>();
            tactile.hideFlags = HideFlags.DontSave;
            tactile.bgImage = bgImg;
            tactile.contentRt = contentRt;
            tactile.normalSprite = normalSpr;
            tactile.hoverSprite = hoverSpr;
            tactile.pressedSprite = pressSpr;
            tactile.titleText = titleTxt;
            tactile.subText = subTxt;
            tactile.iconImage = iconImg;
            tactile.keycapText = keycapLbl;
            tactile.normalTitleColor = titleNormal;
            tactile.hoverTitleColor = titleHover;
            tactile.pressedTitleColor = theme == ButtonTheme.PrimaryCyan ? new Color(0.02f, 0.05f, 0.08f, 1f) : OrangeLight;
        }

        return btn;
    }
}
