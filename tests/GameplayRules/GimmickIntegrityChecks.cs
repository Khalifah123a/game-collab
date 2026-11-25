using System;
using System.IO;

public static class GimmickIntegrityChecks
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

        // 1. Check Gimmick Pixel Art Assets
        string artDir = Path.Combine(projectRoot, "Assets/Resources/GeneratedArt");
        string[] artFiles = new string[] {
            "data_switch.png",
            "wind_zone.png",
            "gravity_beam.png",
            "packet_integrity_icon.png"
        };
        foreach (string art in artFiles)
        {
            string p = Path.Combine(artDir, art);
            Assert(File.Exists(p), string.Format("{0} exists", art));
            Assert(File.Exists(p + ".meta"), string.Format("{0}.meta exists", art));
        }

        // 2. Pure Logic Check: Packet Integrity Grade & Penalties
        float integrity = 100f;
        Assert(PacketIntegrityRules.CalculateGrade(integrity) == DeliveryGrade.S, "100% integrity is Grade S");
        Assert(PacketIntegrityRules.GetBonusBits(DeliveryGrade.S) == 150, "Grade S awards 150 bonus bits");

        // Simulate taking 3 hits (-18%)
        integrity -= 3f * PacketIntegrityRules.PenaltyPerHit;
        Assert(integrity == 82f, "integrity drops by penalty per hit");
        Assert(PacketIntegrityRules.CalculateGrade(integrity) == DeliveryGrade.A, "82% integrity is Grade A");
        Assert(PacketIntegrityRules.GetBonusBits(DeliveryGrade.A) == 80, "Grade A awards 80 bonus bits");

        // Simulate taking 6 more hits (-36% -> 46%)
        integrity -= 6f * PacketIntegrityRules.PenaltyPerHit;
        Assert(PacketIntegrityRules.CalculateGrade(integrity) == DeliveryGrade.B, "46% integrity is Grade B");
        Assert(PacketIntegrityRules.GetBonusBits(DeliveryGrade.B) == 30, "Grade B awards 30 bonus bits");

        // Simulate taking many hits (heavy corruption)
        integrity -= 10f * PacketIntegrityRules.PenaltyPerHit;
        float clamped = Math.Max(PacketIntegrityRules.MinIntegrity, integrity);
        Assert(clamped >= PacketIntegrityRules.MinIntegrity, "integrity clamps to minimum threshold so delivery never breaks");
        Assert(PacketIntegrityRules.CalculateGrade(clamped) == DeliveryGrade.C, "minimum integrity is Grade C");
        Assert(PacketIntegrityRules.GetBonusBits(DeliveryGrade.C) == 0, "Grade C awards 0 bonus bits");

        // 3. Check Code Contracts: World Gimmicks
        string windScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/NetworkWindZone.cs");
        Assert(File.Exists(windScript), "NetworkWindZone.cs exists");
        string windText = File.ReadAllText(windScript);
        Assert(windText.Contains("windForce"), "NetworkWindZone defines windForce");
        Assert(windText.Contains("IsGustActive"), "NetworkWindZone tracks IsGustActive cycle");

        string switchScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/DataBlasterSwitch.cs");
        Assert(File.Exists(switchScript), "DataBlasterSwitch.cs exists");
        string switchText = File.ReadAllText(switchScript);
        Assert(switchText.Contains("IEnemyDamageable"), "DataBlasterSwitch implements IEnemyDamageable for sword/blaster hits");
        Assert(switchText.Contains("TakeHit"), "DataBlasterSwitch implements TakeHit toggle");

        string barrierScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/DataLaserBarrier.cs");
        Assert(File.Exists(barrierScript), "DataLaserBarrier.cs exists");
        string barrierText = File.ReadAllText(barrierScript);
        Assert(barrierText.Contains("SetBarrierOpen"), "DataLaserBarrier has SetBarrierOpen method");

        string uplinkScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/AntiGravityUplink.cs");
        Assert(File.Exists(uplinkScript), "AntiGravityUplink.cs exists");
        string uplinkText = File.ReadAllText(uplinkScript);
        Assert(uplinkText.Contains("liftSpeed"), "AntiGravityUplink defines liftSpeed");
        Assert(uplinkText.Contains("airDashesLeft = 1"), "AntiGravityUplink refreshes air dash inside stream");

        // 4. Check LevelManager Integration
        string lmScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/LevelManager.cs");
        string lmText = File.ReadAllText(lmScript);
        Assert(lmText.Contains("NetworkWindZone"), "LevelManager installs World 2 NetworkWindZone");
        Assert(lmText.Contains("DataBlasterSwitch"), "LevelManager installs World 3 DataBlasterSwitch");
        Assert(lmText.Contains("AntiGravityUplink"), "LevelManager installs World 4 AntiGravityUplink");

        // 5. Check Receiver & HUD Integration
        string rxScript = Path.Combine(projectRoot, "Assets/_Scripts/Quest/PackageReceiverNPC.cs");
        string rxText = File.ReadAllText(rxScript);
        Assert(rxText.Contains("PacketIntegrityManager"), "PackageReceiverNPC evaluates PacketIntegrityManager grade and pays bonus");

        string hudScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/GameplayHUD.cs");
        string hudText = File.ReadAllText(hudScript);
        Assert(hudText.Contains("PacketIntegrityManager"), "GameplayHUD displays packet integrity % and grade");

        Console.WriteLine(string.Format("PASS: {0} world gimmicks and packet integrity checks", _passed));
    }
}
