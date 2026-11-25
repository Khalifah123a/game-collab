using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage 2 play-mode smoke for the mandatory package-delivery quest and the world gate.
///
/// Covers: accept -> deliver -> gate opens, gate locked before delivery, package progress
/// surviving scene reloads and RetrySegment, the run seed not being re-rolled, and the
/// hand-in reward paying exactly once. Optional-quest work is deliberately absent here:
/// it must not be able to move the gate this suite locks onto.
/// </summary>
public static class Stage2PackageSmoke
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        var results = new List<string>();
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAIL: " + name); results.Add("PASS: " + name); };

        var gm = GameManager.Ensure();
        var quests = QuestService.Ensure();
        const int world = 0;
        int segments = WorldRules.Segments(world);
        int finalSegment = segments - 1;

        // ---- singleton contract --------------------------------------------------------
        check(quests.gameObject == gm.gameObject, "quest service rides the persistent GameManager object");
        check(quests.gameObject.scene.name == "DontDestroyOnLoad", "quest service is on a DontDestroyOnLoad object");
        QuestService.Ensure();
        QuestService.Ensure();
        check(UnityEngine.Object.FindObjectsOfType<QuestService>().Length == 1, "repeated Ensure never creates a second quest service");

        // Clean slate for the mandatory quest in this world.
        gm.worldsUnlocked[world] = true;
        gm.packageStates[world] = PackageState.None;
        gm.packageRewardsPaid[world] = false;

        // ---- world start: giver is always on the main path ---------------------------
        gm.StartRun(world);
        await Task.Delay(400);
        check(quests.GetPackageState(world) == PackageState.Offered, "world start offers the package");
        var giver = UnityEngine.Object.FindObjectOfType<PackageGiverNPC>();
        check(giver != null && giver.worldIndex == world, "world-start safe zone has the package giver");
        check(UnityEngine.Object.FindObjectOfType<PackageReceiverNPC>() == null, "receiver only exists at the world end");

        check(giver.GetChoices().Count == 1 && giver.GetChoices()[0].Contains("Accept"), "giver offers an accept dialog choice");
        giver.InteractFromRange();
        await Task.Delay(100);
        check(quests.GetPackageState(world) == PackageState.Active, "giver hands the package over on dialog choice");

        // ---- gate locked before delivery ----------------------------------------------
        gm.currentSegmentIndex = finalSegment;
        gm.hasCheckpoint = false;
        gm.LoadSegment(finalSegment);
        await Task.Delay(400);
        check(SceneManager.GetActiveScene().name == $"World{world + 1}_Segment{segments}", "final segment loaded");
        var receiver = UnityEngine.Object.FindObjectOfType<PackageReceiverNPC>();
        check(receiver != null && receiver.worldIndex == world, "world-end safe zone has the package receiver");
        check(quests.GetPackageState(world) == PackageState.Active, "package stays in transit across the segment change");

        var lockedManager = UnityEngine.Object.FindObjectOfType<LevelManager>();
        check(lockedManager != null && lockedManager.IsFinalSegment, "level manager recognises the final segment");
        check(lockedManager.IsGoalLocked && !quests.IsPackageDelivered(world), "final goal is locked while the package is in transit");

        lockedManager.CompleteSegment();
        await Task.Delay(250);
        check(!lockedManager.isComplete, "locked final goal refuses to complete the world");
        check(SceneManager.GetActiveScene().name == $"World{world + 1}_Segment{segments}", "locked final goal keeps the player in the segment");
        check(!string.IsNullOrEmpty(lockedManager.LastGateHint), "locked final goal publishes a quest hint");
        check(lockedManager.LastGateHint.Contains("receiver"), "locked hint points at the missing receiver hand-in");
        check(UnityEngine.Object.FindObjectOfType<ServerB_Goal>().GateLocked, "goal reports itself locked to the rest of the game");

        // ---- persistence across reload and retry ---------------------------------------
        int seedBefore = gm.runSeed;
        int ledgerBitsBefore = gm.ledger.Bits;
        gm.LoadSegment(finalSegment);
        await Task.Delay(400);
        check(quests.GetPackageState(world) == PackageState.Active, "package survives a scene reload");

        gm.RetrySegment();
        await Task.Delay(650);
        check(quests.GetPackageState(world) == PackageState.Active, "RetrySegment does not reset package progress");
        check(gm.runSeed == seedBefore, "RetrySegment does not re-randomize the run seed");
        check(gm.ledger.Bits == ledgerBitsBefore, "RetrySegment leaves the ledger snapshot consistent");
        check(UnityEngine.Object.FindObjectOfType<PackageReceiverNPC>() != null, "receiver is back on the main path after a retry");

        // ---- deliver, then the gate opens ---------------------------------------------
        int runBitsBefore = gm.ledger.Bits;
        receiver = UnityEngine.Object.FindObjectOfType<PackageReceiverNPC>();
        receiver.InteractFromRange();
        await Task.Delay(150);
        check(quests.IsPackageDelivered(world), "receiver accepts the hand-in");
        check(gm.ledger.Bits == runBitsBefore + quests.packageRewardBits, "hand-in pays the package reward once");
        check(quests.ClaimPackage(world) && quests.GetPackageState(world) == PackageState.Claimed, "reward is claimable exactly once");
        check(!quests.ClaimPackage(world), "a second claim is rejected");

        // ---- no double reward ----------------------------------------------------------
        receiver = UnityEngine.Object.FindObjectOfType<PackageReceiverNPC>();
        receiver.InteractFromRange();
        await Task.Delay(150);
        check(!quests.DeliverPackage(world), "repeat hand-in is rejected");
        check(!quests.AcceptPackage(world), "a delivered package cannot be accepted again");
        check(gm.ledger.Bits == runBitsBefore + quests.packageRewardBits, "repeat hand-in pays nothing");
        check(quests.GetPackageState(world) == PackageState.Claimed, "quest state stays claimed after repeat hand-ins");

        lockedManager = UnityEngine.Object.FindObjectOfType<LevelManager>();
        check(!lockedManager.IsGoalLocked, "delivered package unlocks the final goal");
        check(string.IsNullOrEmpty(quests.GetLockedHint(world)) || quests.GetLockedHint(world) == "GOAL OPEN", "unlocked goal drops the locked hint");

        lockedManager.CompleteSegment();
        await Task.Delay(400);
        check(lockedManager.isComplete, "final goal completes the world once the package arrived");
        check(gm.totalDataBits >= quests.packageRewardBits, "world reward including the hand-in was banked");
        check(gm.worldsUnlocked[world + 1], "unlocking the next world still works");

        // ---- world completion is not re-randomized or reset ----------------------------
        gm.currentWorldIndex = world;
        gm.currentSegmentIndex = 0;
        gm.hasCheckpoint = false;
        gm.LoadSegment(0);
        await Task.Delay(400);
        check(quests.IsPackageDelivered(world), "replaying a world keeps the delivered package");
        var replayReceiver = UnityEngine.Object.FindObjectOfType<PackageReceiverNPC>();
        check(replayReceiver == null && UnityEngine.Object.FindObjectOfType<PackageGiverNPC>() != null, "replay spawns only the giver at the start");
        check(!quests.AcceptPackage(world) && !quests.DeliverPackage(world), "a replayed world cannot re-run the mandatory quest");

        // ---- untouched worlds are still blocked ---------------------------------------
        for (int other = 1; other < WorldRules.Count; other++)
            check(!quests.IsPackageDelivered(other), "world " + other + " package is untouched");

        gm.ReturnToMenu();
        await Task.Delay(300);
        check(SceneManager.GetActiveScene().name == "Menu", "return to menu failed");

        return string.Join("\n", results);
    }
}
