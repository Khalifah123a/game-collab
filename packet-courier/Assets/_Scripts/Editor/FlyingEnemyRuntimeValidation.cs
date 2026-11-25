#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public static class FlyingEnemyRuntimeValidation
{
    private const string ActiveKey = "PacketCourier.FlyingEnemyValidation.Active";
    private const string FinishedKey = "PacketCourier.FlyingEnemyValidation.Finished";
    private const string PassedKey = "PacketCourier.FlyingEnemyValidation.Passed";

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeListener()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Run()
    {
        if (SessionState.GetBool(ActiveKey, false) || EditorApplication.isPlaying)
            throw new InvalidOperationException("Flying enemy validation requires Edit Mode.");

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(PassedKey, false);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
        {
            var host = new GameObject("FlyingEnemyRuntimeValidation");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<FlyingEnemyRuntimeValidationRunner>();
        }
        else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(FinishedKey, false))
        {
            bool passed = SessionState.GetBool(PassedKey, false);
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseBool(FinishedKey);
            SessionState.EraseBool(PassedKey);
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    public static void Finish(bool success, string message)
    {
        SessionState.SetBool(PassedKey, success);
        SessionState.SetBool(FinishedKey, true);
        if (success) Debug.Log("[Flying runtime validation] PASS: " + message);
        else Debug.LogError("[Flying runtime validation] FAIL: " + message);
        EditorApplication.isPlaying = false;
    }
}

public sealed class FlyingEnemyRuntimeValidationRunner : MonoBehaviour
{
    private PlayerHealth health;
    private PlayerController player;
    private Rigidbody2D playerBody;
    private PlayerCombat combat;
    private InputManager input;

    private IEnumerator Start()
    {
        GameManager.Ensure().StartRun(0);
        yield return WaitFor(() => PlayerHealth.Instance != null && UnityEngine.Object.FindAnyObjectByType<Enemy_Flying>() != null, 4f);
        if (PlayerHealth.Instance == null)
        {
            Fail("World 1 did not load a player.");
            yield break;
        }

        health = PlayerHealth.Instance;
        player = health.GetComponent<PlayerController>();
        playerBody = player.GetComponent<Rigidbody2D>();
        combat = player.GetComponent<PlayerCombat>();
        input = InputManager.Instance;
        if (player == null || playerBody == null || combat == null || input == null)
        {
            Fail("Player combat setup is incomplete.");
            yield break;
        }

        input.enabled = false;
        player.enabled = false;
        health.drainRate = 0;
        playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
        playerBody.position = new Vector2(10000, 10);
        Physics2D.SyncTransforms();

        yield return StartCoroutine(CheckDetectionAndChase());
        if (!enabled) yield break;
        yield return StartCoroutine(CheckWallSight());
        if (!enabled) yield break;
        yield return StartCoroutine(CheckDiveAndRecovery());
        if (!enabled) yield break;
        yield return StartCoroutine(CheckPlayerMelee());
        if (!enabled) yield break;

        FlyingEnemyRuntimeValidation.Finish(true, "detection, wall sight, dive, recovery, and player melee");
    }

    private IEnumerator CheckDetectionAndChase()
    {
        var enemy = Spawn(new Vector2(10008, 15));
        Vector2 initial = enemy.transform.position;
        yield return new WaitForSeconds(0.3f);
        if (enemy.currentState != Enemy_Flying.State.Chase || enemy.transform.position.x >= initial.x || enemy.transform.position.y >= initial.y)
        {
            Fail("Flying enemy did not acquire and chase a player through open air.");
            yield break;
        }
        if (enemy.GetComponent<Rigidbody2D>().gravityScale != 0 || !enemy.GetComponent<Collider2D>().isTrigger)
        {
            Fail("Flying enemy physics can fall or push the player.");
            yield break;
        }
        Destroy(enemy.gameObject);
        yield return null;
    }

    private IEnumerator CheckWallSight()
    {
        var wall = new GameObject("Flying validation wall");
        wall.transform.position = new Vector2(10004, 12);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.25f, 16);
        var enemy = Spawn(new Vector2(10008, 13));
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.3f);
        if (enemy.currentState != Enemy_Flying.State.Patrol)
        {
            Fail("Flying enemy detected a player through solid terrain.");
            yield break;
        }
        Destroy(enemy.gameObject);
        Destroy(wall);
        yield return null;
    }

    private IEnumerator CheckDiveAndRecovery()
    {
        var enemy = Spawn(new Vector2(10002.3f, 11));
        float initialTtl = health.ttlCurrent;
        yield return WaitFor(() => enemy.currentState == Enemy_Flying.State.Windup, 1.5f);
        if (enemy.currentState != Enemy_Flying.State.Windup || !enemy.GetComponentInChildren<TextMesh>(true).gameObject.activeSelf)
        {
            Fail("Flying enemy did not show a dive warning.");
            yield break;
        }
        yield return WaitFor(() => health.ttlCurrent < initialTtl, 2f);
        if (!Mathf.Approximately(initialTtl - health.ttlCurrent, enemy.damageAmount))
        {
            Fail("Flying dive did not deal exactly one configured TTL hit.");
            yield break;
        }
        float ttlAfterHit = health.ttlCurrent;
        yield return new WaitForSeconds(0.35f);
        if (enemy.currentState != Enemy_Flying.State.Recovery || !Mathf.Approximately(ttlAfterHit, health.ttlCurrent))
        {
            Fail("Flying enemy did not recover safely after a dive.");
            yield break;
        }
        Destroy(enemy.gameObject);
        yield return null;
    }

    private IEnumerator CheckPlayerMelee()
    {
        var enemy = Spawn(new Vector2(10001.25f, 10));
        enemy.detectionRadius = 0;
        enemy.patrolRadius = 0;
        var secondCollider = enemy.gameObject.AddComponent<BoxCollider2D>();
        secondCollider.isTrigger = true;
        Physics2D.SyncTransforms();

        if (!combat.TryAttack())
        {
            Fail("Player could not start an airborne melee strike.");
            yield break;
        }
        yield return new WaitForSeconds(0.3f);
        if (enemy.hitPoints != 1)
        {
            Fail("One melee swing did not hit the flying enemy exactly once.");
            yield break;
        }
        yield return new WaitForSeconds(0.25f);
        combat.TryAttack();
        yield return new WaitForSeconds(0.35f);
        if (enemy != null && enemy.hitPoints != 0)
        {
            Fail("Second melee strike did not defeat a normal flying enemy.");
            yield break;
        }
    }

    private Enemy_Flying Spawn(Vector2 position)
    {
        var go = new GameObject("Flying validation enemy");
        go.transform.position = position;
        go.AddComponent<CircleCollider2D>().radius = 0.35f;
        go.AddComponent<Rigidbody2D>();
        return go.AddComponent<Enemy_Flying>();
    }

    private IEnumerator WaitFor(Func<bool> condition, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    private void Fail(string reason)
    {
        FlyingEnemyRuntimeValidation.Finish(false, reason);
        enabled = false;
    }
}
#endif
