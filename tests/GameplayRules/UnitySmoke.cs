using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class UnitySmoke
{
    public static async Task<string> Main()
    {
        var results = new List<string>();
        Application.runInBackground = true;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAIL: " + name); results.Add("PASS: " + name); };
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (!Application.isPlaying || !player) throw new Exception("Open World1_Segment1 in Play Mode first.");
        var input = InputManager.Instance;
        var health = player.GetComponent<PlayerHealth>();
        var body = player.GetComponent<Rigidbody2D>();
        input.enabled = false;
        health.drainRate = 0;
        input.moveInput = Vector2.zero;
        player.transform.position = new Vector3(3, -17, 0);
        body.linearVelocity = Vector2.zero;
        var wall = new GameObject("SmokeWall");
        wall.transform.position = new Vector3(6, -16, 0);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 4);
        try
        {
            await Task.Delay(300);
            check(player.isGrounded, "ground detection at spawn");
            input.dashDirection = Vector2.right; input.dashPressed = true;
            await Task.Delay(250);
            check(player.transform.position.x > 3.5f && player.transform.position.x < 5.7f, "dash stops before thin wall");
            check(player.GetComponent<BoxCollider2D>().enabled, "dash retains body collider");
            check(player.GetComponent<PlayerStamina>().Current < 90, "dash consumes stamina in Play Mode");
            UnityEngine.Object.Destroy(wall);
            player.transform.position = new Vector3(3, -17, 0); body.linearVelocity = Vector2.zero;
            await Task.Delay(250);
            input.jumpPressed = true;
            await Task.Delay(120);
            float firstHeight = player.transform.position.y;
            check(firstHeight > -16, "ground jump gains height");
            input.jumpPressed = true;
            await Task.Delay(100);
            check(player.airJumpsLeft == 0 && player.transform.position.y > firstHeight, "double jump accepted in air");
            player.transform.position = new Vector3(3, -17, 0); body.linearVelocity = Vector2.zero;
            await Task.Delay(450);
            float before = health.ttlCurrent;
            health.TakeDamage(15, PlayerHealth.DamageType.TTLDamage);
            health.TakeDamage(15, PlayerHealth.DamageType.TTLDamage);
            check(Mathf.Approximately(before - health.ttlCurrent, 15), "invulnerability prevents repeated contact damage");
            Time.timeScale = 0;
            float pausedTTL = health.ttlCurrent, pausedStamina = player.GetComponent<PlayerStamina>().Current;
            await Task.Delay(200);
            check(health.ttlCurrent == pausedTTL && player.GetComponent<PlayerStamina>().Current == pausedStamina, "pause freezes resources");
            Time.timeScale = 1;
            results.Add(GameplayValidation.CheckRuntimeHUD());
            var enemyObject = new GameObject("SmokeEnemy");
            enemyObject.transform.position = player.transform.position + Vector3.right;
            enemyObject.AddComponent<BoxCollider2D>(); enemyObject.AddComponent<Rigidbody2D>();
            var enemy = enemyObject.AddComponent<Enemy_Patrol>();
            await Task.Delay(100);
            check(enemy.currentState == Enemy_Patrol.State.Windup && enemy.GetComponent<Rigidbody2D>().linearVelocity == Vector2.zero, "enemy stops and telegraphs attack");
            check(enemy.GetComponent<BoxCollider2D>().isTrigger, "enemy body cannot push player");
            UnityEngine.Object.Destroy(enemyObject);
            var gm = GameManager.Instance;
            gm.ledger.Clear(); gm.ledger.BeginSegment(); gm.ledger.Collect("smoke-bit", 10);
            gm.SaveCheckpoint("smoke-checkpoint", new Vector3(3, -17, 0), 60);
            gm.ledger.Collect("after", 20);
            gm.RetrySegment();
            await Task.Delay(650);
            var respawn = PlayerHealth.Instance;
            check(gm.ledger.Bits == 10 && !gm.ledger.IsCollected("after"), "retry restores checkpoint bits");
            check(respawn.ttlCurrent > 68 && respawn.ttlCurrent <= 70 && respawn.transform.position.x < 4, "retry restores safe position and TTL floor");
            return string.Join("\n", results);
        }
        finally
        {
            Time.timeScale = 1;
            if (wall) UnityEngine.Object.Destroy(wall);
            if (input) input.enabled = true;
            if (health) health.drainRate = 1;
        }
    }
}
