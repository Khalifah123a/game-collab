using System;
using System.Collections.Generic;

// Standalone (csc, no UnityEngine) proof for Stage 2: optional quests + shop economy.
// Compiled by Run-Stage2Checks.ps1 alongside the Unity-free gameplay models.
public static class Stage2OptionalShopLogic
{
    private static readonly List<string> Kills = new List<string> { "enemy:patrol", "enemy:flying" };
    private static readonly List<string> Bits = new List<string> { "bit:recycle", "bit:tray" };
    private static readonly List<string> None = new List<string>();

    public static int Main()
    {
        Run("deterministic roll is stable across retry/death/segment change", DeterministicRollStability);
        Run("seed changes what gets rolled", SeedChangesRoll);
        Run("only targets present in the world are rolled", OnlyPresentTargetsRolled);
        Run("kill-3 quest completes on three distinct kills", KillQuestCompletes);
        Run("reporting the same id twice never double counts", ReportIsIdempotentPerId);
        Run("collect quest completes on three distinct pickups", CollectQuestCompletes);
        Run("collect quest ignores kills", CollectIgnoresKills);
        Run("claim pays the bonus exactly once", SinglePayout);
        Run("incomplete quest pays nothing", IncompletePaysNothing);
        Run("unknown world or index pays nothing", UnknownPayNothing);
        Run("book reset re-rolls for the new run", ResetRerolls);
        Run("shop rows expose price, effect, level and max level", ShopRowShape);
        Run("buy raises level and deducts the listed price", BuyDeducts);
        Run("cancel spends nothing", CancelFree);
        Run("max level blocks further purchases", MaxLevelEnforced);
        Run("insufficient funds change nothing", InsufficientFundsPath);
        Console.WriteLine("PASS: " + Stage2OptionalShopLogic.Passed + " stage-2 optional/shop logic checks");
        return 0;
    }

    public static int Passed;

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

    private static OptionalQuestBook FreshBook(int runSeed)
    {
        var book = new OptionalQuestBook();
        book.Reset(runSeed);
        return book;
    }

    private static string Fingerprint(OptionalQuestBook book, int world)
    {
        var quests = book.Roll(world, Kills, Bits);
        var text = "";
        foreach (var q in quests)
            text += q.Kind + "/" + q.TargetId + "/" + q.Required + "/" + q.BonusBits + ";";
        return text;
    }

    // A retry rolls the same world again with whatever the reloaded segment happens to
    // contain. The book must hand back the very same quests, not a fresh roll.
    private static void DeterministicRollStability()
    {
        var book = FreshBook(7);
        string first = Fingerprint(book, 2);
        for (int retry = 0; retry < 5; retry++)
        {
            string again = Fingerprint(book, 2);
            Assert(first == again, "retry " + retry + " re-rolled optionals: " + first + " vs " + again);
        }
        string other = Fingerprint(FreshBook(7), 2);
        Assert(first == other, "same run seed produced a different roll");
    }

    private static void SeedChangesRoll()
    {
        var book = FreshBook(7);
        string seeded = Fingerprint(book, 2);
        int differing = 0;
        for (int runSeed = 8; runSeed <= 20; runSeed++)
            if (Fingerprint(FreshBook(runSeed), 2) != seeded) differing++;
        Assert(differing > 0, "run seed has no influence on the optional roll");
    }

    private static void OnlyPresentTargetsRolled()
    {
        var book = FreshBook(3);
        var quests = book.Roll(0, Kills, None);
        Assert(quests.Count > 0, "world with enemies rolled no optional at all");
        foreach (var q in quests)
        {
            Assert(q.Kind != OptionalQuestKind.CollectItems, "collect quest rolled with no collectibles in world");
            Assert(Kills.Contains(q.TargetId), "kill quest targets '" + q.TargetId + "' which is not in the world");
        }

        var collectOnly = FreshBook(3).Roll(1, None, Bits);
        Assert(collectOnly.Count > 0, "collectible world rolled no optional at all");
        foreach (var q in collectOnly)
        {
            Assert(q.Kind == OptionalQuestKind.CollectItems, "kill quest rolled with no enemies in world");
            Assert(Bits.Contains(q.TargetId), "collect quest targets '" + q.TargetId + "' which is not in the world");
        }

        AssertEq(FreshBook(3).Roll(2, None, None).Count, 0, "bare world should offer no optional quest");
    }

    private static int IndexOf(OptionalQuestBook book, int world, OptionalQuestKind kind)
    {
        var quests = book.Roll(world, Kills, Bits);
        for (int i = 0; i < quests.Count; i++) if (quests[i].Kind == kind) return i;
        throw new Exception("world " + world + " rolled no " + kind + " quest");
    }

    private static void KillQuestCompletes()
    {
        var book = FreshBook(11);
        int world = 1;
        int idx = IndexOf(book, world, OptionalQuestKind.KillMonsters);
        var quest = book.Roll(world, Kills, Bits)[idx];
        Assert(quest.Required == 3, "kill quest must need 3 kills, needed " + quest.Required);
        Assert(!quest.IsComplete, "quest started already complete");
        for (int i = 1; i <= 3; i++)
        {
            Assert(book.ReportKill(world, quest.TargetId, "enemy#" + i), "kill " + i + " was not credited");
            AssertEq(quest.Progress, i, "progress after " + i + " kills");
        }
        Assert(quest.IsComplete, "three kills did not complete the quest");
        Assert(quest.ProgressLabel.Contains("3/3"), "progress label missing completion: " + quest.ProgressLabel);
    }

    private static void ReportIsIdempotentPerId()
    {
        var book = FreshBook(12);
        int world = 0;
        int idx = IndexOf(book, world, OptionalQuestKind.KillMonsters);
        var quest = book.Roll(world, Kills, Bits)[idx];
        Assert(book.ReportKill(world, quest.TargetId, "enemy#a"), "first report should credit");
        Assert(!book.ReportKill(world, quest.TargetId, "enemy#a"), "duplicate id credited twice");
        AssertEq(quest.Progress, 1, "duplicate id moved progress");
        Assert(!book.ReportKill(world, quest.TargetId, "enemy#a"), "third duplicate credited");
        AssertEq(quest.Progress, 1, "third duplicate moved progress");
        Assert(!book.ReportKill(world, "enemy:not-in-world", "enemy#b"), "off-target kill credited");
        AssertEq(quest.Progress, 1, "off-target kill moved progress");
    }

    private static void CollectQuestCompletes()
    {
        var book = FreshBook(13);
        int world = 3;
        int idx = IndexOf(book, world, OptionalQuestKind.CollectItems);
        var quest = book.Roll(world, Kills, Bits)[idx];
        AssertEq(quest.Required, 3, "collect quest must need 3 items");
        book.ReportCollect(world, quest.TargetId, "bit#1");
        book.ReportCollect(world, quest.TargetId, "bit#2");
        Assert(!quest.IsComplete, "two pickups completed a three pickup quest");
        book.ReportCollect(world, quest.TargetId, "bit#3");
        Assert(quest.IsComplete, "three pickups did not complete the quest");
    }

    private static void CollectIgnoresKills()
    {
        var book = FreshBook(14);
        int world = 2;
        int idx = IndexOf(book, world, OptionalQuestKind.CollectItems);
        var quest = book.Roll(world, Kills, Bits)[idx];
        for (int i = 1; i <= 9; i++) book.ReportKill(world, "enemy:patrol", "enemy#" + i);
        AssertEq(quest.Progress, 0, "kills advanced a collect quest");
    }

    private static void SinglePayout()
    {
        int paid = 0;
        var book = FreshBook(15);
        Action<int> sink = amount => paid += amount;
        OptionalQuestService.PayoutSink = sink;
        try
        {
            int world = 0;
            int idx = IndexOf(book, world, OptionalQuestKind.KillMonsters);
            var quest = book.Roll(world, Kills, Bits)[idx];
            AssertEq(OptionalQuestService.ClaimOptional(book, world, idx), 0, "claiming an unfinished quest paid out");

            book.ReportKill(world, quest.TargetId, "e1");
            book.ReportKill(world, quest.TargetId, "e2");
            book.ReportKill(world, quest.TargetId, "e3");

            int first = OptionalQuestService.ClaimOptional(book, world, idx);
            AssertEq(first, quest.BonusBits, "first claim paid the wrong amount");
            AssertEq(paid, quest.BonusBits, "payout sink received the wrong total");
            Assert(quest.Claimed, "quest not marked claimed");
            AssertEq(OptionalQuestService.ClaimOptional(book, world, idx), 0, "second claim paid again");
            AssertEq(OptionalQuestService.ClaimOptional(book, world, idx), 0, "third claim paid again");
            AssertEq(paid, quest.BonusBits, "bonus granted more than once");
            AssertEq(book.TotalBitsEarned, quest.BonusBits, "book reported more than one payout");
        }
        finally { OptionalQuestService.PayoutSink = null; }
    }

    private static void IncompletePaysNothing()
    {
        var book = FreshBook(16);
        int world = 1;
        int idx = IndexOf(book, world, OptionalQuestKind.CollectItems);
        book.ReportCollect(world, "bit:recycle", "b1");
        AssertEq(OptionalQuestService.ClaimOptional(book, world, idx), 0, "partial collect quest paid out");
    }

    private static void UnknownPayNothing()
    {
        var book = FreshBook(17);
        book.Roll(0, Kills, Bits);
        AssertEq(OptionalQuestService.ClaimOptional(book, 9, 0), 0, "unknown world paid out");
        AssertEq(OptionalQuestService.ClaimOptional(book, 0, 7), 0, "unknown index paid out");
    }

    private static void ResetRerolls()
    {
        var book = FreshBook(18);
        int idx = IndexOf(book, 0, OptionalQuestKind.KillMonsters);
        var afterFirst = book.Roll(0, Kills, Bits);
        var again = book.Roll(0, Kills, Bits);
        Assert(object.ReferenceEquals(afterFirst, again), "re-rolling a live world created a second quest set");
        AssertEq(book.RollCount(0), afterFirst.Count, "roll count mismatch");
        Assert(afterFirst.Count > 0, "live world rolled no optional quest");
        book.Reset(19);
        AssertEq(book.RollCount(0), 0, "reset kept the previous run's quests");
        Assert(book.Get(0, 0) == null, "reset left a stale quest behind");
    }

    // ---- shop ----

    private static void ShopRowShape()
    {
        var wallet = new FakeWallet();
        Assert(ShopCatalog.Entries.Length == 6, "shop should list 6 upgrades, listed " + ShopCatalog.Entries.Length);
        foreach (var entry in ShopCatalog.Entries)
        {
            Assert(!string.IsNullOrEmpty(entry.Label), "upgrade has no label");
            Assert(!string.IsNullOrEmpty(entry.Effect), entry.Label + " has no effect text");
            string row = ShopRules.RowLabel(wallet, entry);
            Assert(row.Contains(entry.Label), "row missing label: " + row);
            Assert(row.Contains(entry.Effect), "row missing effect: " + row);
            Assert(row.Contains("0/3") || row.Contains("Lv"), "row missing current/max level: " + row);
            Assert(row.Contains(ShopRules.NextCost(wallet, entry).ToString()), "row missing price: " + row);
        }
        AssertEq(ShopCatalog.IndexOf(ShopId.WeaponDamage), (int)ShopCatalog.WeaponDamageUpgradeIndex, "catalog index drifted");
    }

    private static void BuyDeducts()
    {
        var wallet = new FakeWallet { Balance = 1000 };
        var entry = ShopCatalog.Find(ShopId.WeaponDamage);
        int cost = ShopRules.NextCost(wallet, entry);
        Assert(cost > 0, "first level has no price");
        Assert(ShopRules.CanBuy(wallet, entry) == PurchaseResult.Success, "affordable upgrade reported as blocked");
        AssertEq((int)ShopRules.Buy(wallet, entry), (int)PurchaseResult.Success, "buy failed");
        AssertEq(wallet.GetLevel(entry.UpgradeIndex), 1, "buy did not raise the level");
        AssertEq(wallet.Balance, 1000 - cost, "buy did not deduct the listed price");
        AssertEq(wallet.SpendCalls, 1, "buy did not go through the wallet spend path exactly once");
    }

    private static void CancelFree()
    {
        var wallet = new FakeWallet { Balance = 1000 };
        int before = wallet.Balance;
        AssertEq((int)ShopRules.Cancel(wallet), (int)PurchaseResult.Success, "cancel did not simply succeed");
        AssertEq(wallet.Balance, before, "cancel spent Data Bits");
        AssertEq(wallet.SpendCalls, 0, "cancel touched the wallet spend path");
        AssertEq(wallet.GetLevel(ShopCatalog.Find(ShopId.WeaponDamage).UpgradeIndex), 0, "cancel changed an upgrade level");
    }

    private static void MaxLevelEnforced()
    {
        var wallet = new FakeWallet { Balance = 100000 };
        var entry = ShopCatalog.Find(ShopId.RunSpeed);
        int max = wallet.GetMaxLevel(entry.UpgradeIndex);
        Assert(max > 0, "upgrade has no max level");
        for (int i = 0; i < max; i++) Assert(ShopRules.Buy(wallet, entry) == PurchaseResult.Success, "level " + (i + 1) + " refused");
        AssertEq(wallet.GetLevel(entry.UpgradeIndex), max, "did not reach max level");
        AssertEq(ShopRules.NextCost(wallet, entry), -1, "maxed upgrade still quotes a price");
        Assert(ShopRules.CanBuy(wallet, entry) == PurchaseResult.MaxLevel, "maxed upgrade reported as buyable");
        int balance = wallet.Balance;
        Assert(ShopRules.Buy(wallet, entry) == PurchaseResult.MaxLevel, "bought past max level");
        AssertEq(wallet.GetLevel(entry.UpgradeIndex), max, "max level exceeded");
        AssertEq(wallet.Balance, balance, "refused max-level purchase still charged");
        Assert(wallet.SpendCalls == max, "max-level refusal tried to spend");
    }

    private static void InsufficientFundsPath()
    {
        var entry = ShopCatalog.Find(ShopId.AttackSpeed);
        int cost;
        var poor = new FakeWallet { Balance = 0 };
        cost = ShopRules.NextCost(poor, entry);
        Assert(ShopRules.CanBuy(poor, entry) == PurchaseResult.InsufficientFunds, "broke player reported as able to buy");
        Assert(ShopRules.Buy(poor, entry) == PurchaseResult.InsufficientFunds, "broke player bought an upgrade");
        AssertEq(poor.GetLevel(entry.UpgradeIndex), 0, "failed purchase raised the level");
        AssertEq(poor.Balance, 0, "failed purchase charged a negative or partial amount");
        AssertEq(poor.SpendCalls, 1, "failed purchase skipped the wallet rejection path");

        poor.Balance = cost - 1;
        Assert(ShopRules.CanBuy(poor, entry) == PurchaseResult.InsufficientFunds, "one bit short reported as affordable");
        Assert(ShopRules.Buy(poor, entry) == PurchaseResult.InsufficientFunds, "one bit short still bought");
        AssertEq(poor.Balance, cost - 1, "one bit short lost bits");

        poor.Balance = cost;
        Assert(ShopRules.Buy(poor, entry) == PurchaseResult.Success, "exact balance could not buy");
        AssertEq(poor.Balance, 0, "exact balance not fully charged");
    }

    private sealed class FakeWallet : IUpgradeWallet
    {
        private readonly Dictionary<int, int> levels = new Dictionary<int, int>();
        private readonly Dictionary<int, int> maxLevels = new Dictionary<int, int>();
        private Dictionary<int, int[]> costs = new Dictionary<int, int[]>();
        private int balance;
        public int SpendCalls;

        public int Balance
        {
            get { return balance; }
            set { balance = value; }
        }

        public FakeWallet()
        {
            for (int i = 0; i < 24; i++)
            {
                levels[i] = 0;
                maxLevels[i] = 3;
                costs[i] = new[] { 100, 200, 400, 800, 1600 };
            }
            maxLevels[ShopCatalog.WeaponDamageUpgradeIndex] = 5;
        }

        public int GetLevel(int upgradeIndex) { return levels[upgradeIndex]; }
        public int GetMaxLevel(int upgradeIndex) { return maxLevels[upgradeIndex]; }
        public int GetCost(int upgradeIndex, int nextLevel) { return costs[upgradeIndex][nextLevel - 1]; }
        public bool CanAfford(int amount) { return amount >= 0 && Balance >= amount; }

        public PurchaseResult Buy(int upgradeIndex)
        {
            if (levels[upgradeIndex] >= maxLevels[upgradeIndex]) return PurchaseResult.MaxLevel;
            int cost = GetCost(upgradeIndex, levels[upgradeIndex] + 1);
            SpendCalls++;
            if (!TrySpend(cost)) return PurchaseResult.InsufficientFunds;
            levels[upgradeIndex]++;
            return PurchaseResult.Success;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount) return false;
            Balance -= amount;
            return true;
        }
    }
}
