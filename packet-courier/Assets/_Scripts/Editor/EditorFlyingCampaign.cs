#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class EditorGenerator
{
    [MenuItem("Tools/Packet Courier/Install Flying Enemies")]
    public static void InstallFlyingEnemies()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scene before installing flying enemies.");

        EnsureFlyingEnemyPrefab();
        for (int world = 0; world < WorldRules.Count; world++)
        {
            for (int segment = 0; segment < WorldRules.Segments(world); segment++)
            {
                string path = $"{ScenesRoot}/World{world + 1}_Segment{segment + 1}.unity";
                EditorSceneManager.OpenScene(path);
                InstallFlyingEncounters(world, segment);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
            }
        }
        EditorSceneManager.OpenScene(ScenesRoot + "/Menu.unity");
    }

    private static void EnsureFlyingEnemyPrefab()
    {
        string path = PrefabRoot + "/Enemy_Flying.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            CreateFlyingEnemyPrefab();

        var editable = PrefabUtility.LoadPrefabContents(path);
        var body = editable.GetComponent<Rigidbody2D>();
        var collider = editable.GetComponent<Collider2D>();
        if (body == null || collider == null || editable.GetComponent<Enemy_Flying>() == null)
        {
            PrefabUtility.UnloadPrefabContents(editable);
            throw new InvalidOperationException("Enemy_Flying prefab is incomplete.");
        }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        collider.isTrigger = true;
        PrefabUtility.SaveAsPrefabAsset(editable, path);
        PrefabUtility.UnloadPrefabContents(editable);
        AssetDatabase.SaveAssets();
    }

    private static void InstallFlyingEncounters(int worldIndex, int segmentIndex)
    {
        var existing = GameObject.Find("FlyingEncounters");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var goal = UnityEngine.Object.FindAnyObjectByType<ServerB_Goal>();
        if (player == null || goal == null) throw new InvalidOperationException("Level is missing player or goal.");
        var root = new GameObject("FlyingEncounters");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Enemy_Flying.prefab");
        int count = worldIndex == 0 ? 2 : worldIndex == 3 ? 4 : 3;
        float left = player.transform.position.x + 42f;
        float right = goal.transform.position.x - 42f;
        for (int i = 0; i < count; i++)
        {
            float targetX = Mathf.Lerp(left, right, (i + 1f) / (count + 1f));
            Vector2 position = FindFlyingPosition(targetX, worldIndex, segmentIndex, i);
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            enemy.name = $"Enemy_Flying_{i + 1}";
            enemy.transform.position = position;
        }
    }

    private static Vector2 FindFlyingPosition(float targetX, int worldIndex, int segmentIndex, int encounterIndex)
    {
        float x = targetX;
        float bestTop = -19f;
        foreach (var collider in UnityEngine.Object.FindObjectsByType<BoxCollider2D>())
        {
            if (collider.isTrigger || collider.GetComponentInParent<PlayerController>() != null) continue;
            var bounds = collider.bounds;
            if (x >= bounds.min.x && x <= bounds.max.x) bestTop = Mathf.Max(bestTop, bounds.max.y);
        }
        float height = 3.1f + (worldIndex * 0.25f) + ((segmentIndex + encounterIndex) % 2) * 0.65f;
        return new Vector2(x, bestTop + height);
    }

    [MenuItem("Tools/Packet Courier/Validate Flying Enemies")]
    public static void ValidateFlyingEnemies()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Enemy_Flying.prefab");
        if (prefab == null || prefab.GetComponent<Enemy_Flying>() == null)
            throw new InvalidOperationException("Enemy_Flying prefab is missing its behavior.");

        var body = prefab.GetComponent<Rigidbody2D>();
        var collider = prefab.GetComponent<Collider2D>();
        if (body == null || body.bodyType != RigidbodyType2D.Kinematic || body.gravityScale != 0 || collider == null || !collider.isTrigger)
            throw new InvalidOperationException("Enemy_Flying prefab must use a kinematic trigger body with no gravity.");

        var report = new List<string>();
        for (int world = 0; world < WorldRules.Count; world++)
        {
            int required = world == 0 ? 2 : world == 3 ? 4 : 3;
            for (int segment = 0; segment < WorldRules.Segments(world); segment++)
            {
                string path = $"{ScenesRoot}/World{world + 1}_Segment{segment + 1}.unity";
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var root = GameObject.Find("FlyingEncounters");
                var enemies = root ? root.GetComponentsInChildren<Enemy_Flying>(true) : System.Array.Empty<Enemy_Flying>();
                if (enemies.Length != required)
                    throw new InvalidOperationException($"{scene.name} needs {required} flying enemies, found {enemies.Length}.");

                var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                var goal = UnityEngine.Object.FindAnyObjectByType<ServerB_Goal>();
                if (player == null || goal == null)
                    throw new InvalidOperationException($"{scene.name} is missing its player or goal.");
                if (enemies.Any(enemy => enemy.transform.position.x <= player.transform.position.x + 25f ||
                                         enemy.transform.position.x >= goal.transform.position.x - 25f))
                    throw new InvalidOperationException($"{scene.name} places a flying enemy inside a safe edge.");
                report.Add($"PASS: {scene.name} has {enemies.Length} flying enemies");
            }
        }
        EditorSceneManager.OpenScene(ScenesRoot + "/Menu.unity", OpenSceneMode.Single);
        Debug.Log(string.Join("\n", report));
    }
}
#endif
