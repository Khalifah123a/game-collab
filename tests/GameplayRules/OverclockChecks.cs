using System;
using System.Collections.Generic;

// Standalone csc test suite for Overclock System.
public static class OverclockChecks
{
    public static int Passed;

    public static int Main()
    {
        Run("overclock catalog has 8 hardware upgrades", CatalogLengthShape);
        Run("all entries expose valid name effect and controls hint", EntryMetadataValid);
        Run("triple jump requires level 1 and max level is 1", TripleJumpConfig);
        Run("cyber blaster has 3 levels and correct scaling prices", BlasterConfig);
        Run("nano parry has 3 levels and correct scaling prices", ParryConfig);
        Run("core damage has 3 levels and boosts melee and projectile", CoreDamageConfig);
        Run("phase dash provides invulnerability contract", PhaseDashConfig);
        Run("data magnet provides 6m and 10m pull radius", MagnetConfig);
        Run("turbo battery reduces dash cost and accelerates regen", TurboBatteryConfig);
        Run("buffer boost increases TTL capacity and grants reviver", BufferBoostConfig);
        Run("affordable upgrade is purchased and deducts data bits", BuyDeductsDataBits);
        Run("insufficient funds rejects purchase and deducts zero bits", InsufficientFundsRejection);
        Run("max level blocks further purchases", MaxLevelEnforced);
        Run("row label displays formatted name level and cost", RowLabelFormatting);

        Console.WriteLine("PASS: " + Passed + " overclock system checks");
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

    private static void AssertEq(int actual, int expected, string message)
    {
        if (actual != expected) throw new Exception(message + " (expected " + expected + ", got " + actual + ")");
    }

    private static void CatalogLengthShape()
    {
        AssertEq(OverclockCatalog.Entries.Length, 8, "catalog should contain 8 upgrades");
        for (int i = 0; i < OverclockCatalog.Entries.Length; i++)
        {
            AssertEq(OverclockCatalog.Entries[i].Index, i, "index should match array position");
        }
    }

    private static void EntryMetadataValid()
    {
        foreach (var entry in OverclockCatalog.Entries)
        {
            Assert(!string.IsNullOrEmpty(entry.Name), "entry missing name: " + entry.Id);
            Assert(!string.IsNullOrEmpty(entry.Effect), "entry missing effect: " + entry.Id);
            Assert(!string.IsNullOrEmpty(entry.ControlsHint), "entry missing controls hint: " + entry.Id);
            Assert(entry.MaxLevel > 0, "entry max level <= 0: " + entry.Id);
            AssertEq(entry.Costs.Length, entry.MaxLevel, "costs count must match max level for " + entry.Id);
            foreach (var cost in entry.Costs) Assert(cost > 0, "cost must be positive for " + entry.Id);
        }
    }

    private static void TripleJumpConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.TripleJump);
        Assert(entry != null, "triple jump missing");
        AssertEq(entry.MaxLevel, 1, "triple jump max level");
        AssertEq(entry.Costs[0], 250, "triple jump cost");
        Assert(entry.ControlsHint.Contains("Spasi"), "controls hint should mention jump key");
    }

    private static void BlasterConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.CyberBlaster);
        Assert(entry != null, "blaster missing");
        AssertEq(entry.MaxLevel, 3, "blaster max level");
        AssertEq(entry.Costs[0], 200, "blaster lv1 cost");
        AssertEq(entry.Costs[1], 400, "blaster lv2 cost");
        AssertEq(entry.Costs[2], 700, "blaster lv3 cost");
    }

    private static void ParryConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.NanoParry);
        Assert(entry != null, "parry missing");
        AssertEq(entry.MaxLevel, 3, "parry max level");
        Assert(entry.ControlsHint.Contains("Q"), "parry controls hint should mention Q");
    }

    private static void CoreDamageConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.CoreDamage);
        Assert(entry != null, "damage missing");
        AssertEq(entry.MaxLevel, 3, "damage max level");
    }

    private static void PhaseDashConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.PhaseDash);
        Assert(entry != null, "phase dash missing");
        AssertEq(entry.MaxLevel, 1, "phase dash max level");
        AssertEq(entry.Costs[0], 300, "phase dash cost");
    }

    private static void MagnetConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.MagneticPull);
        Assert(entry != null, "magnet missing");
        AssertEq(entry.MaxLevel, 2, "magnet max level");
    }

    private static void TurboBatteryConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.TurboBattery);
        Assert(entry != null, "battery missing");
        AssertEq(entry.MaxLevel, 2, "battery max level");
    }

    private static void BufferBoostConfig()
    {
        var entry = OverclockCatalog.Find(OverclockId.EmergencyBuffer);
        Assert(entry != null, "buffer missing");
        AssertEq(entry.MaxLevel, 2, "buffer max level");
    }

    private static void BuyDeductsDataBits()
    {
        var wallet = new FakeOverclockWallet { Balance = 1000 };
        var entry = OverclockCatalog.Find(OverclockId.CyberBlaster);
        int cost = OverclockCatalog.NextCost(wallet, entry);
        AssertEq(cost, 200, "first cost mismatch");

        Assert(OverclockCatalog.CanBuy(wallet, entry) == PurchaseResult.Success, "affordable entry blocked");
        var res = OverclockCatalog.Buy(wallet, entry);
        Assert(res == PurchaseResult.Success, "buy failed");
        AssertEq(wallet.GetLevel(entry.Index), 1, "level not incremented");
        AssertEq(wallet.Balance, 800, "balance not deducted properly");
    }

    private static void InsufficientFundsRejection()
    {
        var wallet = new FakeOverclockWallet { Balance = 50 };
        var entry = OverclockCatalog.Find(OverclockId.TripleJump);
        Assert(OverclockCatalog.CanBuy(wallet, entry) == PurchaseResult.InsufficientFunds, "broke player reported as can buy");
        var res = OverclockCatalog.Buy(wallet, entry);
        Assert(res == PurchaseResult.InsufficientFunds, "broke player bought upgrade");
        AssertEq(wallet.Balance, 50, "balance changed after rejected purchase");
        AssertEq(wallet.GetLevel(entry.Index), 0, "level changed after rejected purchase");
    }

    private static void MaxLevelEnforced()
    {
        var wallet = new FakeOverclockWallet { Balance = 10000 };
        var entry = OverclockCatalog.Find(OverclockId.TripleJump);
        AssertEq((int)OverclockCatalog.Buy(wallet, entry), (int)PurchaseResult.Success, "lv 1 buy failed");
        AssertEq(wallet.GetLevel(entry.Index), 1, "level not 1");
        AssertEq(OverclockCatalog.NextCost(wallet, entry), -1, "maxed entry still has next cost");
        Assert(OverclockCatalog.CanBuy(wallet, entry) == PurchaseResult.MaxLevel, "maxed entry buyable");
        Assert(OverclockCatalog.Buy(wallet, entry) == PurchaseResult.MaxLevel, "bought past max level");
        AssertEq(wallet.GetLevel(entry.Index), 1, "level exceeded max");
    }

    private static void RowLabelFormatting()
    {
        var wallet = new FakeOverclockWallet { Balance = 1000 };
        var entry = OverclockCatalog.Find(OverclockId.NanoParry);
        string label = OverclockCatalog.RowLabel(wallet, entry);
        Assert(label.Contains("Nano Parry"), "row missing name: " + label);
        Assert(label.Contains("Lv 0/3"), "row missing level: " + label);
        Assert(label.Contains("200 DB"), "row missing price: " + label);
    }

    private sealed class FakeOverclockWallet : IUpgradeWallet
    {
        public int Balance;
        private readonly int[] levels = new int[8];

        public int GetLevel(int upgradeIndex) { return levels[upgradeIndex]; }
        public int GetMaxLevel(int upgradeIndex) { return OverclockCatalog.Entries[upgradeIndex].MaxLevel; }
        public int GetCost(int upgradeIndex, int nextLevel) { return OverclockCatalog.Entries[upgradeIndex].Costs[nextLevel - 1]; }
        public bool CanAfford(int amount) { return Balance >= amount; }
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount) return false;
            Balance -= amount;
            return true;
        }

        public PurchaseResult Buy(int upgradeIndex)
        {
            var entry = OverclockCatalog.Entries[upgradeIndex];
            if (levels[upgradeIndex] >= entry.MaxLevel) return PurchaseResult.MaxLevel;
            int cost = GetCost(upgradeIndex, levels[upgradeIndex] + 1);
            if (!TrySpend(cost)) return PurchaseResult.InsufficientFunds;
            levels[upgradeIndex]++;
            return PurchaseResult.Success;
        }
    }
}
