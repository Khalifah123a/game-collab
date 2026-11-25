using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class FloatingPlatformSmoke
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        Time.timeScale = 1;
        var result = new List<string>();
        Action<bool,string> check = (ok,label) => { if (!ok) throw new Exception(label); result.Add("PASS: " + label); };
        var gm = GameManager.Ensure();
        try
        {
            for (int world = 0; world < 4; world++)
            {
                gm.hasCheckpoint = false; gm.ledger.Clear(); gm.currentWorldIndex = world; gm.LoadSegment(0);
                await Task.Delay(300);
                var input = InputManager.Instance; input.enabled = false; input.moveInput = Vector2.zero;
                var player = PlayerHealth.Instance.GetComponent<PlayerController>();
                var body = player.GetComponent<Rigidbody2D>();
                PlayerHealth.Instance.drainRate = 0;
                var platforms = UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None);
                var rest = UnityEngine.Object.FindObjectsByType<Checkpoint_TCP>(FindObjectsSortMode.None)
                    .OrderBy(c => c.transform.position.x).First(c => platforms.Any(p => Supports(p, c.transform.position) && p.GetComponent<BoxCollider2D>().size.x >= 7));
                var support = platforms.First(p => Supports(p, rest.transform.position));
                check(support.GetComponents<Collider2D>().Length == 1, $"World {world+1} checkpoint island has one continuous collider");
                check(!support.GetComponent<TimedPlatform>() && !support.GetComponent<Platform_Moving>(), $"World {world+1} checkpoint support is permanent");
                player.transform.position = rest.transform.position; body.linearVelocity = Vector2.zero;
                await Task.Delay(750);
                check(player.isGrounded, $"World {world+1} floating checkpoint landing");
                if (world >= 2)
                    typeof(Checkpoint_TCP).GetMethod("Activate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rest, null);
                check(gm.hasCheckpoint && rest.isActivated, $"World {world+1} checkpoint snapshot saved");
                Vector3 saved = gm.checkpointPosition;
                gm.RetrySegment();
                await Task.Delay(500);
                player = PlayerHealth.Instance.GetComponent<PlayerController>();
                PlayerHealth.Instance.drainRate = 0;
                check(player.isGrounded && Mathf.Abs(player.transform.position.x - saved.x) < 0.1f && Mathf.Abs(player.transform.position.y - saved.y) < 0.6f,
                    $"World {world+1} retry lands safely on floating island");
            }
            gm.hasCheckpoint = false; gm.ledger.Clear(); gm.currentWorldIndex = 0; gm.LoadSegment(0);
            await Task.Delay(300);
            var controller = PlayerHealth.Instance.GetComponent<PlayerController>();
            var rb = controller.GetComponent<Rigidbody2D>();
            var controls = InputManager.Instance; controls.enabled = false; controls.moveInput = Vector2.zero;
            PlayerHealth.Instance.drainRate = 0;
            var middle = UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None)
                .OrderBy(p => p.transform.position.x).First(p => p.transform.position.x > 20 && Mathf.Approximately(p.GetComponent<BoxCollider2D>().size.x, 3));
            controller.transform.position = middle.transform.position + Vector3.down * 1.5f;
            rb.linearVelocity = Vector2.zero;
            controller.airJumpsLeft = 1;
            await Task.Delay(30);
            controls.jumpPressed = true;
            await Task.Delay(220);
            check(controller.transform.position.y > middle.transform.position.y + 0.7f, "jump passes through underside of floating platform");
            await Task.Delay(500);
            check(controller.isGrounded && Mathf.Abs(controller.transform.position.x - middle.transform.position.x) < 0.1f, "player lands on top of same platform");
            check(gm.ledger.Bits > 0, "item above floating platform is collectible");
            controls.moveInput = Vector2.down;
            await Task.Delay(350);
            check(controller.transform.position.y < middle.transform.position.y - 0.7f, "drop-through works across merged platform");
            controls.moveInput = Vector2.zero;
            var launch = UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None)
                .Where(p => p.transform.position.x < middle.transform.position.x && p.GetComponent<BoxCollider2D>().size.x >= 4)
                .OrderByDescending(p => p.transform.position.x).First();
            controller.transform.position = launch.transform.position + Vector3.up * 0.85f;
            rb.linearVelocity = Vector2.zero;
            await Task.Delay(400);
            check(controller.isGrounded, "jump chain starts on permanent floating ledge");
            controls.moveInput = Vector2.right; controls.jumpPressed = true;
            await Task.Delay(150);
            controls.jumpPressed = true;
            float deadline = Time.time + 1.4f;
            while (Time.time < deadline && rb.position.x < middle.transform.position.x - 0.5f)
                await Task.Delay(10);
            controls.moveInput = Vector2.zero;
            await Task.Delay(450);
            var landing = middle.GetComponent<BoxCollider2D>().bounds;
            check(controller.isGrounded && rb.position.x > landing.min.x && rb.position.x < landing.max.x && Mathf.Abs(rb.position.y - (landing.max.y + 0.5f)) < 0.15f,
                "jump and double jump reach the next raised floating platform");
            return string.Join("\n", result);
        }
        finally
        {
            Time.timeScale = 1; gm.hasCheckpoint = false; gm.ledger.Clear(); gm.currentWorldIndex = 0; gm.LoadSegment(0);
            await Task.Delay(250);
            PlayerHealth.Instance.drainRate = 0;
        }
    }

    private static bool Supports(PlatformEffector2D platform, Vector3 position)
    {
        var bounds = platform.GetComponent<BoxCollider2D>().bounds;
        return position.x > bounds.min.x + 0.5f && position.x < bounds.max.x - 0.5f &&
            position.y > bounds.max.y && position.y - bounds.max.y < 1.2f;
    }
}
