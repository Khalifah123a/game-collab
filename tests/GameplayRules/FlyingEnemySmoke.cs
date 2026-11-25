using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class FlyingEnemySmoke
{
    public static async Task<string> Main()
    {
        var type = typeof(Enemy_Patrol).Assembly.GetType("Enemy_Flying");
        if (type == null) throw new Exception("FAIL: Enemy_Flying is not implemented");
        Application.runInBackground = true;
        Time.timeScale = 1;
        var report = new List<string>();
        var created = new List<GameObject>();
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception("FAIL: " + label); report.Add("PASS: " + label); };
        Action<Component, string, object> set = (c, field, value) => type.GetField(field).SetValue(c, value);
        Func<Component, string> state = c => type.GetField("currentState").GetValue(c).ToString();
        Func<Component, int> hp = c => (int)type.GetField("hitPoints").GetValue(c);
        Func<Vector2, Component> spawn = pos =>
        {
            var go = new GameObject("Flying smoke enemy"); created.Add(go);
            go.transform.position = pos;
            go.AddComponent<CircleCollider2D>().radius = 0.35f;
            go.AddComponent<Rigidbody2D>();
            return go.AddComponent(type);
        };
        Func<Vector2, Vector2, GameObject> wall = (pos, size) =>
        {
            var go = new GameObject("Flying smoke wall"); created.Add(go);
            go.transform.position = pos; go.AddComponent<BoxCollider2D>().size = size;
            Physics2D.SyncTransforms(); return go;
        };
        var gm = GameManager.Ensure();
        try
        {
            gm.StartRun(0); await Task.Delay(350);
            var player = PlayerHealth.Instance.GetComponent<PlayerController>();
            var health = PlayerHealth.Instance;
            var body = player.GetComponent<Rigidbody2D>();
            var combat = player.GetComponent<PlayerCombat>();
            InputManager.Instance.enabled = false;
            InputManager.Instance.moveInput = Vector2.zero;
            health.drainRate = 0;
            player.enabled = false;
            player.FacingDir = 1;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            Vector2 origin = new Vector2(2000, 10);
            body.position = origin; body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            var far = spawn(origin + new Vector2(8, 5));
            await Task.Delay(220);
            check(state(far) == "Chase", "detects player eight units away with vertical separation");
            check(far.transform.position.x < origin.x + 7.9f && far.transform.position.y < origin.y + 4.9f, "pursuit moves smoothly in both axes without ground");
            check(far.GetComponent<Rigidbody2D>().gravityScale == 0 && far.GetComponent<Collider2D>().isTrigger, "flying body does not fall or push the player");
            body.position = origin - Vector2.right * 35; Physics2D.SyncTransforms();
            await Task.Delay(850);
            check(state(far) == "Return" || state(far) == "Patrol", "lost target beyond leash returns to patrol");
            UnityEngine.Object.Destroy(far.gameObject);
            body.position = origin; Physics2D.SyncTransforms();

            var barrier = wall(origin + new Vector2(4, 2), new Vector2(0.25f, 16));
            var hidden = spawn(origin + new Vector2(8, 3));
            await Task.Delay(300);
            check(state(hidden) == "Patrol", "solid wall prevents acquiring player");
            UnityEngine.Object.Destroy(hidden.gameObject); UnityEngine.Object.Destroy(barrier);
            await Task.Delay(40);

            var contact = spawn(origin);
            set(contact, "detectionRadius", 0f);
            float ttl = health.ttlCurrent;
            await Task.Delay(220);
            check(Mathf.Approximately(ttl, health.ttlCurrent), "passive overlap does not inflict contact damage");
            UnityEngine.Object.Destroy(contact.gameObject);

            var attacker = spawn(origin + new Vector2(2.3f, 1));
            await Until(() => state(attacker) == "Windup", 1200);
            check(Mathf.Approximately(ttl, health.ttlCurrent), "windup warns before damage");
            check(attacker.GetComponentInChildren<TextMesh>(true).gameObject.activeSelf, "attack warning is visible");
            Time.timeScale = 0;
            Vector3 paused = attacker.transform.position;
            await Task.Delay(200);
            check(state(attacker) == "Windup" && Vector3.Distance(paused, attacker.transform.position) < 0.01f, "pause freezes flight and attack timer");
            Time.timeScale = 1;
            await Until(() => health.ttlCurrent < ttl, 1600);
            check(Mathf.Approximately(ttl - health.ttlCurrent, 12), "dive deals one configured TTL hit");
            await Until(() => state(attacker) == "Recovery", 1200);
            float afterHit = health.ttlCurrent;
            await Task.Delay(200);
            check(state(attacker) == "Recovery" && Mathf.Approximately(afterHit, health.ttlCurrent), "recovery prevents repeated contact hits");
            UnityEngine.Object.Destroy(attacker.gameObject);
            await Task.Delay(100);

            var dodged = spawn(origin + new Vector2(2.3f, 1));
            await Until(() => state(dodged) == "Attack", 1500);
            body.position = origin + Vector2.up * 6; Physics2D.SyncTransforms();
            await Task.Delay(450);
            check(Mathf.Approximately(afterHit, health.ttlCurrent) && dodged.transform.position.y < origin.y + 2, "dive locks direction instead of tracking an evasive jump");
            UnityEngine.Object.Destroy(dodged.gameObject);
            body.position = origin; Physics2D.SyncTransforms();

            var stopped = spawn(origin + new Vector2(2.6f, 0));
            await Until(() => state(stopped) == "Windup", 1000);
            var thinWall = wall(origin + Vector2.right * 1.2f, new Vector2(0.08f, 8));
            await Task.Delay(900);
            check(stopped.transform.position.x > origin.x + 1.55f, "swept flight cannot cross a thin wall");
            check(Mathf.Approximately(afterHit, health.ttlCurrent), "attack cannot damage through a newly placed wall");
            UnityEngine.Object.Destroy(stopped.gameObject); UnityEngine.Object.Destroy(thinWall);
            await Task.Delay(80);

            var target = spawn(origin + Vector2.right * 1.25f);
            set(target, "detectionRadius", 0f); set(target, "patrolRadius", 0f);
            target.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            check(combat.TryAttack(), "player can initiate an airborne melee strike");
            await Task.Delay(260);
            check(hp(target) == 1, "one swing hits flying target once even with multiple colliders");
            check(state(target) == "Stunned" || state(target) == "Recovery", "hit interrupts the flying enemy");
            await Task.Delay(240);
            combat.TryAttack(); await Task.Delay(300);
            check(!target || hp(target) == 0, "second melee strike defeats ordinary flying enemy");
            combat.CancelAttack();

            var shielded = spawn(origin + Vector2.right * 1.5f);
            set(shielded, "detectionRadius", 0f); set(shielded, "patrolRadius", 0f);
            wall(origin + Vector2.right * 0.75f, new Vector2(0.1f, 3));
            combat.TryAttack(); await Task.Delay(260);
            check(hp(shielded) == 2, "player melee cannot strike flying enemy through terrain");
            return string.Join("\n", report);
        }
        finally
        {
            Time.timeScale = 1;
            foreach (var go in created) if (go) UnityEngine.Object.Destroy(go);
            gm.ReturnToMenu();
        }
    }

    private static async Task Until(Func<bool> predicate, int timeout)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!predicate())
        {
            if (watch.ElapsedMilliseconds > timeout) throw new Exception("Timed out waiting for flying enemy state");
            await Task.Delay(10);
        }
    }
}
