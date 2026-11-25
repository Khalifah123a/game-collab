using System;
using System.Collections.Generic;

// Unity-free core for Stage 2 optional quests and the quest payout.
//
// Everything here is plain C# so it can be compiled and exercised by the
// standalone tests in tests/GameplayRules without booting Unity.
//
// Determinism contract
// --------------------
//  * A run is identified by GameManager.runSeed (constant for the whole run).
//  * Per world the seed is exactly world * 1000 + runSeed.
//  * The roll for a world happens at most once. Death / retry / segment change
//    must never re-roll, so the book caches the result and hands the same list
//    back. Only a new run (BeginRun with a new runSeed) clears it.
//
// The roll only ever picks from targets that the world actually spawned, so a
// quest can never reference an enemy or collectible that is not there.

public enum OptionalQuestKind
{
    KillMonsters = 0,
    CollectItems = 1
}

/// <summary>One optional quest in one world. Progress is capped and idempotent per instance id.</summary>
public sealed class OptionalQuest
{
    private readonly HashSet<string> creditedInstances = new HashSet<string>();

    public int World { get; private set; }
    public int Index { get; private set; }
    public OptionalQuestKind Kind { get; private set; }
    public string TargetId { get; private set; }
    public int Required { get; private set; }
    public int BonusBits { get; private set; }
    public int Progress { get; private set; }
    public bool Claimed { get; private set; }

    public bool IsComplete { get { return Progress >= Required; } }
    public bool CanClaim { get { return IsComplete && !Claimed; } }

    public string Label
    {
        get
        {
            return Kind == OptionalQuestKind.KillMonsters
                ? "Hapus " + Required + " " + TargetId
                : "Kumpulkan " + Required + " " + TargetId;
        }
    }

    public string ProgressLabel
    {
        get
        {
            string text = Label + " " + Progress + "/" + Required;
            if (Claimed) return text + " (selesai)";
            return CanClaim ? text + " (siap diklaim)" : text;
        }
    }

    internal OptionalQuest(int world, int index, OptionalQuestKind kind, string targetId, int required, int bonusBits)
    {
        World = world;
        Index = index;
        Kind = kind;
        TargetId = targetId;
        Required = required;
        BonusBits = bonusBits;
    }

    /// <summary>Credits progress once per instanceId. Returns true only the first time.</summary>
    internal bool Credit(string instanceId)
    {
        if (Claimed || IsComplete) return false;
        if (string.IsNullOrEmpty(instanceId)) return false;
        if (!creditedInstances.Add(instanceId)) return false;
        Progress++;
        return true;
    }

    internal void MarkClaimed() { Claimed = true; }
}

public sealed class OptionalQuestBook
{
    public const int KillRequired = 3;
    public const int CollectRequired = 3;
    public const int MinBonusBits = 40;
    public const int MaxBonusBits = 120;
    public const int MaxQuestsPerWorld = 2;

    private readonly Dictionary<int, List<OptionalQuest>> rolled = new Dictionary<int, List<OptionalQuest>>();
    private int runSeed;

    public int RunSeed { get { return runSeed; } }

    public void Reset(int newRunSeed)
    {
        runSeed = newRunSeed;
        rolled.Clear();
    }

    /// <summary>
    /// Returns this world's 0..2 optional quests, rolling them on first call only.
    /// Repeated calls (retry, death, segment change) return the cached list.
    /// </summary>
    public List<OptionalQuest> Roll(int world, IList<string> availableKillTargets, IList<string> availableCollectTargets)
    {
        List<OptionalQuest> cached;
        if (rolled.TryGetValue(world, out cached)) return cached;

        var quests = new List<OptionalQuest>(MaxQuestsPerWorld);
        var rng = new DeterministicRandom(SeedFor(world, runSeed));

        string killTarget = Pick(availableKillTargets, rng);
        if (killTarget != null)
            quests.Add(new OptionalQuest(world, quests.Count, OptionalQuestKind.KillMonsters, killTarget, KillRequired, Bonus(rng)));

        string collectTarget = Pick(availableCollectTargets, rng);
        if (collectTarget != null)
            quests.Add(new OptionalQuest(world, quests.Count, OptionalQuestKind.CollectItems, collectTarget, CollectRequired, Bonus(rng)));

        rolled[world] = quests;
        return quests;
    }

    public OptionalQuest Get(int world, int index)
    {
        List<OptionalQuest> quests;
        if (!rolled.TryGetValue(world, out quests)) return null;
        if (index < 0 || index >= quests.Count) return null;
        return quests[index];
    }

    public int Count(int world)
    {
        List<OptionalQuest> quests;
        return rolled.TryGetValue(world, out quests) ? quests.Count : 0;
    }

    public int RollCount(int world)
    {
        return Count(world);
    }

    /// <summary>Total bonus bits already granted in this run.</summary>
    public int TotalBitsEarned { get; private set; }

    internal void MarkEarned(int bits) { TotalBitsEarned += bits; }

    public bool ReportKill(int world, string targetId, string instanceId)
    {
        return Report(world, OptionalQuestKind.KillMonsters, targetId, instanceId);
    }

    public bool ReportCollect(int world, string targetId, string instanceId)
    {
        return Report(world, OptionalQuestKind.CollectItems, targetId, instanceId);
    }

    private bool Report(int world, OptionalQuestKind kind, string targetId, string instanceId)
    {
        List<OptionalQuest> quests;
        if (!rolled.TryGetValue(world, out quests)) return false;
        foreach (var quest in quests)
        {
            if (quest.Kind != kind) continue;
            if (!string.Equals(quest.TargetId, targetId, StringComparison.Ordinal)) continue;
            return quest.Credit(instanceId);
        }
        return false;
    }

    /// <summary>World seed for a given run. Exposed so tests and runtime agree on the formula.</summary>
    public static int SeedFor(int world, int runSeed)
    {
        return world * 1000 + runSeed;
    }

    private static string Pick(IList<string> candidates, DeterministicRandom rng)
    {
        if (candidates == null || candidates.Count == 0) return null;
        return candidates[rng.Next(candidates.Count)];
    }

    private static int Bonus(DeterministicRandom rng)
    {
        return rng.Range(MinBonusBits, MaxBonusBits);
    }
}

/// <summary>
/// Runtime facade. GameManager owns the instance and forwards its run lifecycle
/// here; the payout sink is GameManager.AddDataBits so money flows through the
/// single existing currency path.
/// </summary>
public static class OptionalQuestService
{
    /// <summary>Set by GameManager to AddDataBits.</summary>
    public static Action<int> PayoutSink;

    private static OptionalQuestBook book = new OptionalQuestBook();

    public static int RunSeed { get { return book.RunSeed; } }

    /// <summary>Call once per run. Re-seeding starts a fresh optional roll.</summary>
    public static void BeginRun(int newRunSeed)
    {
        book.Reset(newRunSeed);
    }

    public static List<OptionalQuest> Roll(int world, IList<string> availableKillTargets, IList<string> availableCollectTargets)
    {
        return book.Roll(world, availableKillTargets, availableCollectTargets);
    }

    public static OptionalQuest Get(int world, int index)
    {
        return book.Get(world, index);
    }

    public static bool ReportKill(int world, string targetId, string instanceId)
    {
        return book.ReportKill(world, targetId, instanceId);
    }

    public static bool ReportCollect(int world, string targetId, string instanceId)
    {
        return book.ReportCollect(world, targetId, instanceId);
    }

    public static string ProgressLabel(int world, int index)
    {
        OptionalQuest quest = book.Get(world, index);
        return quest == null ? string.Empty : quest.ProgressLabel;
    }

    /// <summary>
    /// Pays the bonus exactly once. Returns the amount paid, or 0 when the quest
    /// is missing, unfinished, or already claimed.
    /// </summary>
    public static int ClaimOptional(int world, int index)
    {
        OptionalQuest quest = book.Get(world, index);
        if (quest == null || !quest.CanClaim) return 0;

        quest.MarkClaimed();
        book.MarkEarned(quest.BonusBits);

        Action<int> sink = PayoutSink;
        if (sink != null) sink(quest.BonusBits);
        return quest.BonusBits;
    }

    public static int ClaimOptional(OptionalQuestBook bookRef, int world, int index)
    {
        if (bookRef == null) return 0;
        OptionalQuest quest = bookRef.Get(world, index);
        if (quest == null || !quest.CanClaim) return 0;

        quest.MarkClaimed();
        bookRef.MarkEarned(quest.BonusBits);

        Action<int> sink = PayoutSink;
        if (sink != null) sink(quest.BonusBits);
        return quest.BonusBits;
    }
}

/// <summary>xorshift32. Small, deterministic, and identical on every platform.</summary>
internal sealed class DeterministicRandom
{
    private uint state;

    public DeterministicRandom(int seed)
    {
        state = unchecked((uint)seed);
        if (state == 0) state = 0x9E3779B9u;
    }

    public uint NextUInt()
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    /// <summary>Uniform-ish value in [0, count).</summary>
    public int Next(int count)
    {
        if (count <= 1) return 0;
        return (int)(NextUInt() % (uint)count);
    }

    /// <summary>Inclusive on both ends.</summary>
    public int Range(int minInclusive, int maxInclusive)
    {
        uint span = (uint)(maxInclusive - minInclusive + 1);
        return minInclusive + (int)(NextUInt() % span);
    }
}