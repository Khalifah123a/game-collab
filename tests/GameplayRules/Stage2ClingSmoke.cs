using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Stage 2 wall-cling smoke test.
/// Asserts the approved slide-and-fall mechanic:
///   1. clinging requires the player to PUSH toward a wall (contact alone is not enough)
///   2. clinging slides DOWN at a conservative constant speed (no free-fall, no climb)
///   3. the cling HUD is visible ONLY while clinging
///   4. running out of cling stamina releases the player into a normal-gravity fall
///   5. an exhausted player cannot re-cling until it touches the ground
///   6. the WallClingTime upgrade slows the slide and softens the drain
/// </summary>
public static class Stage2ClingSmoke
{
    private const float HomeX = 400f;
    private const float HomeY = 10f;
    private const float WallCenterX = HomeX + 1.1f;
    private const float FloorCenterY = 0f;
    private const float FloorTop = 1f;

    public static async Task<string> Main()
    {
        Application.runInBackground = true;

        var gm = GameManager.Ensure();
        gm.hasCheckpoint = false;
        gm.currentWorldIndex = 0;
        gm.LoadSegment(0);
        await Task.Delay(400);

        var health = PlayerHealth.Instance;
        health.drainRate = 0;

        var ctrl = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (ctrl == null) throw new Exception("PlayerController not found");
        var stamina = ctrl.GetComponent<PlayerStamina>();
        var rb = ctrl.GetComponent<Rigidbody2D>();
        var col = ctrl.GetComponent<BoxCollider2D>();
        if (stamina == null || rb == null || col == null) throw new Exception("Player components missing");

        var input = InputManager.EnsureExists();
        input.enabled = false; // stop the reader from overwriting injected input
        input.useTouchInput = false;

        // Isolated fixtures, far outside any level geometry.
        var wall = NewBox("ClingSmokeWall", new Vector3(WallCenterX, HomeY, 0f), new Vector2(1f, 40f));
        var floor = NewBox("ClingSmokeFloor", new Vector3(HomeX, FloorCenterY, 0f), new Vector2(30f, FloorTop * 2f));

        gm.upgradeLevels[(int)GameManager.UpgradeType.WallClingTime] = 0;
        var baseSlide = ctrl.ClingSlideSpeed;
        if (baseSlide < 1f || baseSlide > 3.5f)
            throw new Exception($"Baseline cling slide speed {baseSlide} is not conservative (expected 1-3.5 u/s)");

        // ---- 1. PUSH REQUIRED -------------------------------------------------
        Prep(ctrl, rb, new Vector3(HomeX, HomeY, 0f));
        input.moveInput = Vector2.zero; // touching the wall, but not pushing into it
        await Frames(4);
        if (ctrl.isClinging) throw new Exception("Player clung without pushing toward the wall");

        Prep(ctrl, rb, new Vector3(HomeX, HomeY, 0f));
        input.moveInput = Vector2.left; // pushing away from the wall
        await Frames(4);
        if (ctrl.isClinging) throw new Exception("Player clung while pushing away from the wall");

        // ---- 2. SLIDE DOWN AT A CONSTANT, CONSERVATIVE SPEED -----------------
        Prep(ctrl, rb, new Vector3(HomeX, HomeY, 0f));
        stamina.state.Reset();
        input.moveInput = Vector2.right; // pushing into the wall
        await Frames(4);
        if (!ctrl.isClinging) throw new Exception("Player did not cling while pushing into the wall");

        var slide = await SampleSlide(rb, ctrl, 8);
        if (slide.min > -1.5f || slide.max < -3.5f)
            throw new Exception($"Cling slide speed [{slide.min:F2},{slide.max:F2}] outside conservative 1.5-3.5 u/s band");
        if (slide.max - slide.min > 0.75f)
            throw new Exception($"Cling slide speed is not constant (varied by {slide.max - slide.min:F2} u/s)");
        if (Mathf.Abs(slide.avg + baseSlide) > 0.6f)
            throw new Exception($"Measured cling slide {slide.avg:F2} does not match ClingSlideSpeed {baseSlide:F2}");

        // No upward climbing while attached.
        input.moveInput = new Vector2(1f, 1f);
        await Frames(4);
        if (rb.linearVelocity.y > -0.1f)
            throw new Exception("Player gained upward speed while clinging (climbing is not allowed)");
        input.moveInput = Vector2.right;
        await Frames(2);

        // ---- 3. HUD VISIBLE ONLY WHILE CLINGING ------------------------------
        var hud = ctrl.GetComponent<ClingStaminaHUD>();
        if (hud == null) throw new Exception("ClingStaminaHUD was not attached to the player");
        if (!hud.IsVisible) throw new Exception("Cling HUD hidden while the player is clinging");

        input.moveInput = Vector2.left;
        await Frames(4);
        if (ctrl.isClinging) throw new Exception("Player did not release when push stopped");
        if (hud.IsVisible) throw new Exception("Cling HUD still visible after the player released");

        // ---- 4. DEPLETION RELEASES INTO A NORMAL-GRAVITY FALL -----------------
        Land(ctrl, rb, col);
        stamina.state.Reset();
        Prep(ctrl, rb, new Vector3(HomeX, HomeY, 0f));
        input.moveInput = Vector2.right;
        await Frames(4);
        if (!ctrl.isClinging) throw new Exception("Player failed to re-cling after landing");

        var carried = stamina.Current;
        stamina.state.Drain(carried - 6f); // leave a short, measurable reserve
        var drained = await WaitForRelease(ctrl, 2f);
        if (!drained) throw new Exception("Cling stamina was never depleted");
        if (!ctrl.clingExhausted) throw new Exception("Cling depletion did not set the exhaustion lock");
        if (ctrl.isClinging) throw new Exception("Player is still clinging after stamina hit zero");
        if (hud.IsVisible) throw new Exception("Cling HUD still visible after stamina hit zero");
        await Frames(4);
        if (rb.linearVelocity.y > -1f)
            throw new Exception($"Player did not fall under gravity after depletion (vy={rb.linearVelocity.y:F2})");

        // ---- 5. NO RE-CLING UNTIL GROUNDED -----------------------------------
        PrepKeepExhausted(ctrl, rb, new Vector3(HomeX, HomeY, 0f));
        input.moveInput = Vector2.right;
        await Frames(10);
        if (ctrl.isClinging)
            throw new Exception("Exhausted player re-clinged before touching the ground");
        if (!ctrl.clingExhausted) throw new Exception("Exhaustion lock cleared while still airborne");

        Land(ctrl, rb, col);
        await Frames(4);
        if (!ctrl.isGrounded) throw new Exception("Fixture failed to ground the player");
        if (ctrl.clingExhausted) throw new Exception("Landing did not clear the cling exhaustion lock");

        // ---- 6. UPGRADE SLOWS THE SLIDE AND SOFTENS THE DRAIN -----------------
        var baseDrainScale = ctrl.GetClingDrainScale();
        gm.upgradeLevels[(int)GameManager.UpgradeType.WallClingTime] = 1;
        var upSlide = ctrl.ClingSlideSpeed;
        var upDrainScale = ctrl.GetClingDrainScale();
        if (upSlide >= baseSlide) throw new Exception("WallClingTime upgrade did not slow the cling slide");
        if (upDrainScale >= baseDrainScale) throw new Exception("WallClingTime upgrade did not soften the cling drain");
        if (upSlide < 1f) throw new Exception("Upgraded cling slide is no longer conservative");
        if (upDrainScale <= 0f) throw new Exception("Upgraded cling drain is non-positive (would mean infinite cling)");

        Prep(ctrl, rb, new Vector3(HomeX, HomeY, 0f));
        stamina.state.Reset();
        input.moveInput = Vector2.right;
        await Frames(4);
        if (!ctrl.isClinging) throw new Exception("Player failed to cling with the upgrade installed");
        var upgraded = await SampleSlide(rb, ctrl, 6);
        if (Mathf.Abs(upgraded.avg + upSlide) > 0.6f)
            throw new Exception($"Upgraded cling slide {upgraded.avg:F2} does not match ClingSlideSpeed {upSlide:F2}");
        if (upgraded.avg <= slide.avg)
            throw new Exception("Upgraded cling slide is not slower than the baseline slide");

        gm.upgradeLevels[(int)GameManager.UpgradeType.WallClingTime] = 0;
        input.enabled = true;
        UnityEngine.Object.Destroy(wall.gameObject);
        UnityEngine.Object.Destroy(floor.gameObject);

        return "PASS: push-required cling, constant conservative slide, cling-only HUD visibility, " +
               "depletion drop, exhaustion re-cling lock, WallClingTime upgrade";
    }

    private static BoxCollider2D NewBox(string name, Vector3 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        return box;
    }

    private static void Prep(PlayerController ctrl, Rigidbody2D rb, Vector3 pos)
    {
        ctrl.isDashing = false;
        ctrl.clingExhausted = false;
        ctrl.wallUnstickTimer = 0f;
        ctrl.wallClingTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        ctrl.transform.position = pos;
    }

    private static void PrepKeepExhausted(PlayerController ctrl, Rigidbody2D rb, Vector3 pos)
    {
        ctrl.isDashing = false;
        ctrl.wallUnstickTimer = 0f;
        ctrl.wallClingTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        ctrl.transform.position = pos;
    }

    private static void Land(PlayerController ctrl, Rigidbody2D rb, BoxCollider2D col)
    {
        rb.linearVelocity = Vector2.zero;
        ctrl.transform.position = new Vector3(HomeX, FloorTop + col.bounds.extents.y + 0.05f, 0f);
    }

    private static async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await Task.Delay(16);
    }

    private static async Task<(float min, float max, float avg)> SampleSlide(Rigidbody2D rb, PlayerController ctrl, int samples)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        float sum = 0f;
        for (int i = 0; i < samples; i++)
        {
            await Task.Delay(24);
            float vy = rb.linearVelocity.y;
            if (vy < min) min = vy;
            if (vy > max) max = vy;
            sum += vy;
        }
        return (min, max, sum / samples);
    }

    private static async Task<bool> WaitForRelease(PlayerController ctrl, float timeoutSeconds)
    {
        int ticks = (int)(timeoutSeconds / 0.02f);
        for (int i = 0; i < ticks; i++)
        {
            await Task.Delay(20);
            if (!ctrl.isClinging) return true;
        }
        return !ctrl.isClinging;
    }
}