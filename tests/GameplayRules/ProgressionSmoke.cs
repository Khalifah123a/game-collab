using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProgressionSmoke
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        var gm = GameManager.Ensure();
        int before = gm.totalDataBits;
        for (int world = 0; world < WorldRules.Count; world++)
        {
            if (!gm.worldsUnlocked[world]) throw new Exception("World was not unlocked: " + world);
            gm.StartRun(world);
            await Task.Delay(300);
            gm.ledger.Collect("flow-reward", 10);
            for (int segment = 0; segment < WorldRules.Segments(world); segment++)
            {
                if (SceneManager.GetActiveScene().name != $"World{world + 1}_Segment{segment + 1}") throw new Exception("Wrong segment progression");
                gm.OnSegmentComplete();
                await Task.Delay(200);
            }
            if (SceneManager.GetActiveScene().name != "Hub" || gm.totalDataBits != before + (world + 1) * 10) throw new Exception("Hub/reward did not match completed world");
            gm.OnSegmentComplete();
            if (gm.totalDataBits != before + (world + 1) * 10) throw new Exception("Repeated completion banked twice");
        }
        gm.ReturnToMenu();
        await Task.Delay(200);
        if (SceneManager.GetActiveScene().name != "Menu") throw new Exception("Return to menu failed");
        return "PASS: Menu, 17 ordered segments, four Hub completions, unlocks, exactly-once banking, return to Menu";
    }
}
