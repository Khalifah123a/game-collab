using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class WorldMechanicsSmoke
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        var gm = GameManager.Ensure(); gm.hasCheckpoint = false; gm.currentWorldIndex = 1; gm.LoadSegment(0);
        await Task.Delay(400);
        var gate = UnityEngine.Object.FindObjectsOfType<Gate_Firewall>().First(g => g.requiredPort == 22);
        var player = PlayerHealth.Instance;
        var mode = player.GetComponent<PlayerMode>();
        mode.SetPort(22); mode.SetMode(false);
        player.transform.position = gate.transform.position + Vector3.left * 2;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        await Task.Delay(100);
        if (gate.isOpen) throw new Exception("Encrypted gate accepted plaintext");
        mode.SetMode(true);
        await Task.Delay(100);
        if (!gate.isOpen || gate.GetComponent<Collider2D>().enabled) throw new Exception("Matching encrypted gate did not open");
        gm.hasCheckpoint = false; gm.currentWorldIndex = 3; gm.LoadSegment(4);
        await Task.Delay(400);
        var zone = UnityEngine.Object.FindObjectOfType<GravityZone>();
        var controller = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (!zone || UnityEngine.Object.FindObjectsOfType<TimedPlatform>().Length == 0) throw new Exception("Missing World 4 mechanics");
        controller.transform.position = zone.transform.position + Vector3.right;
        controller.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        await Task.Delay(100);
        if (controller.gravity != 0) throw new Exception("Zero gravity zone did not activate");
        controller.airDashesLeft = 0;
        var dock = UnityEngine.Object.FindObjectsOfType<DockingPoint>()
            .Where(d => d.transform.position.x > zone.transform.position.x + 5)
            .OrderBy(d => Vector3.Distance(d.transform.position, zone.transform.position)).First();
        controller.transform.position = dock.transform.position;
        await Task.Delay(100);
        if (controller.airDashesLeft != 1) throw new Exception("Dock did not restore air dash");
        controller.transform.position = zone.transform.position + Vector3.left * 4;
        await Task.Delay(100);
        if (controller.gravity != 42) throw new Exception("Leaving zero gravity did not restore world gravity");
        gm.hasCheckpoint = false; gm.currentWorldIndex = 0; gm.LoadSegment(0);
        await Task.Delay(350);
        PlayerHealth.Instance.drainRate = 0;
        return "PASS: port/encryption gate rules, solid gate opening, World 4 timed platforms, zero gravity entry/exit, docking dash refill";
    }
}
