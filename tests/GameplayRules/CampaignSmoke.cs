using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampaignSmoke
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        var report = new List<string>();
        var gm = GameManager.Ensure();
        for (int world = 0; world < WorldRules.Count; world++)
        for (int segment = 0; segment < WorldRules.Segments(world); segment++)
        {
            gm.hasCheckpoint = false; gm.currentWorldIndex = world; gm.ledger.Clear();
            gm.LoadSegment(segment);
            await Task.Delay(350);
            var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
            var health = PlayerHealth.Instance;
            string name = $"World{world + 1}_Segment{segment + 1}";
            if (SceneManager.GetActiveScene().name != name || !player || !health) throw new Exception("Failed to load " + name);
            if (health.ttlMax != WorldRules.TTL(world) || health.isDead || !player.isGrounded) throw new Exception("Invalid spawn/resources: " + name);
            report.Add(name + ": spawn, TTL " + health.ttlMax + ", " + GameplayValidation.CheckRuntimeHUD());
        }
        gm.hasCheckpoint = false; gm.currentWorldIndex = 0; gm.ledger.Clear(); gm.LoadSegment(0);
        await Task.Delay(300);
        PlayerHealth.Instance.drainRate = 0;
        return string.Join("\n", report);
    }
}
