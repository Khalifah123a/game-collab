using System;
using System.Reflection;

class Program
{
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }

    static void Main()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var staminaType = assembly.GetType("StaminaState");
        Check(staminaType != null, "stamina gameplay model exists");
        dynamic stamina = Activator.CreateInstance(staminaType);
        Check(stamina.TrySpend(25f), "dash can spend stamina");
        Check(stamina.Current == 75f, "one dash costs 25");
        Check(!stamina.TrySpend(80f) && stamina.Current == 75f, "rejected action has no cost");
        stamina.Tick(0.5f);
        Check(stamina.Current == 75f, "regen waits for delay");
        stamina.Tick(0.75f);
        Check(Math.Abs((float)stamina.Current - 92.5f) < 0.001f, "regen only uses elapsed time beyond delay");
        stamina.Tick(10f);
        Check(stamina.Current == 100f, "regen clamps to capacity");
        Check(!stamina.TrySpend(-1f), "negative costs rejected");

        var ledgerType = assembly.GetType("RunLedger");
        Check(ledgerType != null, "run ledger exists");
        dynamic ledger = Activator.CreateInstance(ledgerType);
        ledger.BeginSegment();
        Check(ledger.Collect("bit-a", 10), "first collectible grants reward");
        Check(!ledger.Collect("bit-a", 10), "duplicate trigger gives no reward");
        ledger.SaveCheckpoint();
        ledger.Collect("bit-b", 20);
        ledger.Retry();
        Check(ledger.Bits == 10 && ledger.IsCollected("bit-a") && !ledger.IsCollected("bit-b"), "retry restores checkpoint snapshot");
        ledger.Collect("bit-b", 20);
        ledger.BeginSegment();
        ledger.Collect("next", 50);
        ledger.Retry();
        Check(ledger.Bits == 30, "retry without checkpoint preserves earlier segments only");
        Check(ledger.Bank() == 30 && ledger.Bank() == 0, "world reward banked exactly once");

        ledger.Collect("spendable", 100);
        Check(ledger.Spend(40) && ledger.Bits == 60, "ledger spend deducts balance");
        Check(!ledger.Spend(100), "insufficient ledger balance rejects spend");
        Check(ledger.Spend(60) && ledger.Bits == 0, "ledger spend deducts full remaining balance");

        dynamic runLedger = Activator.CreateInstance(assembly.GetType("RunLedger"));
        runLedger.BeginSegment();
        Check(runLedger.CollectRun("quest:package:0", 250), "run-scoped reward grants once");
        Check(!runLedger.CollectRun("quest:package:0", 250) && runLedger.Bits == 250, "duplicate run reward is rejected");
        runLedger.SaveCheckpoint();
        runLedger.BeginSegment();
        Check(runLedger.Bits == 250, "run-scoped reward survives a segment change");
        Check(!runLedger.IsRunCollected("bit") && runLedger.IsRunCollected("quest:package:0"), "segment ids stay separate from run ids");
        runLedger.Retry();
        Check(runLedger.Bits == 250, "retry after delivery keeps the reward paid");
        Check(runLedger.Bank() == 250 && runLedger.Bank() == 0, "run-scoped reward banks exactly once");

        PackageRuleChecks.Run(Check);
    }
}
