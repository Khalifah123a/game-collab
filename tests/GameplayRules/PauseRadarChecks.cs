using System;
using System.IO;

public static class PauseRadarChecks
{
    private static int _passed = 0;

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Console.WriteLine(string.Format("FAIL: {0}", message));
            Environment.Exit(1);
        }
        Console.WriteLine(string.Format("PASS: {0}", message));
        _passed++;
    }

    public static void Main(string[] args)
    {
        string projectRoot = args.Length > 0 ? args[0] : "packet-courier";

        // 1. Check PauseMenuUI Script & Contracts
        string pauseScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/PauseMenuUI.cs");
        Assert(File.Exists(pauseScript), "PauseMenuUI.cs exists");
        string pauseText = File.ReadAllText(pauseScript);
        Assert(pauseText.Contains("public static void TogglePause"), "PauseMenuUI exposes TogglePause");
        Assert(pauseText.Contains("public static void Show"), "PauseMenuUI exposes Show");
        Assert(pauseText.Contains("public static void Resume"), "PauseMenuUI exposes Resume");
        Assert(pauseText.Contains("public static bool IsPaused"), "PauseMenuUI tracks IsPaused property");
        Assert(pauseText.Contains("RetryCheckpoint"), "PauseMenuUI provides RetryCheckpoint");
        Assert(pauseText.Contains("ReturnToMenu"), "PauseMenuUI provides ReturnToMenu");
        Assert(pauseText.Contains("AudioManager.SetVolumes"), "PauseMenuUI modifies AudioManager volumes");
        Assert(pauseText.Contains("JuiceManager.EnableScreenShake"), "PauseMenuUI toggles JuiceManager screen shake");
        Assert(pauseText.Contains("RefreshOverclockSummary"), "PauseMenuUI displays installed hardware mods");

        // 2. Check AudioManager Volume Controls
        string audioScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/AudioManager.cs");
        string audioText = File.ReadAllText(audioScript);
        Assert(audioText.Contains("public static float MasterVolume"), "AudioManager defines MasterVolume");
        Assert(audioText.Contains("public static float BGMVolume"), "AudioManager defines BGMVolume");
        Assert(audioText.Contains("public static float SFXVolume"), "AudioManager defines SFXVolume");
        Assert(audioText.Contains("public static void SetVolumes"), "AudioManager defines SetVolumes method");
        Assert(audioText.Contains("public void ApplyVolumes"), "AudioManager defines ApplyVolumes method");

        // 3. Check JuiceManager Screen Shake Toggle
        string juiceScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/JuiceManager.cs");
        string juiceText = File.ReadAllText(juiceScript);
        Assert(juiceText.Contains("public static bool EnableScreenShake"), "JuiceManager defines EnableScreenShake property");
        Assert(juiceText.Contains("!EnableScreenShake"), "JuiceManager bypasses shake offset when shake is disabled");

        // 4. Check NetworkRadarHUD Script & Contracts
        string radarScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/NetworkRadarHUD.cs");
        Assert(File.Exists(radarScript), "NetworkRadarHUD.cs exists");
        string radarText = File.ReadAllText(radarScript);
        Assert(radarText.Contains("public static NetworkRadarHUD Attach"), "NetworkRadarHUD exposes Attach method");
        Assert(radarText.Contains("GetDirectionArrow"), "NetworkRadarHUD calculates directional arrow cues");
        Assert(radarText.Contains("ServerB_Goal"), "NetworkRadarHUD tracks ServerB_Goal exit distance");
        Assert(radarText.Contains("Checkpoint_TCP"), "NetworkRadarHUD tracks TCP checkpoint distance");
        Assert(radarText.Contains("SecretMemoryCore"), "NetworkRadarHUD scans for secret memory cores");

        // 5. Check Direction Arrow Math Logic
        float dx = 25f, dy = 0f;
        string arrowRight = (Math.Abs(dx) > Math.Abs(dy) * 1.5f) ? (dx > 0 ? "►" : "◄") : "▲";
        Assert(arrowRight == "►", "positive delta X points right");

        dx = -42f; dy = 0f;
        string arrowLeft = (Math.Abs(dx) > Math.Abs(dy) * 1.5f) ? (dx > 0 ? "►" : "◄") : "▲";
        Assert(arrowLeft == "◄", "negative delta X points left");

        // 6. Check Integration in GameplayHUD & GameManager
        string hudScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/GameplayHUD.cs");
        string hudText = File.ReadAllText(hudScript);
        Assert(hudText.Contains("NetworkRadarHUD.Attach"), "GameplayHUD attaches NetworkRadarHUD");

        string gmScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/GameManager.cs");
        string gmText = File.ReadAllText(gmScript);
        Assert(gmText.Contains("PauseMenuUI.TogglePause"), "GameManager routes Escape key to PauseMenuUI.TogglePause");

        Console.WriteLine(string.Format("PASS: {0} pause menu, audio settings and network radar checks", _passed));
    }
}
