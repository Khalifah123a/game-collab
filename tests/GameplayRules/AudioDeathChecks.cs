using System;
using System.IO;

public static class AudioDeathChecks
{
    public static int Passed;

    public static int Main(string[] args)
    {
        string repoRoot = args != null && args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        string audioDir = Path.Combine(repoRoot, "packet-courier/Assets/Resources/Audio");
        string artDir = Path.Combine(repoRoot, "packet-courier/Assets/Resources/GeneratedArt");

        Run("audio directory exists", () => {
            Assert(Directory.Exists(audioDir), "Audio directory not found: " + audioDir);
        });

        string[] requiredClips = {
            "sfx_npc_talk.wav", "sfx_interact.wav", "sfx_jump.wav", "sfx_jump_air.wav",
            "sfx_dash.wav", "sfx_hurt.wav", "sfx_attack.wav", "sfx_shoot.wav",
            "sfx_parry.wav", "sfx_collect.wav", "sfx_death.wav", "sfx_ui_open.wav", "sfx_ui_click.wav"
        };

        foreach (var clip in requiredClips)
        {
            string captured = clip;
            Run("valid wav format for " + captured, () => {
                string filePath = Path.Combine(audioDir, captured);
                Assert(File.Exists(filePath), "Clip missing: " + captured);
                byte[] bytes = File.ReadAllBytes(filePath);
                Assert(bytes.Length > 44, "File too small to be WAV: " + captured);
                // RIFF header
                string riff = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
                string wave = System.Text.Encoding.ASCII.GetString(bytes, 8, 4);
                Assert(riff == "RIFF", "Header is not RIFF for " + captured);
                Assert(wave == "WAVE", "Format is not WAVE for " + captured);
            });
        }

        Run("death card background sprite exists with sliced border", () => {
            string bgPath = Path.Combine(artDir, "death_card_bg.png");
            string metaPath = bgPath + ".meta";
            Assert(File.Exists(bgPath), "death_card_bg.png missing");
            Assert(File.Exists(metaPath), "death_card_bg.png.meta missing");
            string metaContent = File.ReadAllText(metaPath);
            Assert(metaContent.Contains("spriteBorder:"), "meta missing 9-slice spriteBorder definition");
        });

        Console.WriteLine("PASS: " + Passed + " audio and death screen checks");
        return 0;
    }

    private static void Run(string label, Action body)
    {
        try { body(); }
        catch (Exception e) { Console.WriteLine("FAIL: " + label + " -> " + e.Message); throw; }
        Passed++;
        Console.WriteLine("PASS: " + label);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
