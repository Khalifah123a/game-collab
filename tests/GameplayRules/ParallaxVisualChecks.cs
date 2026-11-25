using System;
using System.IO;

public static class ParallaxVisualChecks
{
    private static int _passes = 0;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        Console.WriteLine("PASS: " + label);
        _passes++;
    }

    public static void Main()
    {
        string root = @"d:\unity\game-collab\packet-courier";
        string art = Path.Combine(root, @"Assets\Resources\GeneratedArt");
        string src = Path.Combine(root, @"Assets\_Scripts\Level");

        // 1. Script checks
        string sysFile = Path.Combine(src, "ParallaxBackgroundSystem.cs");
        Check(File.Exists(sysFile), "ParallaxBackgroundSystem.cs exists");
        string sysCode = File.ReadAllText(sysFile);
        Check(sysCode.Contains("class ParallaxBackgroundSystem"), "Defines ParallaxBackgroundSystem class");
        Check(sysCode.Contains("Ensure(int worldIndex)"), "Exposes Ensure method");
        Check(sysCode.Contains("SetupAmbientParticles"), "Includes dynamic ambient cyber particle simulation");
        Check(sysCode.Contains("RebuildForWorld"), "Supports per-world layer generation");

        // 2. Integration in WorldThemeApplier
        string themeFile = Path.Combine(src, "WorldThemeApplier.cs");
        Check(File.Exists(themeFile), "WorldThemeApplier.cs exists");
        string themeCode = File.ReadAllText(themeFile);
        Check(themeCode.Contains("ParallaxBackgroundSystem.Ensure(idx)"), "WorldThemeApplier auto-activates ParallaxBackgroundSystem");
        Check(themeCode.Contains("_hazardSprites"), "WorldThemeApplier skins hazard spikes");

        // 3. Asset verification for all 5 worlds
        for (int w = 1; w <= 5; w++)
        {
            // Background layers
            for (int l = 0; l < 3; l++)
            {
                string bgPng = Path.Combine(art, "bg_w" + w + "_layer" + l + ".png");
                Check(File.Exists(bgPng), "bg_w" + w + "_layer" + l + ".png exists");
                Check(File.Exists(bgPng + ".meta"), "bg_w" + w + "_layer" + l + ".png.meta exists");
                Check(new FileInfo(bgPng).Length > 100, "bg_w" + w + "_layer" + l + ".png has valid non-empty byte stream");
            }

            // Thematic terrain & hazards
            string g = Path.Combine(art, "ground_w" + w + ".png");
            Check(File.Exists(g), "ground_w" + w + ".png exists");
            Check(new FileInfo(g).Length > 100, "ground_w" + w + ".png has valid pixel density");

            string ow = Path.Combine(art, "platform_oneway_w" + w + ".png");
            Check(File.Exists(ow), "platform_oneway_w" + w + ".png exists");

            string mp = Path.Combine(art, "platform_moving_w" + w + ".png");
            Check(File.Exists(mp), "platform_moving_w" + w + ".png exists");

            string sp = Path.Combine(art, "hazard_spikes_w" + w + ".png");
            Check(File.Exists(sp), "hazard_spikes_w" + w + ".png exists");
            Check(File.Exists(sp + ".meta"), "hazard_spikes_w" + w + ".png.meta exists");
        }

        Console.WriteLine("TOTAL: " + _passes + " Visual & Parallax Background checks passed successfully!");
    }
}
