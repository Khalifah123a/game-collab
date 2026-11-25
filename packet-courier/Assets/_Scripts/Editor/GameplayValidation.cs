#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GameplayValidation
{
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    public static string ValidateAssets()
    {
        var report = new List<string>();
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Player.prefab");
        Check(player && player.GetComponent<PlayerStamina>(), "Player stamina missing");
        Check(player.GetComponent<BoxCollider2D>().enabled, "Player collision disabled");
        var animator = player.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        foreach (string state in new[] { "Idle", "Run", "Jump", "DoubleJump", "Fall", "Dash", "Land", "Hurt", "Death", "Attack" })
        {
            var entry = animator.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name == state);
            Check(entry.state && entry.state.motion, "Missing animation: " + state);
        }
        Check(player.GetComponent<PlayerCombat>(), "Player melee combat missing");
        report.Add("PASS: ten animation motions, player combat, collider and stamina");
        for (int world = 0; world < WorldRules.Count; world++)
        for (int segment = 1; segment <= WorldRules.Segments(world); segment++)
        {
            string name = $"World{world + 1}_Segment{segment}";
            string[] rows = File.ReadAllLines("Assets/_Levels/" + name + ".txt");
            string all = string.Join("\n", rows);
            Check(all.Count(c => c == 'P') == 1 && all.Count(c => c == 'G') == 1, name + " needs one spawn and goal");
            int spawnX = Array.Find(rows, line => line.Contains('P')).IndexOf('P');
            int goalX = Array.Find(rows, line => line.Contains('G')).IndexOf('G');
            Check(goalX - spawnX > 400, name + " is too short");
            Check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith(name + ".unity")), name + " missing from build");
            Check(all.Count(c => c == 'C') >= 2, name + " needs checkpoints");
            report.Add("PASS: " + name + " spawn-goal=" + (goalX-spawnX) + " units");
        }
        return string.Join("\n", report);
    }

    public static string CheckRuntimeHUD()
    {
        var health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
        var hud = UnityEngine.Object.FindAnyObjectByType<GameplayHUD>();
        Check(health && hud, "Player and HUD must be loaded");
        var texts = hud.GetComponentsInChildren<UnityEngine.UI.Text>();
        Check(texts.Any(t => t.text.StartsWith("TTL")) && texts.Any(t => t.text.StartsWith("STAMINA")), "Resource labels missing");
        var corners = new Vector3[4];
        foreach (var text in texts.Where(t => !string.IsNullOrEmpty(t.text)))
        {
            text.rectTransform.GetWorldCorners(corners);
            Check(corners[0].x >= -1 && corners[0].y >= -1 && corners[2].x <= Screen.width+1 && corners[2].y <= Screen.height+1,
                "HUD outside screen: " + text.name);
        }
        return $"PASS: HUD inside {Screen.width}x{Screen.height}, TTL={health.ttlCurrent:0.0}";
    }
}
#endif
