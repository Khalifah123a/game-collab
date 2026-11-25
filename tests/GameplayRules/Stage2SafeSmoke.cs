using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Play Mode smoke coverage for the Safe Zone contract.
/// Inside a safe zone the courier must be untouchable: enemies drop their aggro,
/// hazards and lasers deal nothing, and the TTL drain pauses. Run with
/// Unity CLI <c>run_script --file Stage2SafeSmoke.cs --entry Main</c> in Play Mode.
/// </summary>
public static class Stage2SafeSmoke
{
    private static readonly Vector2 ZoneSize = new Vector2(6f, 4f);
    private static readonly float TTL_Epsilon = 0.05f;

    public static async Task<string> Main()
    {
        var zoneType = typeof(Enemy_Patrol).Assembly.GetType("SafeZone");
        if (zoneType == null) throw new Exception("FAIL: SafeZone is not implemented");
        var safeFlag = zoneType.GetProperty("IsPlayerSafe", BindingFlags.Public | BindingFlags.Static);
        if (safeFlag == null || !safeFlag.CanRead)
            throw new Exception("FAIL: SafeZone.IsPlayerSafe static property is missing");
        Func<bool> playerSafe = () => (bool)safeFlag.GetValue(null, null);

        Application.runInBackground = true;
        Time.timeScale = 1;
        var report = new List<string>();
        var created = new List<GameObject>();
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception("FAIL: " + label); report.Add("PASS: " + label); };

        Func<Vector2, Vector2, Component> spawnZone = (pos, size) =>
        {
            var go = new GameObject("Safe smoke zone"); created.Add(go);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var zone = go.AddComponent(zoneType);
            Physics2D.SyncTransforms();
            return zone;
        };
        Func<Vector2, GameObject> floor = pos =>
        {
            var go = new GameObject("Safe smoke floor"); created.Add(go);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(24f, 1f);
            Physics2D.SyncTransforms();
            return go;
        };
        Func<Vector2, Hazard_Spike> spawnSpike = pos =>
        {
            var go = new GameObject("Safe smoke spike"); created.Add(go);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.7f, 0.7f);
            Physics2D.SyncTransforms();
            return go.AddComponent<Hazard_Spike>();
        };
        Func<Vector2, Laser_Beam> spawnLaser = pos =>
        {
            var go = new GameObject("Safe smoke laser"); created.Add(go);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.4f, 4f);
            Physics2D.SyncTransforms();
            var laser = go.AddComponent<Laser_Beam>();
            laser.onDuration = 999f;
            laser.offDuration = 1f;
            laser.damageAmount = 30f;
            laser.damageCooldown = 0.2f;
            return laser;
        };
        Func<Vector2, Enemy_Patrol> spawnPatrol = pos =>
        {
            var go = new GameObject("Safe smoke patrol"); created.Add(go);
            go.transform.position = pos;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            go.AddComponent<Rigidbody2D>();
            var enemy = go.AddComponent<Enemy_Patrol>();
            enemy.damageAmount = 15f;
            Physics2D.SyncTransforms();
            return enemy;
        };
        Func<Vector2, Enemy_Flying> spawnFlying = pos =>
        {
            var go = new GameObject("Safe smoke flyer"); created.Add(go);
            go.transform.position = pos;
            go.AddComponent<CircleCollider2D>().radius = 0.35f;
            go.AddComponent<Rigidbody2D>();
            var enemy = go.AddComponent<Enemy_Flying>();
            enemy.damageAmount = 12f;
            Physics2D.SyncTransforms();
            return enemy;
        };

        var gm = GameManager.Ensure();
        try
        {
            check(!playerSafe(), "no safe zone in the world means the courier is exposed");
            gm.StartRun(0); await Task.Delay(400);
            var health = PlayerHealth.Instance;
            var player = health.GetComponent<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            InputManager.Instance.enabled = false;
            InputManager.Instance.moveInput = Vector2.zero;
            player.enabled = false;
            player.FacingDir = 1;
            body.constraints = RigidbodyConstraints2D.FreezeAll;

            Vector2 origin = new Vector2(2000, 10);
            void ParkPlayer()
            {
                body.position = origin;
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
            }
            ParkPlayer();
            floor(origin + Vector2.down * 0.65f);
            await Task.Delay(120);

            // --- Trigger counter: enter, overlap, leave -------------------------------
            var zoneA = spawnZone(origin, ZoneSize);
            var zoneB = spawnZone(origin, ZoneSize);
            await Task.Delay(150);
            check(playerSafe(), "entering a safe zone marks the courier safe");
            check(zoneA.GetComponent<BoxCollider2D>().isTrigger, "safe zone uses a trigger collider and never blocks the courier");
            check(zoneA.GetComponentInChildren<SpriteRenderer>(true) != null, "safe zone draws a placeholder visual");
            UnityEngine.Object.Destroy(zoneB.gameObject);
            await Task.Delay(120);
            check(playerSafe(), "overlapping safe zones keep the courier safe until the last one is left");
            UnityEngine.Object.Destroy(zoneA.gameObject);
            await Task.Delay(120);
            check(!playerSafe(), "leaving the last safe zone restores normal danger");

            // --- TTL drain pauses ------------------------------------------------------
            health.drainRate = 5f;
            health.ttlCurrent = 80f;
            var safeZone = spawnZone(origin, ZoneSize);
            await Task.Delay(400);
            check(Mathf.Abs(health.ttlCurrent - 80f) < TTL_Epsilon, "TTL does not drain while the courier is safe");
            UnityEngine.Object.Destroy(safeZone.gameObject);
            await Task.Delay(400);
            check(health.ttlCurrent < 80f - 0.5f, "TTL drain resumes once the courier leaves the safe zone");
            health.drainRate = 0f;

            // --- Spikes: dangerous outside, inert inside ------------------------------
            ParkPlayer();
            var outsideSpike = spawnSpike(origin);
            outsideSpike.ttlDamage = 20f;
            outsideSpike.damageType = PlayerHealth.DamageType.TTLDamage;
            Physics2D.SyncTransforms();
            float beforeSpike = health.ttlCurrent;
            await Task.Delay(250);
            check(beforeSpike - health.ttlCurrent > 15f, "spikes hurt the courier outside a safe zone");
            UnityEngine.Object.Destroy(outsideSpike.gameObject);
            await Task.Delay(900);

            safeZone = spawnZone(origin, ZoneSize);
            var insideSpike = spawnSpike(origin);
            insideSpike.ttlDamage = 20f;
            insideSpike.damageType = PlayerHealth.DamageType.TTLDamage;
            Physics2D.SyncTransforms();
            float beforeInsideSpike = health.ttlCurrent;
            await Task.Delay(250);
            check(Mathf.Abs(health.ttlCurrent - beforeInsideSpike) < TTL_Epsilon, "spikes deal no damage inside a safe zone");

            var killerSpike = spawnSpike(origin + Vector2.left);
            killerSpike.damageType = PlayerHealth.DamageType.InstaKill;
            Physics2D.SyncTransforms();
            await Task.Delay(250);
            check(!health.isDead, "an instakill hazard cannot kill the courier inside a safe zone");
            UnityEngine.Object.Destroy(killerSpike.gameObject);
            UnityEngine.Object.Destroy(insideSpike.gameObject);
            UnityEngine.Object.Destroy(safeZone.gameObject);
            await Task.Delay(150);

            // --- Laser: dangerous outside, inert inside --------------------------------
            ParkPlayer();
            var outsideLaser = spawnLaser(origin);
            float beforeLaser = health.ttlCurrent;
            await Task.Delay(300);
            check(beforeLaser - health.ttlCurrent > 15f, "an active laser burns the courier outside a safe zone");
            UnityEngine.Object.Destroy(outsideLaser.gameObject);
            await Task.Delay(900);

            safeZone = spawnZone(origin, ZoneSize);
            var insideLaser = spawnLaser(origin);
            Physics2D.SyncTransforms();
            float beforeInsideLaser = health.ttlCurrent;
            await Task.Delay(500);
            check(Mathf.Abs(health.ttlCurrent - beforeInsideLaser) < TTL_Epsilon, "an active laser deals no damage inside a safe zone");
            UnityEngine.Object.Destroy(insideLaser.gameObject);
            UnityEngine.Object.Destroy(safeZone.gameObject);
            await Task.Delay(150);

            // --- Patrol enemy loses aggro while the courier is safe ---------------------
            ParkPlayer();
            var patrol = spawnPatrol(origin + new Vector2(1.4f, 0f));
            await Until(() => patrol.currentState == Enemy_Patrol.State.Windup, 1500);
            float beforePatrol = health.ttlCurrent;
            await Task.Delay(300);
            check(beforePatrol - health.ttlCurrent > 10f, "a patrolling enemy lands its swing outside a safe zone");

            safeZone = spawnZone(origin, ZoneSize);
            await Task.Delay(400);
            check(patrol.currentState == Enemy_Patrol.State.Patrol, "a safe zone sends a winding-up patrol enemy back to patrol");
            check(Mathf.Abs(health.ttlCurrent - beforePatrol) < TTL_Epsilon, "a patrol enemy cannot damage the courier inside a safe zone");
            UnityEngine.Object.Destroy(safeZone.gameObject);
            await Until(() => patrol.currentState == Enemy_Patrol.State.Windup || patrol.currentState == Enemy_Patrol.State.Chase, 1500);
            check(true, "patrol aggro returns as soon as the courier leaves the safe zone");
            UnityEngine.Object.Destroy(patrol.gameObject);
            await Task.Delay(120);

            // --- Flying enemy loses aggro while the courier is safe ---------------------
            ParkPlayer();
            var flyer = spawnFlying(origin + new Vector2(2f, 1f));
            await Until(() => flyer.currentState == Enemy_Flying.State.Windup || flyer.currentState == Enemy_Flying.State.Attack, 2000);
            float beforeFlyer = health.ttlCurrent;
            await Task.Delay(300);
            check(beforeFlyer - health.ttlCurrent > 8f, "a diving flyer deals its damage outside a safe zone");

            safeZone = spawnZone(origin, ZoneSize);
            await Task.Delay(400);
            check(flyer.currentState == Enemy_Flying.State.Patrol, "a safe zone cancels a committed dive and returns the flyer to patrol");
            check(Mathf.Abs(health.ttlCurrent - beforeFlyer) < TTL_Epsilon, "a diving flyer cannot damage the courier inside a safe zone");
            UnityEngine.Object.Destroy(safeZone.gameObject);
            await Until(() => flyer.currentState == Enemy_Flying.State.Windup || flyer.currentState == Enemy_Flying.State.Chase, 2000);
            check(true, "flying aggro returns as soon as the courier leaves the safe zone");
            UnityEngine.Object.Destroy(flyer.gameObject);
            await Task.Delay(120);

            check(!playerSafe(), "the safe zone counter unwinds cleanly once every zone is gone");
            return string.Join("\n", report);
        }
        finally
        {
            Time.timeScale = 1;
            foreach (var go in created) if (go) UnityEngine.Object.Destroy(go);
            // Never let cleanup throw: it would mask the assertion failure we actually care about.
            if (Application.isPlaying) gm.ReturnToMenu();
        }
    }

    private static async Task Until(Func<bool> predicate, int timeout)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!predicate())
        {
            if (watch.ElapsedMilliseconds > timeout) throw new Exception("Timed out waiting for safe zone enemy state");
            await Task.Delay(10);
        }
    }
}
