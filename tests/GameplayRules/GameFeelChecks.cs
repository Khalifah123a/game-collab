using System;
using System.IO;

public static class GameFeelChecks
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

        // 1. Check BGM WAV Files
        string audioDir = Path.Combine(projectRoot, "Assets/Resources/Audio");
        Assert(Directory.Exists(audioDir), "audio directory exists");

        string[] bgmFiles = new string[] { "bgm_safezone.wav", "bgm_traversal.wav", "bgm_boss.wav" };
        foreach (string bgm in bgmFiles)
        {
            string p = Path.Combine(audioDir, bgm);
            Assert(File.Exists(p), string.Format("{0} exists", bgm));
            Assert(File.Exists(p + ".meta"), string.Format("{0}.meta exists", bgm));

            byte[] header = new byte[44];
            using (FileStream fs = File.OpenRead(p))
            {
                fs.Read(header, 0, 44);
            }
            string riff = System.Text.Encoding.ASCII.GetString(header, 0, 4);
            string wave = System.Text.Encoding.ASCII.GetString(header, 8, 4);
            Assert(riff == "RIFF" && wave == "WAVE", string.Format("{0} has valid RIFF/WAVE header", bgm));
        }

        // 2. Check Pixel Art Assets
        string artDir = Path.Combine(projectRoot, "Assets/Resources/GeneratedArt");
        string[] artFiles = new string[] {
            "portal_warp.png",
            "portal_ring.png",
            "server_rack.png",
            "courier_run_0.png",
            "courier_run_1.png",
            "courier_run_2.png",
            "courier_run_3.png"
        };
        foreach (string art in artFiles)
        {
            string p = Path.Combine(artDir, art);
            Assert(File.Exists(p), string.Format("{0} exists", art));
            Assert(File.Exists(p + ".meta"), string.Format("{0}.meta exists", art));
        }

        // 3. Check JuiceManager Code
        string juiceScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/JuiceManager.cs");
        Assert(File.Exists(juiceScript), "JuiceManager.cs exists");
        string juiceText = File.ReadAllText(juiceScript);
        Assert(juiceText.Contains("public static void Hitstop"), "JuiceManager has Hitstop method");
        Assert(juiceText.Contains("public static void Shake"), "JuiceManager has Shake method");
        Assert(juiceText.Contains("public static void SpawnImpactSparks"), "JuiceManager has SpawnImpactSparks method");
        Assert(juiceText.Contains("public static void SpawnDustPuff"), "JuiceManager has SpawnDustPuff method");
        Assert(juiceText.Contains("CurrentShakeOffset"), "JuiceManager calculates CurrentShakeOffset");

        // 4. Check CameraFollow Integration
        string camScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/CameraFollow.cs");
        string camText = File.ReadAllText(camScript);
        Assert(camText.Contains("CurrentShakeOffset"), "CameraFollow integrates CurrentShakeOffset");

        // 5. Check AudioManager BGM Integration
        string audioScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/AudioManager.cs");
        string audioText = File.ReadAllText(audioScript);
        Assert(audioText.Contains("enum BGMTrack"), "AudioManager defines BGMTrack enum");
        Assert(audioText.Contains("PlayBGM"), "AudioManager has PlayBGM method");
        Assert(audioText.Contains("SetMuffled"), "AudioManager has SetMuffled low-pass method");

        // 6. Check SafeZonePortal & ServerB_Goal
        string portalScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/SafeZonePortal.cs");
        Assert(File.Exists(portalScript), "SafeZonePortal.cs exists");
        string portalText = File.ReadAllText(portalScript);
        Assert(portalText.Contains("TriggerWarp"), "SafeZonePortal has TriggerWarp method");

        string goalScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/ServerB_Goal.cs");
        string goalText = File.ReadAllText(goalScript);
        Assert(goalText.Contains("SafeZonePortal"), "ServerB_Goal installs SafeZonePortal");

        // 7. Check Loading Screen
        string loadingScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/PacketCourierLoadingScreen.cs");
        Assert(File.Exists(loadingScript), "PacketCourierLoadingScreen.cs exists");
        string loadingText = File.ReadAllText(loadingScript);
        Assert(loadingText.Contains("AnimateCourier"), "PacketCourierLoadingScreen animates courier running");
        Assert(loadingText.Contains("TCP 3-WAY HANDSHAKE"), "PacketCourierLoadingScreen simulates TCP handshake");

        // 8. Check Dash Ghost Trail
        string ghostScript = Path.Combine(projectRoot, "Assets/_Scripts/Player/DashGhostTrail.cs");
        Assert(File.Exists(ghostScript), "DashGhostTrail.cs exists");
        string ghostText = File.ReadAllText(ghostScript);
        Assert(ghostText.Contains("EnsurePoolPrewarmed"), "DashGhostTrail implements pre-warmed object pooling");
        Assert(ghostText.Contains("normalDashColor") && ghostText.Contains("phaseDashColor"), "DashGhostTrail supports dynamic cyber tints");

        // 9. Check Enhanced JuiceManager (Pooling, Damage Glitch, Wall Sparks, Data Dissolve)
        Assert(juiceText.Contains("GetPooledSpark"), "JuiceManager provides object-pooled spark recycling");
        Assert(juiceText.Contains("TriggerDamageGlitch"), "JuiceManager provides TriggerDamageGlitch screen feedback");
        Assert(juiceText.Contains("SpawnWallSparks"), "JuiceManager provides SpawnWallSparks");
        Assert(juiceText.Contains("SpawnDataDissolve"), "JuiceManager provides SpawnDataDissolve");

        // 10. Check Traversal Audio in AudioManager
        Assert(audioText.Contains("PlayFootstep"), "AudioManager has PlayFootstep");
        Assert(audioText.Contains("PlayLand"), "AudioManager has PlayLand");
        Assert(audioText.Contains("PlayEnemyDissolve"), "AudioManager has PlayEnemyDissolve");

        // 11. Check Player Controller Integration
        string playerScript = Path.Combine(projectRoot, "Assets/_Scripts/Player/PlayerController.cs");
        string playerText = File.ReadAllText(playerScript);
        Assert(playerText.Contains("DashGhostTrail"), "PlayerController ensures DashGhostTrail in Awake");
        Assert(playerText.Contains("SpawnWallSparks"), "PlayerController spawns wall friction sparks while clinging");
        Assert(playerText.Contains("PlayFootstep"), "PlayerController triggers footstep audio on ground traversal");
        Assert(playerText.Contains("PlayLand"), "PlayerController triggers landing audio on impact");

        // 12. Check Player Health Damage Glitch Integration
        string healthScript = Path.Combine(projectRoot, "Assets/_Scripts/Player/PlayerHealth.cs");
        string healthText = File.ReadAllText(healthScript);
        Assert(healthText.Contains("TriggerDamageGlitch"), "PlayerHealth triggers screen damage glitch on hit");

        // 13. Check Enemy Data Dissolve Integration
        string patrolScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/Enemy_Patrol.cs");
        string patrolText = File.ReadAllText(patrolScript);
        Assert(patrolText.Contains("SpawnDataDissolve"), "Enemy_Patrol spawns data dissolve on death");

        string flyingScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/Enemy_Flying.cs");
        string flyingText = File.ReadAllText(flyingScript);
        Assert(flyingText.Contains("SpawnDataDissolve"), "Enemy_Flying spawns data dissolve on death");

        Console.WriteLine(string.Format("PASS: {0} game feel, juice, BGM, portal, ghost trail and dissolve checks", _passed));
    }
}
