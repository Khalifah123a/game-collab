using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GameplayFeelChecks
{
    public static string Main()
    {
        var results = new List<string>();
        Action<bool, string> check = (ok, label) => results.Add((ok ? "PASS: " : "FAIL: ") + label);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Player.prefab");
        check(player.GetComponent<Rigidbody2D>().interpolation == RigidbodyInterpolation2D.Interpolate, "player interpolates physics motion");
        var movement = player.GetComponent<PlayerController>();
        check(movement.acceleration <= 55 && movement.acceleration >= 30, "walk accelerates over several physics steps");
        check(player.GetComponent("PlayerCombat") != null, "player has melee combat");
        var controller = (AnimatorController)player.GetComponent<Animator>().runtimeAnimatorController;
        check(controller.layers[0].stateMachine.states.Any(s => s.state.name == "Attack" && s.state.motion), "Attack motion exists");
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_Patrol.prefab").GetComponent<Enemy_Patrol>();
        check(enemy.detectionRadius >= 10, "enemy detects at ten units");
        check(typeof(Enemy_Patrol).GetMethod("TakeHit") != null, "enemy receives melee damage");
        var cameraObject = new GameObject("Camera defaults test");
        try { check(cameraObject.AddComponent<CameraFollow>().orthoSize <= 4.5f, "closer camera default"); }
        finally { UnityEngine.Object.DestroyImmediate(cameraObject); }
        foreach (string path in Directory.GetFiles("Assets/_Levels", "World*_Segment*.txt"))
        {
            var rows = File.ReadAllLines(path);
            var surfaces = new List<int>();
            for (int x = 0; x < rows.Max(r => r.Length); x++)
            {
                for (int y = 1; y < rows.Length; y++)
                    if (x < rows[y].Length && rows[y][x] == '#') { surfaces.Add(y); break; }
            }
            check(surfaces.Max() - surfaces.Min() >= 10 && surfaces.Zip(surfaces.Skip(1), (a,b) => a != b ? 1 : 0).Sum() >= 25,
                Path.GetFileNameWithoutExtension(path) + " sustained elevation changes");
        }
        return string.Join("\n", results);
    }
}
