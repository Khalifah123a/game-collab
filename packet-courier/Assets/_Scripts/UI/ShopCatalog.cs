using System;
using System.Collections.Generic;

// Pure shop model (no UnityEngine). Shared by tests and ShopUI/ShopNPC.

public enum ShopId
{
    WallClingTime = 0,
    WeaponDamage = 1,
    WeaponLevel = 2,
    WeaponRange = 3,
    AttackSpeed = 4,
    RunSpeed = 5
}

public enum PurchaseResult
{
    Success = 0,
    InsufficientFunds = 1,
    MaxLevel = 2
}

public interface IUpgradeWallet
{
    int GetLevel(int upgradeIndex);
    int GetMaxLevel(int upgradeIndex);
    int GetCost(int upgradeIndex, int nextLevel);
    /// <summary>Non-mutating affordability probe. Must not spend or count as a spend call.</summary>
    bool CanAfford(int amount);
    PurchaseResult Buy(int upgradeIndex);
    bool TrySpend(int amount);
}

public sealed class ShopEntry
{
    public ShopId Id { get; private set; }
    public int UpgradeIndex { get; private set; }
    public string Label { get; private set; }
    public string Effect { get; private set; }

    internal ShopEntry(ShopId id, int upgradeIndex, string label, string effect)
    {
        Id = id;
        UpgradeIndex = upgradeIndex;
        Label = label;
        Effect = effect;
    }
}

public static class ShopCatalog
{
    // Must match the order expected by tests: 6 entries total.
    public static readonly ShopEntry WallClingTimeEntry = new ShopEntry(ShopId.WallClingTime, 0, "Wall Cling", "+0.15s");
    public static readonly ShopEntry WeaponDamageEntry = new ShopEntry(ShopId.WeaponDamage, 1, "Weapon Damage", "+1 dmg");
    public static readonly ShopEntry WeaponLevelEntry = new ShopEntry(ShopId.WeaponLevel, 2, "Weapon Level", "+1 level");
    public static readonly ShopEntry WeaponRangeEntry = new ShopEntry(ShopId.WeaponRange, 3, "Weapon Range", "+0.25 reach");
    public static readonly ShopEntry AttackSpeedEntry = new ShopEntry(ShopId.AttackSpeed, 4, "Attack Speed", "+10% swing");
    public static readonly ShopEntry RunSpeedEntry = new ShopEntry(ShopId.RunSpeed, 5, "Run Speed", "+0.25 m/s");

    public static readonly ShopEntry[] Entries = new ShopEntry[]
    {
        WallClingTimeEntry,
        WeaponDamageEntry,
        WeaponLevelEntry,
        WeaponRangeEntry,
        AttackSpeedEntry,
        RunSpeedEntry
    };

    // Indices referenced by tests.
    public const int WeaponDamageUpgradeIndex = 1;

    public static ShopEntry Find(ShopId id)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Id == id) return Entries[i];
        }
        return null;
    }

    public static int IndexOf(ShopId id)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Id == id) return i;
        }
        return -1;
    }
}

public static class ShopRules
{
    public static string RowLabel(IUpgradeWallet wallet, ShopEntry entry)
    {
        if (entry == null || wallet == null) return string.Empty;
        int level = wallet.GetLevel(entry.UpgradeIndex);
        int max = wallet.GetMaxLevel(entry.UpgradeIndex);
        int cost = NextCost(wallet, entry);
        string costText = cost < 0 ? "MAX" : cost.ToString();
        return entry.Label + " (" + entry.Effect + ") Lv " + level + "/" + max + " - " + costText + " DB";
    }

    public static int NextCost(IUpgradeWallet wallet, ShopEntry entry)
    {
        if (entry == null || wallet == null) return -1;
        int level = wallet.GetLevel(entry.UpgradeIndex);
        int max = wallet.GetMaxLevel(entry.UpgradeIndex);
        if (level >= max) return -1;
        return wallet.GetCost(entry.UpgradeIndex, level + 1);
    }

    public static PurchaseResult CanBuy(IUpgradeWallet wallet, ShopEntry entry)
    {
        if (entry == null || wallet == null) return PurchaseResult.InsufficientFunds;
        int level = wallet.GetLevel(entry.UpgradeIndex);
        int max = wallet.GetMaxLevel(entry.UpgradeIndex);
        if (level >= max) return PurchaseResult.MaxLevel;
        int cost = wallet.GetCost(entry.UpgradeIndex, level + 1);
        // Non-mutating probe: CanBuy must never spend or count as a spend call,
        // otherwise a CanBuy-then-Buy sequence would charge twice.
        if (!wallet.CanAfford(cost)) return PurchaseResult.InsufficientFunds;
        return PurchaseResult.Success;
    }

    public static PurchaseResult Buy(IUpgradeWallet wallet, ShopEntry entry)
    {
        if (entry == null || wallet == null) return PurchaseResult.InsufficientFunds;
        return wallet.Buy(entry.UpgradeIndex);
    }

    public static PurchaseResult Cancel(IUpgradeWallet wallet)
    {
        // Cancel is free. No spend, no state change.
        return PurchaseResult.Success;
    }
}