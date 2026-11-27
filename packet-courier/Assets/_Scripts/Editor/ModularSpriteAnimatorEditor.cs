using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for ModularSpriteAnimator.
/// Supports drag-and-drop single Default Sprite, reusable SpriteAnimationProfile,
/// one-click Enemy & 3-Phase Boss state presets, multi-sprite / sprite-sheet drag-and-drop,
/// live thumbnail preview, and Edit-mode scene preview scrubbing.
/// </summary>
[CustomEditor(typeof(ModularSpriteAnimator))]
public class ModularSpriteAnimatorEditor : Editor
{
    private ModularSpriteAnimator _animator;
    private SerializedProperty _profileProp;
    private SerializedProperty _defaultSpriteProp;
    private SerializedProperty _targetRendererProp;
    private SerializedProperty _defaultStateProp;
    private SerializedProperty _playOnStartProp;
    private SerializedProperty _invertFacingProp;
    private SerializedProperty _useExternalFacingProp;
    private SerializedProperty _visualScaleProp;
    private SerializedProperty _visualOffsetProp;
    private SerializedProperty _playbackSpeedProp;
    private SerializedProperty _applyStateTintProp;
    private SerializedProperty _statesProp;
    private SerializedProperty _onAnimationStartedProp;
    private SerializedProperty _onAnimationCompletedProp;
    private SerializedProperty _onFrameTriggerProp;
    private SerializedProperty _onFrameChangedProp;

    private int _selectedPreviewStateIndex = 0;
    private int _previewFrameIndex = 0;
    private bool _showAdvanced = false;
    private bool _showEvents = false;
    private bool _isScenePreviewing = false;
    private double _lastEditorTime;

    private static readonly Color HeaderBg = new Color(0.08f, 0.11f, 0.16f, 1f);
    private static readonly Color AccentCyan = new Color(0.18f, 0.85f, 0.95f, 1f);

    private void OnEnable()
    {
        _animator = (ModularSpriteAnimator)target;
        _profileProp = serializedObject.FindProperty("profile");
        _defaultSpriteProp = serializedObject.FindProperty("defaultSprite");
        _targetRendererProp = serializedObject.FindProperty("targetRenderer");
        _defaultStateProp = serializedObject.FindProperty("defaultState");
        _playOnStartProp = serializedObject.FindProperty("playOnStart");
        _invertFacingProp = serializedObject.FindProperty("invertFacing");
        _useExternalFacingProp = serializedObject.FindProperty("useExternalFacing");
        _visualScaleProp = serializedObject.FindProperty("visualScaleMultiplier");
        _visualOffsetProp = serializedObject.FindProperty("visualOffset");
        _playbackSpeedProp = serializedObject.FindProperty("playbackSpeed");
        _applyStateTintProp = serializedObject.FindProperty("applyStateTint");
        _statesProp = serializedObject.FindProperty("states");
        _onAnimationStartedProp = serializedObject.FindProperty("onAnimationStarted");
        _onAnimationCompletedProp = serializedObject.FindProperty("onAnimationCompleted");
        _onFrameTriggerProp = serializedObject.FindProperty("onFrameTrigger");
        _onFrameChangedProp = serializedObject.FindProperty("onFrameChanged");

        _lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!_isScenePreviewing || _animator == null || _animator.states == null || _animator.states.Count == 0)
            return;

        double now = EditorApplication.timeSinceStartup;
        _lastEditorTime = now;

        if (_selectedPreviewStateIndex >= 0 && _selectedPreviewStateIndex < _animator.states.Count)
        {
            SpriteAnimationState state = _animator.states[_selectedPreviewStateIndex];
            if (state != null && state.FrameCount > 0)
            {
                int next = (int)((now * Mathf.Max(1f, state.framesPerSecond)) % state.FrameCount);
                if (next != _previewFrameIndex)
                {
                    _previewFrameIndex = next;
                    _animator.EditorPreviewFrame(state.stateName, _previewFrameIndex);
                    Repaint();
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawCustomHeader();
        DrawQuickPresetsBar();
        DrawPlaybackControls();
        DrawAnimationStatesList();
        DrawEventSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCustomHeader()
    {
        Rect headerRect = EditorGUILayout.GetControlRect(false, 42f);
        EditorGUI.DrawRect(headerRect, HeaderBg);
        EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 2f, headerRect.width, 2f), AccentCyan);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = AccentCyan }
        };
        GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.72f, 0.80f, 0.90f, 1f) }
        };

        GUI.Label(new Rect(headerRect.x + 12f, headerRect.y + 4f, headerRect.width - 24f, 18f), "MODULAR SPRITE ANIMATOR", titleStyle);
        int stateCount = _statesProp.arraySize;
        string status = _animator.HasAnyCustomArt() ? "Custom Sprite Aktif" : "Belum Ada Sprite (Pakai Default Game)";
        GUI.Label(new Rect(headerRect.x + 12f, headerRect.y + 22f, headerRect.width - 24f, 14f), $"{stateCount} State  •  {status}", subStyle);

        EditorGUILayout.Space(6f);

        // Quick Asset & Single Sprite Box
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(_profileProp, new GUIContent("Profile Asset (Opsional)", "Drag SpriteAnimationProfile dari Assets/Resources/CustomAnimations/ atau isi manual di bawah."));
        EditorGUILayout.PropertyField(_defaultSpriteProp, new GUIContent("Sprite Diam (Default)", "Cukup drag 1 gambar sprite ke sini jika belum punya frame animasi."));
        EditorGUILayout.PropertyField(_invertFacingProp, new GUIContent("Balik Arah (Invert Facing)", "Centang jika sprite aslinya menghadap KIRI."));
        EditorGUILayout.PropertyField(_visualScaleProp, new GUIContent("Skala Gambar", "Perbesar/perkecil gambar tanpa ubah hitbox."));
        EditorGUILayout.PropertyField(_visualOffsetProp, new GUIContent("Offset Posisi (X, Y)", "Geser posisi gambar agar kaki pas di lantai."));
        EditorGUILayout.PropertyField(_playbackSpeedProp, new GUIContent("Kecepatan Animasi", "Pengali kecepatan FPS global."));

        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Pengaturan Lanjutan", true);
        if (_showAdvanced)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_targetRendererProp, new GUIContent("Target Renderer"));
            EditorGUILayout.PropertyField(_defaultStateProp, new GUIContent("Default State"));
            EditorGUILayout.PropertyField(_playOnStartProp, new GUIContent("Play On Start"));
            EditorGUILayout.PropertyField(_useExternalFacingProp, new GUIContent("Use External Facing", "Aktif otomatis untuk Boss_Grounded agar tidak double-flip."));
            EditorGUILayout.PropertyField(_applyStateTintProp, new GUIContent("Apply State Tint"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4f);
    }

    private void DrawQuickPresetsBar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("+ Preset Musuh", "Buat slot: Idle, Walk, Chase, Windup, Attack, Hurt, Death"), GUILayout.Height(26f)))
        {
            AddEnemyPresetStates();
        }

        if (GUILayout.Button(new GUIContent("+ Preset Boss 3-Fase", "Buat slot lengkap Boss: Idle, Walk, Windup, Slash, Dash, Jump, Slam, PhaseShift, Hurt, Death"), GUILayout.Height(26f)))
        {
            AddBossPresetStates();
        }

        if (GUILayout.Button(new GUIContent("+ State", "Tambah 1 state animasi baru"), GUILayout.Height(26f), GUILayout.Width(72f)))
        {
            AddNewEmptyState("NewState");
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4f);
    }

    private void DrawPlaybackControls()
    {
        if (_statesProp.arraySize == 0) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("PREVIEW ANIMASI DI SCENE", EditorStyles.boldLabel);

        List<string> stateNames = new List<string>();
        for (int i = 0; i < _statesProp.arraySize; i++)
        {
            SerializedProperty s = _statesProp.GetArrayElementAtIndex(i);
            string name = s.FindPropertyRelative("stateName").stringValue;
            stateNames.Add(string.IsNullOrEmpty(name) ? $"State {i}" : name);
        }

        _selectedPreviewStateIndex = Mathf.Clamp(_selectedPreviewStateIndex, 0, stateNames.Count - 1);

        EditorGUILayout.BeginHorizontal();
        _selectedPreviewStateIndex = EditorGUILayout.Popup("State", _selectedPreviewStateIndex, stateNames.ToArray());

        if (GUILayout.Button(_isScenePreviewing ? "Stop" : "Play Preview", GUILayout.Width(96f)))
        {
            _isScenePreviewing = !_isScenePreviewing;
            _lastEditorTime = EditorApplication.timeSinceStartup;
        }
        EditorGUILayout.EndHorizontal();

        if (_selectedPreviewStateIndex >= 0 && _selectedPreviewStateIndex < _animator.states.Count)
        {
            SpriteAnimationState state = _animator.states[_selectedPreviewStateIndex];
            int maxFrames = state != null ? state.FrameCount : 0;
            if (maxFrames > 0)
            {
                EditorGUI.BeginChangeCheck();
                _previewFrameIndex = EditorGUILayout.IntSlider("Frame", Mathf.Clamp(_previewFrameIndex, 0, maxFrames - 1), 0, maxFrames - 1);
                if (EditorGUI.EndChangeCheck())
                {
                    _isScenePreviewing = false;
                    _animator.EditorPreviewFrame(state.stateName, _previewFrameIndex);
                }

                Sprite previewSpr = state.frames[Mathf.Clamp(_previewFrameIndex, 0, maxFrames - 1)];
                if (previewSpr != null)
                {
                    DrawSpritePreviewBox(previewSpr, 72f);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("State ini belum diisi sprite. Drag sprite ke kotak drop di bawah.", MessageType.Info);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4f);
    }

    private void DrawAnimationStatesList()
    {
        EditorGUILayout.LabelField("DAFTAR STATE ANIMASI", EditorStyles.boldLabel);

        for (int i = 0; i < _statesProp.arraySize; i++)
        {
            SerializedProperty stateProp = _statesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = stateProp.FindPropertyRelative("stateName");
            SerializedProperty framesProp = stateProp.FindPropertyRelative("frames");
            SerializedProperty fpsProp = stateProp.FindPropertyRelative("framesPerSecond");
            SerializedProperty loopProp = stateProp.FindPropertyRelative("loop");
            SerializedProperty triggerFrameProp = stateProp.FindPropertyRelative("triggerFrame");
            SerializedProperty eventTagProp = stateProp.FindPropertyRelative("eventTag");
            SerializedProperty localOffsetProp = stateProp.FindPropertyRelative("localOffset");
            SerializedProperty stateTintProp = stateProp.FindPropertyRelative("stateTint");
            SerializedProperty unscaledProp = stateProp.FindPropertyRelative("unscaledTime");

            string stateName = nameProp.stringValue;
            int frameCount = framesProp.arraySize;
            float duration = (frameCount > 0 && fpsProp.floatValue > 0f) ? frameCount / fpsProp.floatValue : 0f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            string badge = frameCount > 0
                ? $"{stateName}  [{frameCount} frame • {fpsProp.floatValue:F0} FPS • {duration:F2}d{(loopProp.boolValue ? " • Loop" : " • 1x")}]"
                : $"{stateName}  [Kosong — drag sprite di dalam]";
            stateProp.isExpanded = EditorGUILayout.Foldout(stateProp.isExpanded, badge, true, EditorStyles.foldoutHeader);

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
                EditorGUILayout.PropertyField(triggerFrameProp, new GUIContent("Trigger Frame (-1 = mati)"));
                if (triggerFrameProp.intValue >= 0)
                {
                    EditorGUILayout.PropertyField(eventTagProp, new GUIContent("Event Tag"));
                }
                EditorGUILayout.PropertyField(stateTintProp, new GUIContent("State Tint"));
                EditorGUILayout.PropertyField(unscaledProp, new GUIContent("Unscaled Time"));

                DrawSpriteDropArea(framesProp);
                EditorGUILayout.PropertyField(framesProp, new GUIContent("Frame Sprite"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
    }

    public static void DrawSpriteDropArea(SerializedProperty framesProp)
    {
        EditorGUILayout.BeginHorizontal();
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 30.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Banyak Sprite / Sprite Sheet ke Sini", EditorStyles.helpBox);

        if (framesProp.arraySize > 0 && GUILayout.Button("Clear", GUILayout.Width(50f), GUILayout.Height(30f)))
        {
            framesProp.ClearArray();
            GUI.changed = true;
        }
        EditorGUILayout.EndHorizontal();

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    List<Sprite> droppedSprites = CollectSpritesFromObjects(DragAndDrop.objectReferences);

                    if (droppedSprites.Count > 0)
                    {
                        droppedSprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
                        int startIndex = framesProp.arraySize;
                        framesProp.arraySize += droppedSprites.Count;
                        for (int s = 0; s < droppedSprites.Count; s++)
                        {
                            framesProp.GetArrayElementAtIndex(startIndex + s).objectReferenceValue = droppedSprites[s];
                        }
                        GUI.changed = true;
                    }
                }
                Event.current.Use();
                break;
        }
    }

    public static List<Sprite> CollectSpritesFromObjects(UnityEngine.Object[] objects)
    {
        var result = new List<Sprite>();
        if (objects == null) return result;

        foreach (UnityEngine.Object draggedObj in objects)
        {
            if (draggedObj is Sprite spr)
            {
                result.Add(spr);
            }
            else if (draggedObj is Texture2D tex)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (sub is Sprite s) result.Add(s);
                }
            }
            else if (draggedObj is DefaultAsset)
            {
                string folderPath = AssetDatabase.GetAssetPath(draggedObj);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
                    foreach (string g in guids)
                    {
                        string p = AssetDatabase.GUIDToAssetPath(g);
                        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(p))
                        {
                            if (sub is Sprite s) result.Add(s);
                        }
                    }
                }
            }
        }
        return result;
    }

    public static void DrawSpritePreviewBox(Sprite sprite, float height)
    {
        if (sprite == null || sprite.texture == null) return;
        Rect box = GUILayoutUtility.GetRect(height, height, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(box, new Color(0.07f, 0.09f, 0.13f, 1f));

        Rect tr = sprite.textureRect;
        float aspect = tr.height > 0f ? tr.width / tr.height : 1f;
        float drawH = height - 8f;
        float drawW = Mathf.Min(box.width - 8f, drawH * aspect);
        drawH = aspect > 0.001f ? drawW / aspect : drawH;

        Rect drawRect = new Rect(
            box.x + (box.width - drawW) * 0.5f,
            box.y + (box.height - drawH) * 0.5f,
            drawW,
            drawH);

        Rect uv = new Rect(
            tr.x / sprite.texture.width,
            tr.y / sprite.texture.height,
            tr.width / sprite.texture.width,
            tr.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv);
    }

    private void DrawEventSection()
    {
        _showEvents = EditorGUILayout.Foldout(_showEvents, "Unity Events (Opsional)", true, EditorStyles.foldoutHeader);
        if (_showEvents)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_onAnimationStartedProp);
            EditorGUILayout.PropertyField(_onAnimationCompletedProp);
            EditorGUILayout.PropertyField(_onFrameTriggerProp);
            EditorGUILayout.PropertyField(_onFrameChangedProp);
            EditorGUILayout.EndVertical();
        }
    }

    private void AddEnemyPresetStates()
    {
        EnsureStateExists("Idle", 6f, true, -1, "");
        EnsureStateExists("Walk", 10f, true, -1, "");
        EnsureStateExists("Chase", 12f, true, -1, "");
        EnsureStateExists("Windup", 10f, false, -1, "");
        EnsureStateExists("Attack", 12f, false, 1, "hit");
        EnsureStateExists("Hurt", 12f, false, -1, "");
        EnsureStateExists("Death", 8f, false, -1, "");
        serializedObject.ApplyModifiedProperties();
    }

    private void AddBossPresetStates()
    {
        EnsureStateExists("Idle", 6f, true, -1, "");
        EnsureStateExists("Walk", 9f, true, -1, "");
        EnsureStateExists("Windup", 10f, false, -1, "");
        EnsureStateExists("Slash", 14f, false, 2, "hit");
        EnsureStateExists("Attack", 12f, false, 2, "hit");
        EnsureStateExists("Dash", 14f, true, -1, "");
        EnsureStateExists("Jump", 10f, false, -1, "");
        EnsureStateExists("Slam", 12f, false, 1, "slam");
        EnsureStateExists("PhaseShift", 10f, false, -1, "");
        EnsureStateExists("Hurt", 12f, false, -1, "");
        EnsureStateExists("Death", 8f, false, -1, "");
        serializedObject.ApplyModifiedProperties();
    }

    private void EnsureStateExists(string name, float fps, bool loop, int triggerFrame, string tag)
    {
        for (int i = 0; i < _statesProp.arraySize; i++)
        {
            SerializedProperty s = _statesProp.GetArrayElementAtIndex(i);
            if (string.Equals(s.FindPropertyRelative("stateName").stringValue, name, StringComparison.OrdinalIgnoreCase))
                return;
        }

        int index = _statesProp.arraySize;
        _statesProp.InsertArrayElementAtIndex(index);
        SerializedProperty newProp = _statesProp.GetArrayElementAtIndex(index);
        newProp.FindPropertyRelative("stateName").stringValue = name;
        newProp.FindPropertyRelative("frames").arraySize = 0;
        newProp.FindPropertyRelative("framesPerSecond").floatValue = fps;
        newProp.FindPropertyRelative("loop").boolValue = loop;
        newProp.FindPropertyRelative("triggerFrame").intValue = triggerFrame;
        newProp.FindPropertyRelative("eventTag").stringValue = tag;
        newProp.FindPropertyRelative("localOffset").vector2Value = Vector2.zero;
        newProp.FindPropertyRelative("stateTint").colorValue = Color.white;
        newProp.FindPropertyRelative("unscaledTime").boolValue = false;
        newProp.isExpanded = false;
    }

    private void AddNewEmptyState(string baseName)
    {
        string finalName = baseName;
        int count = 1;
        while (_animator.HasState(finalName))
        {
            finalName = $"{baseName}_{count++}";
        }
        EnsureStateExists(finalName, 10f, true, -1, "");
        serializedObject.ApplyModifiedProperties();
    }
}
