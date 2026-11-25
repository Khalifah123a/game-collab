using System.Collections.Generic;

/// <summary>
/// Per-run reward book. Collectibles use the segment-scoped channel (<see cref="Collect"/>),
/// which is dropped by <see cref="BeginSegment"/> and rolled back to the last checkpoint by
/// <see cref="Retry"/>. Quest rewards use the run-scoped channel (<see cref="CollectRun"/>),
/// which survives segment changes and is never rolled back by a retry, so a hand-in can
/// neither pay twice nor silently lose its reward when the final segment is replayed.
/// </summary>
public sealed class RunLedger
{
    public int Bits { get; private set; }
    private HashSet<string> collected = new HashSet<string>();
    private HashSet<string> saved = new HashSet<string>();
    private readonly HashSet<string> runCollected = new HashSet<string>();
    private int runBits;
    private int savedSegmentBits;
    private int savedRunBits;

    public bool Collect(string id, int amount)
    {
        if (!collected.Add(id)) return false;
        Bits += amount;
        return true;
    }

    /// <summary>Grants a run-scoped reward exactly once, even across segments and retries.</summary>
    public bool CollectRun(string id, int amount)
    {
        if (!runCollected.Add(id)) return false;
        runBits += amount;
        Bits += amount;
        return true;
    }

    public bool IsCollected(string id) { return collected.Contains(id); }
    public bool IsRunCollected(string id) { return runCollected.Contains(id); }

    public bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (Bits < amount) return false;
        Bits -= amount;
        if (savedSegmentBits >= amount)
        {
            savedSegmentBits -= amount;
        }
        else
        {
            int rem = amount - savedSegmentBits;
            savedSegmentBits = 0;
            if (runBits >= rem) runBits -= rem;
            else runBits = 0;
        }
        return true;
    }

    public void BeginSegment()
    {
        collected.Clear();
        saved.Clear();
        savedSegmentBits = Bits - runBits;
    }

    public void SaveCheckpoint()
    {
        savedSegmentBits = Bits - runBits;
        savedRunBits = runBits;
        saved = new HashSet<string>(collected);
    }

    /// <summary>
    /// Restores the segment snapshot taken by the last checkpoint (or the last segment
    /// boundary). Run-scoped quest rewards are deliberately kept exactly as they stand.
    /// </summary>
    public void Retry()
    {
        Bits = savedSegmentBits + runBits;
        collected = new HashSet<string>(saved);
    }

    public int Bank() { int result = Bits; Clear(); return result; }

    public void Clear()
    {
        Bits = 0; runBits = 0; savedSegmentBits = 0; savedRunBits = 0;
        collected.Clear(); saved.Clear(); runCollected.Clear();
    }
}
