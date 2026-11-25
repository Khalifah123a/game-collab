using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class CombatSmoke
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        var report = new List<string>();
        Action<bool,string> check = (ok,label) => { if (!ok) throw new Exception("FAIL: " + label); report.Add("PASS: " + label); };
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        var combat = player.GetComponent<PlayerCombat>();
        var health = player.GetComponent<PlayerHealth>();
        var body = player.GetComponent<Rigidbody2D>();
        var input = InputManager.Instance;
        var created = new List<GameObject>();
        input.enabled = false;
        health.drainRate = 0;
        input.moveInput = Vector2.zero;
        Func<Vector2,Enemy_Patrol> spawn = position =>
        {
            var go = new GameObject("Combat smoke enemy"); created.Add(go);
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>(); go.AddComponent<Rigidbody2D>();
            var enemy = go.AddComponent<Enemy_Patrol>();
            enemy.stationary = true;
            enemy.detectionRadius = 0;
            return enemy;
        };
        try
        {
            input.jumpPressed = true; input.ConsumeJump();
            check(!input.jumpPressed, "consuming jump clears pending press");
            player.transform.position = new Vector3(4, -17, 0); body.linearVelocity = Vector2.zero;
            await Task.Delay(250);
            player.FacingDir = 1;
            var front = spawn(new Vector2(5.4f, -17));
            var back = spawn(new Vector2(2.6f, -17));
            check(combat.TryAttack(), "melee begins");
            check(!combat.TryAttack(), "repeated clicks cannot bypass recovery");
            await Task.Delay(160);
            check(front.hitPoints == 1 && front.currentState == Enemy_Patrol.State.Stunned, "front target hit once and staggered");
            check(back.hitPoints == 2, "target behind not hit");
            check(player.GetComponentInChildren<LineRenderer>().enabled, "slash is rendered during active frames");
            check(player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack"), "Attack animation plays");
            await Task.Delay(300);
            check(front.hitPoints == 1, "one swing cannot repeatedly damage same target");
            check(combat.TryAttack(), "attack available after recovery");
            await Task.Delay(280);
            check(!front || front.hitPoints == 0, "second swing defeats ordinary enemy");
            UnityEngine.Object.Destroy(back.gameObject);
            await Task.Delay(180);
            input.attackPressed = true;
            await Task.Delay(40);
            check(combat.IsAttacking && !player.isDashing, "attack input does not trigger dash");
            combat.CancelAttack();
            var shielded = spawn(new Vector2(5.5f, -17));
            var wall = new GameObject("Combat smoke wall"); created.Add(wall);
            wall.transform.position = new Vector3(4.75f, -17, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.15f, 3);
            Physics2D.SyncTransforms();
            combat.TryAttack(); await Task.Delay(250);
            check(shielded.hitPoints == 2, "melee cannot damage through wall");
            UnityEngine.Object.Destroy(wall); UnityEngine.Object.Destroy(shielded.gameObject);
            await Task.Delay(220);
            Time.timeScale = 0;
            check(!combat.TryAttack(), "pause rejects attack");
            Time.timeScale = 1;
            player.isFrozen = true; check(!combat.TryAttack(), "frozen player cannot attack"); player.isFrozen = false;
            player.isDashing = true; check(!combat.TryAttack(), "dash cannot start melee"); player.isDashing = false;
            health.isDead = true; check(!combat.TryAttack(), "dead player cannot attack"); health.isDead = false;
            player.transform.position = new Vector3(3, -17, 0); body.linearVelocity = Vector2.zero;
            input.moveInput = Vector2.right;
            await Task.Delay(55);
            check(body.linearVelocity.x > 0 && body.linearVelocity.x < player.GetComponent<PlayerMode>().GetSpeed(), "movement accelerates gradually");
            await Task.Delay(220);
            check(body.linearVelocity.x > 7.5f, "movement reaches intended full speed");
            input.moveInput = Vector2.zero;
            await Task.Delay(220);
            check(Mathf.Abs(body.linearVelocity.x) < 0.1f, "movement brakes without lingering drift");
            player.transform.position = new Vector3(3, -17, 0); body.linearVelocity = Vector2.zero;
            var distant = spawn(new Vector2(11, -17)); distant.stationary = false; distant.detectionRadius = 10;
            await Task.Delay(150);
            check(distant.currentState == Enemy_Patrol.State.Chase && distant.GetComponent<Rigidbody2D>().linearVelocity.x < 0, "enemy detects player eight units away");
            await Task.Delay(600);
            check(distant.transform.position.x < 8.5f, "chase extends beyond former patrol bounds");
            check(Camera.main.orthographicSize <= 4.6f, "runtime camera is closer");
            return string.Join("\n", report);
        }
        finally
        {
            Time.timeScale = 1; input.moveInput = Vector2.zero; input.enabled = true;
            player.isFrozen = false; player.isDashing = false; health.isDead = false; health.drainRate = 1;
            combat.CancelAttack();
            foreach (var go in created) if (go) UnityEngine.Object.Destroy(go);
        }
    }
}
