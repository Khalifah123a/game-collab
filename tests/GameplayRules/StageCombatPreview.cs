using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class StageCombatPreview
{
    public static async Task<string> Main()
    {
        Application.runInBackground = true;
        Time.timeScale = 1;
        var gm = GameManager.Ensure(); gm.hasCheckpoint = false; gm.currentWorldIndex = 0; gm.LoadSegment(0);
        await Task.Delay(250);
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        var enemy = UnityEngine.Object.FindObjectsByType<Enemy_Patrol>(FindObjectsSortMode.None).OrderBy(e => e.transform.position.x).First();
        enemy.enabled = false; enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.transform.position = enemy.transform.position + Vector3.left * 1.8f;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        InputManager.Instance.enabled = false; InputManager.Instance.moveInput = Vector2.zero;
        PlayerHealth.Instance.drainRate = 0;
        player.FacingDir = 1;
        await Task.Delay(600);
        player.GetComponent<PlayerCombat>().TryAttack();
        await Task.Delay(120);
        Time.timeScale = 0;
        return "Combat preview paused during active slash; reload scene after capture.";
    }
}
