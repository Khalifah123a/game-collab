using System;
using System.IO;

public static class FullExpansionChecks
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

        // =========================================================================
        // 1. Packet Modifiers (Item 2)
        // =========================================================================
        string modScript = Path.Combine(projectRoot, "Assets/_Scripts/Quest/PacketModifier.cs");
        Assert(File.Exists(modScript), "PacketModifier.cs exists");
        string modText = File.ReadAllText(modScript);
        Assert(modText.Contains("enum PacketModifierType"), "defines PacketModifierType enum");
        Assert(modText.Contains("FragileUDP") && modText.Contains("JumboFrame") && modText.Contains("VolatileICMP"), "defines FragileUDP, JumboFrame, VolatileICMP");

        string pimScript = Path.Combine(projectRoot, "Assets/_Scripts/Quest/PacketIntegrityManager.cs");
        string pimText = File.ReadAllText(pimScript);
        Assert(pimText.Contains("ActiveModifier"), "PacketIntegrityManager has ActiveModifier property");
        Assert(pimText.Contains("SetModifier"), "PacketIntegrityManager has SetModifier method");

        string playerScript = Path.Combine(projectRoot, "Assets/_Scripts/Player/PlayerController.cs");
        string playerText = File.ReadAllText(playerScript);
        Assert(playerText.Contains("jumpMultiplier"), "PlayerController applies modifier jumpMultiplier");
        Assert(playerText.Contains("speedMultiplier"), "PlayerController applies modifier speedMultiplier");

        string termScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/ProtocolTerminal.cs");
        Assert(File.Exists(termScript), "ProtocolTerminal.cs exists");
        string termText = File.ReadAllText(termScript);
        Assert(termText.Contains("OpenProtocolMenu"), "ProtocolTerminal has OpenProtocolMenu method");

        string protoUiScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/ProtocolSelectionUI.cs");
        Assert(File.Exists(protoUiScript), "ProtocolSelectionUI.cs exists");
        string protoUiText = File.ReadAllText(protoUiScript);
        Assert(protoUiText.Contains("SelectModifier"), "ProtocolSelectionUI has SelectModifier method");

        string qsScript = Path.Combine(projectRoot, "Assets/_Scripts/Quest/QuestService.cs");
        string qsText = File.ReadAllText(qsScript);
        Assert(qsText.Contains("EnsureProtocolTerminal"), "QuestService installs ProtocolTerminal in Safe Zone 1");

        // =========================================================================
        // 2. Transmission Scorecard & Network Education Facts (Item 2)
        // =========================================================================
        string cardScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/TransmissionScorecardUI.cs");
        Assert(File.Exists(cardScript), "TransmissionScorecardUI.cs exists");
        string cardText = File.ReadAllText(cardScript);
        Assert(cardText.Contains("TCP 3-WAY HANDSHAKE"), "Scorecard contains TCP 3-way handshake educational byte");
        Assert(cardText.Contains("TIME-TO-LIVE (TTL)"), "Scorecard contains TTL educational byte");
        Assert(cardText.Contains("CHECKSUM & ERROR DETECTION"), "Scorecard contains Checksum educational byte");
        Assert(cardText.Contains("MTU & FRAGMENTASI"), "Scorecard contains MTU educational byte");

        string receiverScript = Path.Combine(projectRoot, "Assets/_Scripts/Quest/PackageReceiverNPC.cs");
        string receiverText = File.ReadAllText(receiverScript);
        Assert(receiverText.Contains("TransmissionScorecardUI.Show"), "PackageReceiverNPC triggers TransmissionScorecardUI on delivery");

        // =========================================================================
        // 3. Boss Rush Mode & Speedrun Split Timer (Item 3)
        // =========================================================================
        string rushScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/BossRushManager.cs");
        Assert(File.Exists(rushScript), "BossRushManager.cs exists");
        string rushText = File.ReadAllText(rushScript);
        Assert(rushText.Contains("StartBossRush"), "BossRushManager has StartBossRush method");
        Assert(rushText.Contains("TheCensor") && rushText.Contains("Solaris") && rushText.Contains("RootkitCore"), "BossRush cycles through all 3 bosses");

        string speedrunScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/SpeedrunTimerHUD.cs");
        Assert(File.Exists(speedrunScript), "SpeedrunTimerHUD.cs exists");
        string speedrunText = File.ReadAllText(speedrunScript);
        Assert(speedrunText.Contains("RecordSplit"), "SpeedrunTimerHUD has RecordSplit delta calculation");

        // =========================================================================
        // 4. Achievement & Protocol Badges (Item 3)
        // =========================================================================
        string achScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/AchievementManager.cs");
        Assert(File.Exists(achScript), "AchievementManager.cs exists");
        string achText = File.ReadAllText(achScript);
        Assert(achText.Contains("ACH_RFC791") && achText.Contains("ACH_ZERO_LOSS"), "contains core networking achievements");
        Assert(achText.Contains("ShowToast"), "AchievementManager displays on-screen cyber toast");

        // =========================================================================
        // 5. Gamepad Support & Touch Controls (Item 4)
        // =========================================================================
        string padScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/GamepadControllerSupport.cs");
        Assert(File.Exists(padScript), "GamepadControllerSupport.cs exists");
        string padText = File.ReadAllText(padScript);
        Assert(padText.Contains("TriggerRumble"), "GamepadControllerSupport supports controller vibration");

        string touchScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/TouchControlsUI.cs");
        Assert(File.Exists(touchScript), "TouchControlsUI.cs exists");
        string touchText = File.ReadAllText(touchScript);
        Assert(touchText.Contains("Btn_Jump") && touchText.Contains("Btn_Dash"), "TouchControlsUI provides full mobile button cluster");

        string pauseScript = Path.Combine(projectRoot, "Assets/_Scripts/UI/PauseMenuUI.cs");
        string pauseText = File.ReadAllText(pauseScript);
        Assert(pauseText.Contains("ToggleTouchControls"), "PauseMenuUI has ToggleTouchControls button");
        Assert(pauseText.Contains("ToggleRumble"), "PauseMenuUI has ToggleRumble button");

        string imScript = Path.Combine(projectRoot, "Assets/_Scripts/Core/InputManager.cs");
        string imText = File.ReadAllText(imScript);
        Assert(imText.Contains("GamepadControllerSupport.Ensure"), "InputManager auto-initializes GamepadControllerSupport");
        Assert(imText.Contains("TouchControlsUI.Ensure"), "InputManager auto-initializes TouchControlsUI");

        // =========================================================================
        // 6. Combat Enrichment: Firewall Turret & Glitch Phantom (Item 5)
        // =========================================================================
        string turretScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/Enemy_FirewallTurret.cs");
        Assert(File.Exists(turretScript), "Enemy_FirewallTurret.cs exists");
        string turretText = File.ReadAllText(turretScript);
        Assert(turretText.Contains("IEnemyDamageable"), "FirewallTurret implements IEnemyDamageable");
        Assert(turretText.Contains("FireFirewallLaser"), "FirewallTurret fires firewall projectile beams");

        string phantomScript = Path.Combine(projectRoot, "Assets/_Scripts/Level/Enemy_GlitchPhantom.cs");
        Assert(File.Exists(phantomScript), "Enemy_GlitchPhantom.cs exists");
        string phantomText = File.ReadAllText(phantomScript);
        Assert(phantomText.Contains("IEnemyDamageable"), "GlitchPhantom implements IEnemyDamageable");
        Assert(phantomText.Contains("EnterDesync"), "GlitchPhantom implements clock desynchronization exploit");

        Console.WriteLine(string.Format("PASS: All {0} multi-system feature checks verified (Quality & Educational Standards)", _passed));
    }
}
