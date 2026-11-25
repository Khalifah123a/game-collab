using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class TraversalSmoke
{
    public static Task<string> Main() { return Run(new[] { 0 }); }
    public static Task<string> AllWorlds() { return Run(new[] { 0, 1, 2, 3 }); }
    public static Task<string> FourthWorld() { return Run(new[] { 3 }); }

    private static async Task<string> Run(int[] worlds)
    {
        Application.runInBackground = true;
        var report = new List<string>();
        try
        {
            foreach (int world in worlds)
            {
                var gm = GameManager.Ensure(); gm.hasCheckpoint = false; gm.currentWorldIndex = world;
                gm.LoadSegment(0);
                await Task.Delay(300);
                var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
                var input = InputManager.Instance; input.enabled = false;
                var body = player.GetComponent<Rigidbody2D>();
                var health = PlayerHealth.Instance; health.drainRate = 0;
                // This measures traversability, not combat balance or gate-solving ability.
                foreach (var enemy in UnityEngine.Object.FindObjectsOfType<Enemy_Patrol>()) enemy.gameObject.SetActive(false);
                foreach (var spike in UnityEngine.Object.FindObjectsOfType<Hazard_Spike>()) spike.gameObject.SetActive(false);
                foreach (var laser in UnityEngine.Object.FindObjectsOfType<Laser_Beam>()) laser.gameObject.SetActive(false);
                foreach (var gate in UnityEngine.Object.FindObjectsOfType<Gate_Firewall>()) gate.gameObject.SetActive(false);
                var goal = UnityEngine.Object.FindObjectOfType<ServerB_Goal>(); float target = goal.transform.position.x;
                goal.gameObject.SetActive(false);
                Time.timeScale = 3;
                float start = Time.time, lastJump = -10, furthest = 0, stuckSince = Time.time;
                int jumps = 0;
                var trace = new Queue<string>();
                float nextTrace = 0;
                while (player && !health.isDead && body.position.x < target - 1 && Time.time - start < 180)
                {
                    input.moveInput = Vector2.right;
                    input.jumpPressed = false;
                    float x = body.position.x, y = body.position.y;
                    bool step = Solid(new Vector2(x + 1.8f, y), Vector2.right, 0.5f);
                    bool gap = !Solid(new Vector2(x + 2, y), Vector2.down, 2);
                    if (!player.isGrounded && body.linearVelocity.y < 0 && gap && Solid(new Vector2(x, y), Vector2.down, 4))
                        input.moveInput = Vector2.zero;
                    if (player.isGrounded && (step || gap) && Time.time - lastJump > 0.25f)
                    { input.jumpPressed = true; lastJump = Time.time; jumps++; }
                    else if (!player.isGrounded && !player.isOnWall && player.airJumpsLeft > 0 && Time.time - lastJump > 0.13f &&
                        (Solid(new Vector2(x + 1.5f, y), Vector2.right, 1) || body.linearVelocity.y < 1 && gap))
                    { input.jumpPressed = true; lastJump = Time.time; jumps++; }
                    if (x > furthest + 0.5f) { furthest = x; stuckSince = Time.time; }
                    if (Time.time >= nextTrace)
                    {
                        trace.Enqueue($"x={x:0.0} y={y:0.0} v={body.linearVelocity} ground={player.isGrounded} wall={player.isOnWall} air={player.airJumpsLeft} move={input.moveInput.x} jump={input.jumpPressed}");
                        if (trace.Count > 18) trace.Dequeue();
                        nextTrace = Time.time + 0.15f;
                    }
                    if (Time.time - stuckSince > 5) break;
                    await Task.Delay(16);
                }
                input.moveInput = Vector2.zero; input.jumpPressed = false; input.enabled = true;
                bool finished = player && !health.isDead && body.position.x >= target - 1;
                report.Add((finished ? "PASS" : "FAIL") + $": World{world+1} traversal x={body.position.x:0.0}/{target:0}, y={body.position.y:0.0}, grounded={player.isGrounded}, wall={player.isOnWall}, jumps={jumps}, simulatedTime={Time.time-start:0.0}s");
                if (!finished) report.Add(string.Join("\n", trace));
                Time.timeScale = 1;
            }
            return string.Join("\n", report);
        }
        finally
        {
            Time.timeScale = 1;
            var gm = GameManager.Ensure(); gm.hasCheckpoint = false; gm.currentWorldIndex = 0; gm.LoadSegment(0);
            await Task.Delay(250);
            PlayerHealth.Instance.drainRate = 0;
        }
    }

    private static bool Solid(Vector2 origin, Vector2 direction, float distance)
    {
        return Physics2D.RaycastAll(origin, direction, distance).Any(hit => !hit.collider.isTrigger && !hit.collider.GetComponentInParent<PlayerController>());
    }
}
