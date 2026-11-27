using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for SpriteAnimationProfile assets + Unity Menu Tools to generate
/// ready-to-edit Boss & Enemy profile assets in Assets/Resources/CustomAnimations/.
/// </summary>
[CustomEditor(typeof(SpriteAnimationProfile))]
public class SpriteAnimationProfileEditor : Editor
{
    private SpriteAnimationProfile _profile;
    private SerializedProperty _defaultSpriteProp;
    private SerializedProperty _invertFacingProp;
    private SerializedProperty _visualScaleProp;
    private SerializedProperty _visualOffsetProp;
    private SerializedProperty _playbackSpeedProp;
    private SerializedProperty _applyStateTintProp;
    private SerializedProperty _statesProp;

    private int _previewStateIdx = 0;
    private int _previewFrameIdx = 0;
    private bool _isPlayingPreview = false;
    private double _lastTime;

    private static readonly Color HeaderBg = new Color(0.08f, 0.11f, 0.16f, 1f);
    private static readonly Color AccentMint = new Color(0.21f, 0.96f, 0.61f, 1f);

    private void OnEnable()
    {
        _profile = (SpriteAnimationProfile)target;
        _defaultSpriteProp = serializedObject.FindProperty("defaultSprite");
        _invertFacingProp = serializedObject.FindProperty("invertFacing");
        _visualScaleProp = serializedObject.FindProperty("visualScaleMultiplier");
        _visualOffsetProp = serializedObject.FindProperty("visualOffset");
        _playbackSpeedProp = serializedObject.FindProperty("playbackSpeed");
        _applyStateTintProp = serializedObject.FindProperty("applyStateTint");
        _statesProp = serializedObject.FindProperty("states");

        _lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!_isPlayingPreview || _profile == null || _profile.states == null || _profile.states.Count == 0)
            return;

        double now = EditorApplication.timeSinceStartup;
        _lastTime = now;

        int idx = Mathf.Clamp(_previewStateIdx, 0, _profile.states.Count - 1);
        var st = _profile.states[idx];
        if (st != null && st.FrameCount > 0)
        {
            int next = (int)((now * Mathf.Max(1f, st.framesPerSecond * _profile.playbackSpeed)) % st.FrameCount);
            if (next != _previewFrameIdx)
            {
                _previewFrameIdx = next;
                Repaint();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Rect headerRect = EditorGUILayout.GetControlRect(false, 42f);
        EditorGUI.DrawRect(headerRect, HeaderBg);
        EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 2f, headerRect.width, 2f), AccentMint);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = AccentMint }
        };
        GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.75f, 0.85f, 0.92f, 1f) }
        };

        GUI.Label(new Rect(headerRect.x + 12f, headerRect.y + 4f, headerRect.width - 24f, 18f), $"PROFILE SPRITE & ANIMASI: {_profile.name}", titleStyle);
        string status = _profile.HasAnyCustomArt()
            ? "SIAP DIPAKAI (otomatis tampil di game)"
            : "KOSONG (drag sprite ke bawah untuk mengganti visual game)";
        GUI.Label(new Rect(headerRect.x + 12f, headerRect.y + 22f, headerRect.width - 24f, 14f), status, subStyle);

        EditorGUILayout.Space(6f);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(_defaultSpriteProp, new GUIContent("Sprite Diam (Default)", "Cukup drag 1 sprite ke sini jika belum membuat animasi multi-frame."));
        EditorGUILayout.PropertyField(_invertFacingProp, new GUIContent("Balik Arah (Invert Facing)"));
        EditorGUILayout.PropertyField(_visualScaleProp, new GUIContent("Skala Gambar"));
        EditorGUILayout.PropertyField(_visualOffsetProp, new GUIContent("Offset Posisi (X, Y)"));
        EditorGUILayout.PropertyField(_playbackSpeedProp, new GUIContent("Kecepatan Animasi"));
        EditorGUILayout.PropertyField(_applyStateTintProp, new GUIContent("Pakai Warna State Tint"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4f);

        // Quick preset buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Preset Musuh", GUILayout.Height(25f)))
        {
            Undo.RecordObject(_profile, "Populate Enemy Presets");
            _profile.PopulateEnemyPresets();
            EditorUtility.SetDirty(_profile);
            serializedObject.Update();
        }
        if (GUILayout.Button("+ Preset Boss 3-Fase", GUILayout.Height(25f)))
        {
            Undo.RecordObject(_profile, "Populate Boss Presets");
            _profile.PopulateBossPresets();
            EditorUtility.SetDirty(_profile);
            serializedObject.Update();
        }
        if (GUILayout.Button("+ State", GUILayout.Height(25f), GUILayout.Width(72f)))
        {
            Undo.RecordObject(_profile, "Add State");
            _profile.EnsurePresetState("State_" + (_profile.states.Count + 1), 10f, true);
            EditorUtility.SetDirty(_profile);
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);

        // Live preview box right inside Inspector
        DrawAssetPreview();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("STATE ANIMASI (Drag Sprite / Sprite Sheet / Folder ke Kotak Drop)", EditorStyles.boldLabel);

        for (int i = 0; i < _statesProp.arraySize; i++)
        {
            SerializedProperty stateProp = _statesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = stateProp.FindPropertyRelative("stateName");
            SerializedProperty framesProp = stateProp.FindPropertyRelative("frames");
            SerializedProperty fpsProp = stateProp.FindPropertyRelative("framesPerSecond");
            SerializedProperty loopProp = stateProp.FindPropertyRelative("loop");
            SerializedProperty localOffsetProp = stateProp.FindPropertyRelative("localOffset");

            int count = framesProp.arraySize;
            string hdr = count > 0
                ? $"{nameProp.stringValue}  [{count} frame • {fpsProp.floatValue:F0} FPS{(loopProp.boolValue ? " • Loop" : " • 1x")}]"
                : $"{nameProp.stringValue}  [Kosong]";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            stateProp.isExpanded = EditorGUILayout.Foldout(stateProp.isExpanded, hdr, true, EditorStyles.foldoutHeader);

            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f, 1f);
            if (GUILayout.Button("X", GUILayout.Width(22f), GUILayout.Height(18f)))
            {
                _statesProp.DeleteArrayElementAtIndex(i);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (stateProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nameProp, new GUIContent("Nama State"));
                EditorGUILayout.PropertyField(fpsProp, new GUIContent("FPS"));
                EditorGUILayout.PropertyField(loopProp, new GUIContent("Loop"));
                EditorGUILayout.PropertyField(localOffsetProp, new GUIContent("Offset State"));
                ModularSpriteAnimatorEditor.DrawSpriteDropArea(framesProp);
                EditorGUILayout.PropertyField(framesProp, new GUIContent("Frame Sprite"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAssetPreview()
    {
        if (_profile == null) return;
        Sprite previewSpr = null;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("LIVE PREVIEW", EditorStyles.boldLabel);

        if (_profile.states != null && _profile.states.Count > 0)
        {
            string[] names = new string[_profile.states.Count];
            for (int i = 0; i < _profile.states.Count; i++)
                names[i] = _profile.states[i] != null ? _profile.states[i].stateName : $"State {i}";

            _previewStateIdx = Mathf.Clamp(_previewStateIdx, 0, names.Length - 1);
            EditorGUILayout.BeginHorizontal();
            _previewStateIdx = EditorGUILayout.Popup("Preview", _previewStateIdx, names);
            if (GUILayout.Button(_isPlayingPreview ? "Stop" : "Play", GUILayout.Width(68f)))
            {
                _isPlayingPreview = !_isPlayingPreview;
                _lastTime = EditorApplication.timeSinceStartup;
            }
            EditorGUILayout.EndHorizontal();

            var st = _profile.states[_previewStateIdx];
            if (st != null && st.FrameCount > 0)
            {
                _previewFrameIdx = Mathf.Clamp(_previewFrameIdx, 0, st.FrameCount - 1);
                previewSpr = st.frames[_previewFrameIdx];
            }
        }

        if (previewSpr == null) previewSpr = _profile.GetFirstAvailableSprite();

        if (previewSpr != null)
        {
            ModularSpriteAnimatorEditor.DrawSpritePreviewBox(previewSpr, 88f);
        }
        else
        {
            EditorGUILayout.HelpBox("Belum ada gambar. Drag sprite ke 'Sprite Diam' atau ke salah satu State Animasi di bawah.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
    }

    [MenuItem("PacketCourier/Custom Boss & Musuh/Buat Semua Profile Template (Resources)")]
    public static void CreateAllDefaultProfiles()
    {
        const string folder = "Assets/Resources/CustomAnimations";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        EnsureProfileAsset($"{folder}/Enemy_Patrol.asset", isBoss: false);
        EnsureProfileAsset($"{folder}/Enemy_Flying.asset", isBoss: false);
        EnsureProfileAsset($"{folder}/Enemy_GlitchPhantom.asset", isBoss: false);
        EnsureProfileAsset($"{folder}/Enemy_FirewallTurret.asset", isBoss: false);

        EnsureProfileAsset($"{folder}/Boss_Default.asset", isBoss: true);
        EnsureProfileAsset($"{folder}/Boss_World1_Sentinel.asset", isBoss: true);
        EnsureProfileAsset($"{folder}/Boss_World2_Censor.asset", isBoss: true);
        EnsureProfileAsset($"{folder}/Boss_World4_Solaris.asset", isBoss: true);
        EnsureProfileAsset($"{folder}/Boss_World5_Final.asset", isBoss: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
        if (folderObj != null) EditorGUIUtility.PingObject(folderObj);
        Debug.Log("[PacketCourier] Semua Profile Template Boss & Musuh siap di Assets/Resources/CustomAnimations/");
    }

    [MenuItem("PacketCourier/Custom Boss & Musuh/Buka Folder CustomAnimations")]
    public static void PingCustomAnimationsFolder()
    {
        const string folder = "Assets/Resources/CustomAnimations";
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
        else
        {
            CreateAllDefaultProfiles();
        }
    }

    private static void EnsureProfileAsset(string assetPath, bool isBoss)
    {
        var existing = AssetDatabase.LoadAssetAtPath<SpriteAnimationProfile>(assetPath);
        if (existing != null) return;

        var profile = ScriptableObject.CreateInstance<SpriteAnimationProfile>();
        if (isBoss) profile.PopulateBossPresets();
        else profile.PopulateEnemyPresets();

        AssetDatabase.CreateAsset(profile, assetPath);
    }
}
