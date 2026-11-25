using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    private PlayerController player;
    private Rigidbody2D body;
    private PlayerCombat combat;
    private float doubleUntil, hurtUntil, landUntil;
    private bool dead, wasGrounded;
    private string current;
    private float stateStartTime;
    private float speed = 1f;

    // Prevents attack state from resetting to frame 0 when attack impulse changes velocity mid-swing
    private bool _wasAttacking;
    private string _lockedAttackState = "Attack";

    // Cutscene pose override for Opening Prologue choreography
    private bool _cutsceneOverrideActive;
    private string _cutsceneState = "Idle";
    private float _cutsceneFacing = 1f;

    private static readonly Dictionary<string, Sprite[]> StateSprites = new Dictionary<string, Sprite[]>();
    private static bool spritesLoaded;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        player = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        combat = GetComponent<PlayerCombat>();
        if (!GetComponent<PlayerBackpackPackage>())
            gameObject.AddComponent<PlayerBackpackPackage>();
        EnsureStateSpritesLoaded();
    }

    private static void EnsureStateSpritesLoaded()
    {
        if (spritesLoaded && StateSprites.ContainsKey("Idle") && StateSprites["Idle"] != null && StateSprites["Idle"].Length > 0 && StateSprites["Idle"][0] != null)
            return;

        spritesLoaded = true;
        StateSprites["Idle"] = LoadFrames("idle1", "idle2");
        StateSprites["Walk"] = LoadFrames("walk1", "walk2", "walk3", "walk4");
        StateSprites["Run"] = LoadFrames("run1", "run2", "run3", "run4");
        StateSprites["Dash"] = LoadFrames("dash1", "dash2", "dash3");
        StateSprites["Jump"] = LoadFrames("jump1", "jump2");
        StateSprites["DoubleJump"] = LoadFrames("doublejump1", "doublejump2");
        StateSprites["Fall"] = LoadFrames("fall1", "fall2");
        StateSprites["Land"] = LoadFrames("land1");
        StateSprites["Attack"] = LoadFrames("attack1", "attack2", "attack3", "attack4");
        StateSprites["AttackRun"] = LoadFrames("attack_run1", "attack_run2", "attack_run3", "attack_run4");
        StateSprites["AttackAir"] = LoadFrames("attack_air1", "attack_air2", "attack_air3", "attack_air4");
        StateSprites["Hurt"] = LoadFrames("hurt1");
        StateSprites["Death"] = LoadFrames("death1", "death2");

        // Fallback if walk frames missing
        if (StateSprites["Walk"].Length == 0) StateSprites["Walk"] = StateSprites["Run"];
    }

    private static Sprite[] LoadFrames(params string[] names)
    {
        var list = new List<Sprite>(names.Length);
        foreach (string n in names)
        {
            var s = Resources.Load<Sprite>("PlayerSprites/" + n);
            if (s != null) list.Add(s);
        }
        return list.ToArray();
    }

    public void NotifyDoubleJump() { doubleUntil = Time.time + 0.25f; }
    public void NotifyHurt() { hurtUntil = Time.time + 0.15f; }
    public void NotifyDeath() { dead = true; }

    public void NotifyAttack()
    {
        stateStartTime = Time.time;
        bool hasMoveInput = InputManager.Instance != null && Mathf.Abs(InputManager.Instance.moveInput.x) > 0.1f;
        _lockedAttackState = (player != null && !player.isGrounded)
            ? "AttackAir"
            : (hasMoveInput ? "AttackRun" : "Attack");
    }

    public void SetCutsceneOverride(string stateName, float facingDir)
    {
        if (!_cutsceneOverrideActive || _cutsceneState != stateName)
        {
            stateStartTime = Time.time;
        }
        _cutsceneOverrideActive = true;
        _cutsceneState = string.IsNullOrEmpty(stateName) ? "Idle" : stateName;
        _cutsceneFacing = facingDir >= 0f ? 1f : -1f;
    }

    public void ClearCutsceneOverride()
    {
        _cutsceneOverrideActive = false;
    }

    public void UpdateState(bool moving, float multiplier, float facing)
    {
        speed = multiplier;
        if (spriteRenderer && !_cutsceneOverrideActive) spriteRenderer.flipX = facing < 0;
    }

    private void LateUpdate()
    {
        if (!player) return;

        if (_cutsceneOverrideActive)
        {
            if (spriteRenderer)
            {
                spriteRenderer.color = Color.white;
                spriteRenderer.flipX = _cutsceneFacing < 0f;
            }
            current = _cutsceneState;
            ApplyRuntimeSpriteFrame(current);
            return;
        }

        var health = player.GetComponent<PlayerHealth>();
        if (spriteRenderer)
        {
            bool flash = health && health.IsInvulnerable && Mathf.FloorToInt(Time.time * 14) % 2 == 0;
            spriteRenderer.color = flash ? new Color(1, 1, 1, 0.25f) : Color.white;
        }
        if (!wasGrounded && player.isGrounded) landUntil = Time.time + 0.08f;
        wasGrounded = player.isGrounded;

        bool hasInputX = InputManager.Instance != null && Mathf.Abs(InputManager.Instance.moveInput.x) > 0.05f;
        bool isMoving = (body != null && Mathf.Abs(body.linearVelocity.x) > 0.25f) || hasInputX;
        bool fastRun = player.isSprinting || speed > 1.25f;
        string moveState = fastRun ? "Run" : "Walk";

        bool isAttacking = combat && combat.IsAttacking;
        if (isAttacking && !_wasAttacking)
        {
            _lockedAttackState = !player.isGrounded ? "AttackAir" : (hasInputX ? "AttackRun" : "Attack");
            stateStartTime = Time.time;
        }
        _wasAttacking = isAttacking;

        string next = dead ? "Death"
            : Time.time < hurtUntil ? "Hurt"
            : player.isDashing ? "Dash"
            : isAttacking ? _lockedAttackState
            : (!player.isGrounded && Time.time < doubleUntil) ? "DoubleJump"
            : !player.isGrounded ? ((body != null && body.linearVelocity.y > 0.1f) ? "Jump" : "Fall")
            : isMoving ? moveState
            : Time.time < landUntil ? "Land"
            : "Idle";

        bool isMovePair = (current == "Walk" && next == "Run") || (current == "Run" && next == "Walk");
        bool isAttackPair = current != null && current.StartsWith("Attack") && next.StartsWith("Attack");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.speed = (next == "Run" || next == "Walk") ? speed : 1;
            if (next != current && !isAttackPair)
            {
                if (!isMovePair) stateStartTime = Time.time;
                int hash = Animator.StringToHash(next);
                if (animator.HasState(0, hash)) animator.Play(hash, 0, 0);
            }
        }
        else if (next != current && !isMovePair && !isAttackPair)
        {
            stateStartTime = Time.time;
        }

        current = next;
        ApplyRuntimeSpriteFrame(current);
    }

    private void ApplyRuntimeSpriteFrame(string stateName)
    {
        if (!spriteRenderer || string.IsNullOrEmpty(stateName)) return;
        EnsureStateSpritesLoaded();
        if (!StateSprites.TryGetValue(stateName, out var frames) || frames == null || frames.Length == 0) return;

        float elapsed = Mathf.Max(0f, Time.unscaledTime - stateStartTime);
        if (Time.timeScale > 0f && !_cutsceneOverrideActive)
        {
            elapsed = Mathf.Max(0f, Time.time - stateStartTime);
        }

        float fps = stateName.StartsWith("Attack") ? 15f
            : stateName == "Dash" ? 16f
            : stateName == "Run" ? 14f * Mathf.Max(0.8f, speed)
            : stateName == "Walk" ? 10.5f * Mathf.Max(0.8f, speed)
            : stateName == "Idle" ? 4.5f
            : 10f;

        bool loop = stateName == "Idle" || stateName == "Walk" || stateName == "Run" || stateName == "Fall" || stateName == "Dash";
        int index = loop
            ? Mathf.FloorToInt(elapsed * fps) % frames.Length
            : Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * fps));

        if (frames[index] != null)
        {
            spriteRenderer.sprite = frames[index];
            spriteRenderer.transform.localScale = Vector3.one;
            spriteRenderer.transform.localRotation = Quaternion.identity;
        }
    }
}
