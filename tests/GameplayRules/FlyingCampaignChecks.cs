using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FlyingCampaignChecks
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new Exception("Save open scene first");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_Flying.prefab");
        if (!prefab) throw new Exception("FAIL: flying enemy prefab is missing");
        var type = typeof(Enemy_Patrol).Assembly.GetType("Enemy_Flying");
        if (type == null) throw new Exception("FAIL: Enemy_Flying is missing");
        var report = new List<string>();
        try
        {
            for (int world = 0; world < WorldRules.Count; world++)
                for (int segment = 1; segment <= WorldRules.Segments(world); segment++)
                {
                    string name = $"World{world+1}_Segment{segment}";
                    EditorSceneManager.OpenScene("Assets/_Scenes/" + name + ".unity");
                    var groups = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                        .Where(g => g.name == "FlyingEncounters").ToArray();
                    if (groups.Length != 1) throw new Exception(name + ": expected exactly one generated group");
                    var enemies = groups[0].GetComponentsInChildren(type).Cast<Component>().ToArray();
                    if (enemies.Length < 2) throw new Exception(name + ": not enough flying encounters");
                    var spawn = UnityEngine.Object.FindFirstObjectByType<PlayerController>().transform.position;
                    var goal = UnityEngine.Object.FindFirstObjectByType<ServerB_Goal>().transform.position;
                    var checkpoints = UnityEngine.Object.FindObjectsByType<Checkpoint_TCP>();
                    foreach (var enemy in enemies)
                    {
                        float x = enemy.transform.position.x;
                        if (x - spawn.x < 35 || goal.x - x < 35) throw new Exception(name + ": encounter too close to spawn/goal");
                        if (checkpoints.Any(c => Mathf.Abs(c.transform.position.x - x) < 24)) throw new Exception(name + ": encounter too close to checkpoint");
                        if (enemy.GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Kinematic || !enemy.GetComponent<Collider2D>().isTrigger)
                            throw new Exception(name + ": wrong flying physics");
                        foreach (var hit in Physics2D.OverlapCircleAll(enemy.transform.position, 0.7f))
                            if (!hit.isTrigger && hit.attachedRigidbody != enemy.GetComponent<Rigidbody2D>()) throw new Exception(name + ": drone intersects terrain");
                    }
                    report.Add("PASS: " + name + " has " + enemies.Length + " spaced flying encounters");
                }
            return string.Join("\n", report);
        }
        finally { EditorSceneManager.OpenScene("Assets/_Scenes/Menu.unity"); }
    }
}
