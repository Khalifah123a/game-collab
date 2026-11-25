#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class EditorGenerator
{
    [MenuItem("Tools/Packet Courier/Apply Floating Platform Campaign")]
    public static void ApplyFloatingPlatformCampaign()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scene before rebuilding.");
        for (int world = 0; world < WorldRules.Count; world++)
            for (int segment = 0; segment < WorldRules.Segments(world); segment++)
                File.WriteAllLines($"{LevelsRoot}/World{world + 1}_Segment{segment + 1}.txt", VerticalLayout(world, segment));
        BuildLevelScenes();
        RegisterBuildSettings();
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenesRoot + "/Menu.unity");
    }

    [MenuItem("Tools/Packet Courier/Apply Combat And Vertical Campaign")]
    public static void ApplyCombatAndVerticalCampaign()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scene before rebuilding.");
        for (int world = 0; world < WorldRules.Count; world++)
            for (int segment = 0; segment < WorldRules.Segments(world); segment++)
                File.WriteAllLines($"{LevelsRoot}/World{world + 1}_Segment{segment + 1}.txt", VerticalLayout(world, segment));
        string enemyPath = PrefabRoot + "/Enemy_Patrol.prefab";
        var enemy = PrefabUtility.LoadPrefabContents(enemyPath);
        try
        {
            var patrol = enemy.GetComponent<Enemy_Patrol>();
            patrol.detectionRadius = 10; patrol.chaseRadius = 16; patrol.chaseSpeed = 4.5f;
            patrol.hitPoints = 2;
            enemy.GetComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;
            PrefabUtility.SaveAsPrefabAsset(enemy, enemyPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(enemy); }
        ApplyGameplayRework();
    }

    private static string[] VerticalLayout(int world, int segment)
    {
        string levelPath = $"{LevelsRoot}/World{world + 1}_Segment{segment + 1}.txt";
        if (world == 0 && File.Exists(levelPath))
        {
            return File.ReadAllLines(levelPath);
        }

        int width = 520 + world * 64 + segment * 24;
        const int bottom = 18;
        var grid = new char[bottom + 1][];
        for (int y = 0; y < grid.Length; y++) { grid[y] = new char[width]; Array.Fill(grid[y], ' '); }
        Action<int, int, char> put = (x, y, tile) => { if (x >= 0 && x < width && y >= 0 && y <= bottom) grid[y][x] = tile; };
        Action<int, int, int> floor = (start, end, row) =>
        {
            for (int x = start; x < end && x < width; x++)
                for (int y = row; y <= bottom; y++) put(x, y, '#');
        };
        floor(0, 16, bottom);
        put(3, bottom - 1, 'P');
        put(11, bottom - 2, 'B');
        for (int x = 6; x <= 8; x++) put(x, bottom - 3, '-');
        for (int x = 11; x <= 13; x++) put(x, bottom - 5, '-');
        put(7, bottom - 4, 'B');
        put(12, bottom - 6, 'N');
        int[][] profiles = {
            new[] { 2, 5, 8, 11, 8, 5, 2, 0 },
            new[] { 3, 6, 9, 12, 9, 6, 3, 0 },
            new[] { 2, 5, 8, 5, 8, 11, 8, 5, 2, 0 }
        };
        int xStart = 16, section = 0, previousHeight = 0;
        while (xStart < width - 22)
        {
            int[] profile = profiles[(section / 10 + segment + world) % profiles.Length];
            int height = profile[section % profile.Length];
            // Limit rises at motif boundaries as well as within each motif.
            height = Mathf.Clamp(height, previousHeight - 3, previousHeight + 3);
            int row = bottom - height;
            int length = 11 + (section * 3 + segment) % 5;
            int end = Math.Min(xStart + length, width - 22);
            bool checkpoint = section > 0 && section % 7 == 0;
            bool floating = end - xStart >= 10 && (checkpoint ? section % 14 == 7 : section % 9 >= 1 && section % 9 <= 4);
            bool gap = !floating && !checkpoint && section > 1 && section % 4 == 2 && height <= previousHeight;
            int gapWidth = gap ? 4 + (world > 1 ? 2 : 0) : 0;
            if (floating)
            {
                // Early practice has a rescue floor; later islands are part of the required route.
                if (world == 0 && section < 5) floor(xStart, end, bottom);
                if (checkpoint)
                {
                    for (int x = xStart; x < end; x++) put(x, row, '-');
                    put(xStart + 4, row - 1, 'C');
                    put(xStart + 7, row - 1, 'N');
                }
                else
                {
                    for (int x = xStart; x < xStart + 4; x++) put(x, row, '-');
                    for (int x = end - 4; x < end; x++) put(x, row, '-');
                    int middle = (xStart + end) / 2;
                    if (world >= 2 && section % 9 == 3)
                        put(middle, row - 2, section % 2 == 0 ? 'H' : 'R');
                    else if (world >= 1 && section % 9 == 2)
                        put(middle, row - 2, '=');
                    else
                        for (int x = middle - 1; x <= middle + 1; x++) put(x, row - 2, '-');
                    put(xStart + 1, row - 1, 'B');
                    put(end - 2, row - 1, 'B');
                    put(middle, row - 3, section % 9 == 4 ? 'N' : 'B');
                }
            }
            else floor(xStart + gapWidth, end, row);
            int safeStart = xStart + gapWidth;
            if (gap)
            {
                char bridge = world >= 2 ? (section % 8 == 2 ? 'R' : 'H') : '=';
                put(xStart + gapWidth / 2, row, bridge);
                put(xStart + gapWidth / 2, row - 2, 'B');
            }
            if (checkpoint && !floating) { put(safeStart + 3, row - 1, 'C'); put(safeStart + 6, row - 1, 'N'); }
            else if (!floating && !gap && section % 3 == 1)
                put(safeStart + 6, row - 1, 'E');
            else if (!floating && !checkpoint && section > 3 && end - safeStart >= 9)
            {
                put(end - 4, row - 1, 'S');
                if (world >= 2) put(end - 3, row - 1, 'S');
            }
            bool hasGate = !floating && !checkpoint && !gap && world >= 1 && section % 9 == 5;
            if (hasGate)
                put(end - 3, row - 1, (section / 9) % 2 == 0 ? 'b' : 'c');
            if (!floating && !checkpoint && !gap && world >= 1 && section % 9 == 8)
                put(safeStart + 4, row - 2, 'L');
            if (!floating && !checkpoint && !hasGate && section % 5 == 3)
            {
                for (int x = safeStart + 2; x <= safeStart + 4; x++) put(x, row - 3, '-');
                put(safeStart + 3, row - 4, 'N');
            }
            if (!floating) put(safeStart + 2, row - 2, 'B');
            if (world == 3 && section % 6 == 2) put(safeStart + 3, row - 3, 'D');
            previousHeight = height;
            xStart = end;
            section++;
        }
        floor(xStart, width, bottom - previousHeight);
        put(width - 18, bottom - previousHeight - 1, 'C');
        put(width - 5, bottom - previousHeight - 1, 'G');
        // The final world offers an elevated zero-gravity bonus route above solid terraces.
        if (world == 3)
        {
            put(width - 58, 2, 'Z');
            for (int x = width - 56; x < width - 25; x += 5) put(x, 1, 'D');
        }
        var rows = new string[grid.Length];
        for (int y = 0; y < rows.Length; y++) rows[y] = new string(grid[y]);
        return rows;
    }
}
#endif
