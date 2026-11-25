/// <summary>
/// Lifecycle of the single mandatory package quest in one world.
/// Ordered so a plain comparison expresses "the package already arrived".
/// </summary>
public enum PackageState
{
    None = 0,
    Offered = 1,
    Active = 2,
    Delivered = 3,
    Claimed = 4
}

/// <summary>
/// Pure rules for the mandatory package-delivery quest. Kept free of UnityEngine
/// so the state machine can be verified headlessly by the csc checker and reused
/// by the optional-quest sibling without touching the world gate.
/// </summary>
public static class PackageQuestRules
{
    /// <summary>None -> Offered. Already-offered or in-flight packages are left alone.</summary>
    public static bool TryOffer(PackageState current, out PackageState next)
    {
        if (current == PackageState.None)
        {
            next = PackageState.Offered;
            return true;
        }
        next = current;
        return false;
    }

    /// <summary>Offered -> Active. The player cannot accept a package that was never offered.</summary>
    public static bool TryAccept(PackageState current, out PackageState next)
    {
        if (current == PackageState.Offered)
        {
            next = PackageState.Active;
            return true;
        }
        next = current;
        return false;
    }

    /// <summary>Active -> Delivered. Only a package in transit can be handed in, and only once.</summary>
    public static bool TryDeliver(PackageState current, out PackageState next)
    {
        if (current == PackageState.Active)
        {
            next = PackageState.Delivered;
            return true;
        }
        next = current;
        return false;
    }

    /// <summary>Delivered -> Claimed. Claiming twice is a no-op so the reward pays once.</summary>
    public static bool TryClaim(PackageState current, out PackageState next)
    {
        if (current == PackageState.Delivered)
        {
            next = PackageState.Claimed;
            return true;
        }
        next = current;
        return false;
    }

    /// <summary>The world gate opens once the package reached the receiver.</summary>
    public static bool IsDelivered(PackageState state)
    {
        return state >= PackageState.Delivered;
    }

    /// <summary>Short quest UI label for the current package state.</summary>
    public static string Label(PackageState state)
    {
        switch (state)
        {
            case PackageState.Offered: return "PACKAGE: OFFERED";
            case PackageState.Active: return "PACKAGE: CARRY IT";
            case PackageState.Delivered: return "PACKAGE: DELIVERED";
            case PackageState.Claimed: return "PACKAGE: COMPLETE";
            default: return "PACKAGE: NONE";
        }
    }

    /// <summary>Message shown by the locked final goal, pointing at whichever step is missing.</summary>
    public static string LockedHint(PackageState state)
    {
        switch (state)
        {
            case PackageState.Active:
                return "GOAL LOCKED - hand the packet to the receiver at the exit safe zone";
            case PackageState.Delivered:
            case PackageState.Claimed:
                return "GOAL OPEN";
            default:
                return "GOAL LOCKED - collect the packet from the giver at the start safe zone";
        }
    }

    /// <summary>
    /// Deterministic run seed derived from the world and run ordinal, so a run can be
    /// reproduced exactly while retries and segment changes keep the seed untouched.
    /// </summary>
    public static int DefaultRunSeed(int worldIndex, int sequence)
    {
        unchecked
        {
            int mixed = worldIndex * 7919 + sequence * 104729 + 1;
            mixed = mixed * 1103515245 + 12345;
            mixed ^= mixed >> 13;
            mixed = mixed * 1103515245 + 12345;
            mixed ^= mixed >> 15;
            return (mixed & 0x3FFFFFFF) + 1;
        }
    }
}
