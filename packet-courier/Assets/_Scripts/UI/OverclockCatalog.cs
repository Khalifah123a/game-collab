using System;
using System.Collections.Generic;

// Pure Overclock model (no UnityEngine). Shared by tests, OverclockManager, and OverclockUI.

public enum OverclockId
{
    TripleJump = 0,
    CyberBlaster = 1,
    NanoParry = 2,
    CoreDamage = 3,
    PhaseDash = 4,
    MagneticPull = 5,
    TurboBattery = 6,
    EmergencyBuffer = 7
}

public sealed class OverclockEntry
{
    public OverclockId Id { get; private set; }
    public int Index { get; private set; }
    public string Name { get; private set; }
    public string Effect { get; private set; }
    public string ControlsHint { get; private set; }
    public int MaxLevel { get; private set; }
    public int[] Costs { get; private set; }

    internal OverclockEntry(OverclockId id, int index, string name, string effect, string controlsHint, int maxLevel, int[] costs)
    {
        Id = id;
        Index = index;
        Name = name;
        Effect = effect;
        ControlsHint = controlsHint;
        MaxLevel = maxLevel;
        Costs = costs;
    }
}

public static class OverclockCatalog
{
    public static readonly OverclockEntry TripleJumpEntry = new OverclockEntry(
        OverclockId.TripleJump, 0, "Triple Jump", "3x Lompat di udara", "Spasi 3x di udara", 1, new[] { 250 });

    public static readonly OverclockEntry CyberBlasterEntry = new OverclockEntry(
        OverclockId.CyberBlaster, 1, "Cyber Blaster", "Tembak peluru photon data", "Klik Kanan / F", 3, new[] { 200, 400, 700 });

    public static readonly OverclockEntry NanoParryEntry = new OverclockEntry(
        OverclockId.NanoParry, 2, "Nano Parry", "Tangkis serangan & counter", "Tekan Q / RMB", 3, new[] { 200, 450, 750 });

    public static readonly OverclockEntry CoreDamageEntry = new OverclockEntry(
        OverclockId.CoreDamage, 3, "Core Damage", "+1 Damage melee & peluru", "Pasif otomatis", 3, new[] { 180, 350, 600 });

    public static readonly OverclockEntry PhaseDashEntry = new OverclockEntry(
        OverclockId.PhaseDash, 4, "Phase Dash", "Dash kebal musuh & laser", "Shift / Ctrl", 1, new[] { 300 });

    public static readonly OverclockEntry MagneticPullEntry = new OverclockEntry(
        OverclockId.MagneticPull, 5, "Data Magnet", "Tarik DataBits dari jauh", "Pasif radius", 2, new[] { 150, 300 });

    public static readonly OverclockEntry TurboBatteryEntry = new OverclockEntry(
        OverclockId.TurboBattery, 6, "Turbo Battery", "Stamina +50%, Dash -30%", "Pasif stamina", 2, new[] { 150, 300 });

    public static readonly OverclockEntry EmergencyBufferEntry = new OverclockEntry(
        OverclockId.EmergencyBuffer, 7, "Buffer Boost", "+20 Max TTL & Reviver 1x", "Pasif TTL", 2, new[] { 200, 400 });

    public static readonly OverclockEntry[] Entries = new[]
    {
        TripleJumpEntry,
        CyberBlasterEntry,
        NanoParryEntry,
        CoreDamageEntry,
        PhaseDashEntry,
        MagneticPullEntry,
        TurboBatteryEntry,
        EmergencyBufferEntry
    };

    public static OverclockEntry Find(OverclockId id)
    {
        for (int i = 0; i < Entries.Length; i++)
            if (Entries[i].Id == id) return Entries[i];
        return null;
    }

    public static int IndexOf(OverclockId id)
    {
        for (int i = 0; i < Entries.Length; i++)
            if (Entries[i].Id == id) return i;
        return -1;
    }

    public static int NextCost(IUpgradeWallet wallet, OverclockEntry entry)
    {
        if (wallet == null || entry == null) return -1;
        int level = wallet.GetLevel(entry.Index);
        if (level >= entry.MaxLevel) return -1;
        if (level < 0 || level >= entry.Costs.Length) return -1;
        return entry.Costs[level];
    }

    public static PurchaseResult CanBuy(IUpgradeWallet wallet, OverclockEntry entry)
    {
        if (wallet == null || entry == null) return PurchaseResult.InsufficientFunds;
        int level = wallet.GetLevel(entry.Index);
        if (level >= entry.MaxLevel) return PurchaseResult.MaxLevel;
        int cost = NextCost(wallet, entry);
        if (!wallet.CanAfford(cost)) return PurchaseResult.InsufficientFunds;
        return PurchaseResult.Success;
    }

    public static PurchaseResult Buy(IUpgradeWallet wallet, OverclockEntry entry)
    {
        if (wallet == null || entry == null) return PurchaseResult.InsufficientFunds;
        return wallet.Buy(entry.Index);
    }

    public static string RowLabel(IUpgradeWallet wallet, OverclockEntry entry)
    {
        if (entry == null || wallet == null) return string.Empty;
        int level = wallet.GetLevel(entry.Index);
        int max = entry.MaxLevel;
        int cost = NextCost(wallet, entry);
        string costText = cost < 0 ? "MAX" : cost.ToString() + " DB";
        return string.Format("{0} ({1}) [Lv {2}/{3}] - {4}", entry.Name, entry.Effect, level, max, costText);
    }
}
