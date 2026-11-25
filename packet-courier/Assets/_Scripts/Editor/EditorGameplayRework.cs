#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class EditorGenerator
{
    [MenuItem("Tools/Packet Courier/Apply Gameplay Rework")]
    public static void ApplyGameplayRework()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scene before rebuilding.");
        EnsureWalkSpriteImportSettings();
        CreatePlayerAnimatorController();
        UpgradePlayerPrefab();
        BuildLevelScenes();
        BuildMetaScenes();
        RegisterBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenesRoot + "/Menu.unity");
    }

    private static void UpgradePlayerPrefab()
    {
        string path = PrefabRoot + "/Player.prefab";
        var go = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual = go.transform.Find("Visual");
            if (!visual) { visual = new GameObject("Visual").transform; visual.SetParent(go.transform, false); }
            var sr = visual.GetComponent<SpriteRenderer>();
            if (!sr) sr = visual.gameObject.AddComponent<SpriteRenderer>();
            var old = go.GetComponent<SpriteRenderer>();
            sr.sprite = old && old.sprite ? old.sprite : LoadWalkSprite(0);
            sr.sortingOrder = 10;
            if (old) UnityEngine.Object.DestroyImmediate(old);
            var animation = go.GetComponent<Animator>();
            if (!animation) animation = go.AddComponent<Animator>();
            animation.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(PrefabRoot + "/PlayerAnimator.controller");
            var pa = go.GetComponent<PlayerAnimator>();
            pa.animator = animation; pa.spriteRenderer = sr;
            if (!go.GetComponent<PlayerStamina>()) go.AddComponent<PlayerStamina>();
            if (!go.GetComponent<PlayerCombat>()) go.AddComponent<PlayerCombat>();
            go.GetComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;
            var movement = go.GetComponent<PlayerController>();
            movement.acceleration = 45; movement.groundDecel = 55; movement.airDecel = 32;
            movement.gravity = 70; movement.jumpForce = 20; movement.maxFallSpeed = 30;
            movement.walljumpForceX = 10; movement.walljumpForceY = 20;
            movement.wallCheckOffset = 0.32f; movement.wallCheckSize = new Vector2(0.12f, 0.65f);
            movement.groundMask = 1; movement.dashBlockMask = 1;
            movement.dashDuration = 0.15f; movement.coyoteTime = 0.1f;
            movement.sprintSpeedMultiplier = 1.5f;
            var mode = go.GetComponent<PlayerMode>();
            mode.plaintextDashDist = 7; mode.encryptedDashDist = 5;
            mode.plaintextDashCooldown = 0.4f; mode.encryptedDashCooldown = 0.55f;
            mode.plaintextHitbox = new Vector2(0.5f, 1); mode.encryptedHitbox = new Vector2(0.5f, 1);
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }

    private static void CreatePlayerAnimatorController()
    {
        string path = PrefabRoot + "/PlayerAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        string[] names = { "Idle", "Walk", "Run", "Jump", "DoubleJump", "Fall", "Dash", "Land", "Hurt", "Death", "Attack", "AttackRun", "AttackAir" };
        foreach (string name in names)
        {
            var state = FindOrAddState(machine, name);
            foreach (var transition in state.transitions) state.RemoveTransition(transition);
            var clip = state.motion as AnimationClip;
            // Keep artist-authored clips; replace only our generated placeholders or missing motions.
            if (clip && !clip.name.StartsWith("Preview_")) continue;
            if (!clip) { clip = new AnimationClip { name = "Preview_" + name }; AssetDatabase.AddObjectToAsset(clip, controller); }
            clip.ClearCurves(); clip.frameRate = 12;
            float duration = name == "Dash" ? 0.2f : name == "Attack" ? 0.28f : name == "Death" ? 0.45f : 0.4f;
            var frameSprites = LoadStateSprites(name);
            var sprites = new ObjectReferenceKeyframe[frameSprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                sprites[i] = new ObjectReferenceKeyframe { time = sprites.Length > 1 ? i * duration / (sprites.Length - 1) : 0f, value = frameSprites[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "Visual", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, sprites);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = name == "Idle" || name == "Run" || name == "Fall";
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            state.motion = clip; EditorUtility.SetDirty(clip);
        }
        machine.defaultState = FindOrAddState(machine, "Idle");
        EditorUtility.SetDirty(controller);
    }

    private static Sprite[] LoadStateSprites(string stateName)
    {
        string[] files = stateName switch
        {
            "Idle" => new[] { "idle1", "idle2", "idle1" },
            "Walk" => new[] { "walk1", "walk2", "walk3", "walk4" },
            "Run" => new[] { "run1", "run2", "run3", "run4" },
            "Dash" => new[] { "dash1", "dash2", "dash3", "dash2" },
            "Jump" => new[] { "jump1", "jump2" },
            "DoubleJump" => new[] { "doublejump1", "doublejump2", "doublejump1" },
            "Fall" => new[] { "fall1", "fall2", "fall1" },
            "Land" => new[] { "land1", "idle1" },
            "Attack" => new[] { "attack1", "attack2", "attack3", "attack4" },
            "AttackRun" => new[] { "attack_run1", "attack_run2", "attack_run3", "attack_run4" },
            "AttackAir" => new[] { "attack_air1", "attack_air2", "attack_air3", "attack_air4" },
            "Hurt" => new[] { "hurt1", "hurt1" },
            "Death" => new[] { "death1", "death2", "death2" },
            _ => new[] { "walk" }
        };
        var result = new Sprite[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            result[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{WalkSpriteDir}/{files[i]}.png") ?? LoadWalkSprite(0);
        }
        return result;
    }
}
#endif
