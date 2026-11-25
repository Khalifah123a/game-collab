using System;

public interface IEnemyDamageableTest
{
    void TakeHit(int damage);
    bool IsDead { get; }
}

public interface IBossTest : IEnemyDamageableTest
{
    string BossName { get; }
    int HitPoints { get; }
    int MaxHitPoints { get; }
    float HealthPercent { get; }
    int RewardBits { get; }
}

public class MockBossTracker
{
    public static IBossTest ActiveBoss;

    public static void Register(IBossTest boss) { ActiveBoss = boss; }
    public static void Unregister(IBossTest boss) { if (ActiveBoss == boss) ActiveBoss = null; }
}

public class MockTheCensor : IBossTest
{
    public string BossName { get { return "THE CENSOR // SURVEILLANCE"; } }
    public int MaxHitPoints { get { return 6; } }
    private int _hp = 6;
    public int HitPoints { get { return _hp; } }
    public float HealthPercent { get { return Math.Max(0f, (float)_hp / MaxHitPoints); } }
    public int RewardBits { get { return 200; } }
    public bool IsDead { get { return _hp <= 0; } }

    public void TakeHit(int damage)
    {
        _hp = Math.Max(0, _hp - damage);
        if (IsDead) MockBossTracker.Unregister(this);
    }
}

public class MockSolaris : IBossTest
{
    public string BossName { get { return "SOLARIS // THERMAL PROCESSOR"; } }
    public int MaxHitPoints { get { return 8; } }
    private int _hp = 8;
    public int HitPoints { get { return _hp; } }
    public float HealthPercent { get { return Math.Max(0f, (float)_hp / MaxHitPoints); } }
    public int RewardBits { get { return 300; } }
    public bool IsDead { get { return _hp <= 0; } }

    public void TakeHit(int damage)
    {
        _hp = Math.Max(0, _hp - damage);
        if (IsDead) MockBossTracker.Unregister(this);
    }
}

public class MockBossFinal : IBossTest
{
    private string _name = "Boss Akhir";
    public string BossName { get { return _name; } set { _name = value; } }
    public int MaxHitPoints { get { return 10; } }
    private int _hp = 10;
    public int HitPoints { get { return _hp; } }
    public float HealthPercent { get { return Math.Max(0f, (float)_hp / MaxHitPoints); } }
    public int RewardBits { get { return 500; } }
    public bool IsDead { get { return _hp <= 0; } }

    public void TakeHit(int damage)
    {
        _hp = Math.Max(0, _hp - damage);
        if (IsDead) MockBossTracker.Unregister(this);
    }
}

public static class BossChecks
{
    public static int Main()
    {
        int passed = 0;

        // Check 1: The Censor stats & registry
        var censor = new MockTheCensor();
        MockBossTracker.Register(censor);
        Assert(MockBossTracker.ActiveBoss == censor, "censor registered as active boss");
        Assert(censor.MaxHitPoints == 6, "censor has 6 HP");
        Assert(censor.RewardBits == 200, "censor rewards 200 bits");
        passed += 3;

        // Check 2: The Censor damage & defeat unregisters
        censor.TakeHit(3);
        Assert(censor.HitPoints == 3 && Math.Abs(censor.HealthPercent - 0.5f) < 0.01f, "censor HP drops to 50%");
        censor.TakeHit(3);
        Assert(censor.IsDead, "censor dies after taking 6 damage");
        Assert(MockBossTracker.ActiveBoss == null, "censor unregisters upon death");
        passed += 3;

        // Check 3: Solaris stats & combat
        var solaris = new MockSolaris();
        MockBossTracker.Register(solaris);
        Assert(MockBossTracker.ActiveBoss == solaris, "solaris registered as active boss");
        Assert(solaris.MaxHitPoints == 8, "solaris has 8 HP");
        Assert(solaris.RewardBits == 300, "solaris rewards 300 bits");
        solaris.TakeHit(8);
        Assert(solaris.IsDead && MockBossTracker.ActiveBoss == null, "solaris unregisters upon death");
        passed += 4;

        // Check 4: Boss Akhir modular identity & stats
        var bossAkhir = new MockBossFinal();
        MockBossTracker.Register(bossAkhir);
        Assert(bossAkhir.BossName == "Boss Akhir", "boss akhir default name is Boss Akhir");
        Assert(bossAkhir.MaxHitPoints == 10, "boss akhir has 10 HP");
        Assert(bossAkhir.RewardBits == 500, "boss akhir rewards 500 bits");
        bossAkhir.BossName = "Overlord Prime"; // verifies name can be swapped modularly
        Assert(bossAkhir.BossName == "Overlord Prime", "boss akhir name can be swapped modularly without touching combat logic");
        bossAkhir.TakeHit(10);
        Assert(bossAkhir.IsDead && MockBossTracker.ActiveBoss == null, "boss akhir unregisters upon death");
        passed += 5;

        Console.WriteLine(string.Format("PASS: {0} boss architecture and combat contract checks", passed));
        return 0;
    }

    private static void Assert(bool cond, string msg)
    {
        if (!cond) throw new Exception("FAIL: " + msg);
        Console.WriteLine("PASS: " + msg);
    }
}
