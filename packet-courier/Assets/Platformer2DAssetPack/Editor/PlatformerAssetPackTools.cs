#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platformer2DAssetPack.Editor
{
    public static class PlatformerAssetPackTools
    {
        private const string Root = "Assets/Platformer2DAssetPack";
        private const string CharacterFrames = Root + "/Art/Character/Frames";
        private const string AnimationFolder = Root + "/Animations";
        private const string SceneFolder = Root + "/Scenes";

        [MenuItem("Tools/Platformer Asset Pack/Apply Import Settings")]
        public static void ApplyImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = path.Contains("/Background/") ? TextureWrapMode.Clamp : TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit = path.Contains("/Background/") ? 100f : 64f;
                importer.SaveAndReimport();
                changed++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Platformer Asset Pack",
                $"Import settings diterapkan pada {changed} texture.",
                "OK"
            );
        }

        [MenuItem("Tools/Platformer Asset Pack/Create Character Animation")]
        public static void CreateCharacterAnimation()
        {
            EnsureFolder(AnimationFolder);

            Sprite[] sprites = AssetDatabase.FindAssets("t:Sprite", new[] { CharacterFrames })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                .Where(sprite => sprite != null)
                .ToArray();

            if (sprites.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Platformer Asset Pack",
                    "Sprite karakter belum ditemukan. Jalankan Apply Import Settings terlebih dahulu.",
                    "OK"
                );
                return;
            }

            AnimationClip clip = new AnimationClip
            {
                frameRate = 10f,
                name = "Character_AllFrames"
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            keys[^1] = new ObjectReferenceKeyframe
            {
                time = sprites.Length / clip.frameRate,
                value = sprites[0]
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string path = AnimationFolder + "/Character_AllFrames.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Platformer Asset Pack",
                "Animation clip dibuat di:\n" + path,
                "OK"
            );
        }

        [MenuItem("Tools/Platformer Asset Pack/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PlatformerAssetGallery";

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.10f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[]
            {
                Root + "/Art/Character",
                Root + "/Art/Environment",
                Root + "/Art/Items"
            });

            Sprite[] sprites = spriteGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                .Where(sprite => sprite != null)
                .ToArray();

            const int columns = 10;
            const float spacingX = 2.1f;
            const float spacingY = 2.0f;

            for (int i = 0; i < sprites.Length; i++)
            {
                int column = i % columns;
                int row = i / columns;

                GameObject go = new GameObject(sprites[i].name);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];

                Vector3 size = renderer.bounds.size;
                float largest = Mathf.Max(size.x, size.y);
                if (largest > 1.6f)
                    go.transform.localScale *= 1.6f / largest;

                go.transform.position = new Vector3(
                    (column - (columns - 1) * 0.5f) * spacingX,
                    5.5f - row * spacingY,
                    0f
                );
            }

            string scenePath = SceneFolder + "/PlatformerAssetGallery.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            EditorUtility.DisplayDialog(
                "Platformer Asset Pack",
                "Demo scene dibuat di:\n" + scenePath,
                "OK"
            );
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    public class PlatformerAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Platformer2DAssetPack/", StringComparison.Ordinal))
                return;

            if (assetImporter is not TextureImporter importer)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = assetPath.Contains("/Background/") ? 100f : 64f;
        }
    }
}
#endif