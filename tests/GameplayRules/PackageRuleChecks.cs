using System;

/// <summary>
/// Headless assertions for the mandatory package-delivery quest rules.
/// Lives beside Program.cs so the csc checker (Run-Checks.ps1) compiles the
/// pure quest core together with the gameplay models and can prove the state
/// machine without booting Unity.
/// </summary>
public static class PackageRuleChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Offer is reachable from a clean world and idempotent.
        var state = PackageState.None;
        check(PackageQuestRules.TryOffer(state, out state) && state == PackageState.Offered, "giver offers the package from a clean world");
        check(!PackageQuestRules.TryOffer(state, out state) && state == PackageState.Offered, "re-offering an offered package changes nothing");

        // Accept only advances an offered package.
        check(!PackageQuestRules.TryAccept(PackageState.None, out state) && state == PackageState.None,
            "package cannot be accepted before it is offered");
        check(PackageQuestRules.TryAccept(PackageState.Offered, out state) && state == PackageState.Active, "player accepts the offered package");
        check(!PackageQuestRules.TryAccept(PackageState.Active, out state) && state == PackageState.Active, "package cannot be accepted twice");

        // Deliver requires the package in transit and pays exactly once.
        check(!PackageQuestRules.TryDeliver(PackageState.Offered, out state) && state == PackageState.Offered,
            "package cannot be delivered while still at the giver");
        check(!PackageQuestRules.TryDeliver(PackageState.None, out state) && state == PackageState.None,
            "package cannot be delivered from a clean world");
        check(PackageQuestRules.TryDeliver(PackageState.Active, out state) && state == PackageState.Delivered, "receiver takes the delivered package");
        check(!PackageQuestRules.TryDeliver(PackageState.Delivered, out state) && state == PackageState.Delivered,
            "second hand-in is rejected so the reward pays once");
        check(!PackageQuestRules.TryDeliver(PackageState.Claimed, out state) && state == PackageState.Claimed, "claimed packages cannot be re-delivered");

        // Claim closes the loop and is idempotent.
        check(PackageQuestRules.TryClaim(PackageState.Delivered, out state) && state == PackageState.Claimed, "reward is claimed after hand-in");
        check(!PackageQuestRules.TryClaim(PackageState.Claimed, out state) && state == PackageState.Claimed, "reward cannot be claimed twice");
        check(!PackageQuestRules.TryClaim(PackageState.Active, out state) && state == PackageState.Active, "reward cannot be claimed before hand-in");

        // The world gate is open only from Delivered onwards.
        check(!PackageQuestRules.IsDelivered(PackageState.None) && !PackageQuestRules.IsDelivered(PackageState.Offered) && !PackageQuestRules.IsDelivered(PackageState.Active),
            "goal stays locked while the package is uncollected or in transit");
        check(PackageQuestRules.IsDelivered(PackageState.Delivered) && PackageQuestRules.IsDelivered(PackageState.Claimed),
            "delivered and claimed both open the goal");

        // Labels drive the quest UI and never leak an empty string.
        foreach (PackageState current in Enum.GetValues(typeof(PackageState)))
        {
            check(PackageQuestRules.Label(current).Length > 0, "quest label is populated for " + current);
            check(PackageQuestRules.LockedHint(current).Length > 0, "locked hint is populated for " + current);
        }
        check(PackageQuestRules.LockedHint(PackageState.None) != PackageQuestRules.LockedHint(PackageState.Active),
            "locked hint tells the player which half of the delivery is missing");

        // Deterministic seeds: same inputs give the same seed, retries never re-roll it.
        check(PackageQuestRules.DefaultRunSeed(0, 1) == PackageQuestRules.DefaultRunSeed(0, 1), "run seed is deterministic");
        check(PackageQuestRules.DefaultRunSeed(0, 1) != PackageQuestRules.DefaultRunSeed(0, 2), "consecutive runs use different seeds");
        check(PackageQuestRules.DefaultRunSeed(1, 1) != PackageQuestRules.DefaultRunSeed(0, 1), "different worlds use different seeds");
        check(PackageQuestRules.DefaultRunSeed(-1, 0) > 0 && PackageQuestRules.DefaultRunSeed(3, -5) > 0, "run seed stays positive for out-of-range input");
    }
}
