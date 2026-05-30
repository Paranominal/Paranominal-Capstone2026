#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Summary: Custom inspector for SoundDataSO. 
// Provides in-editor preview, conditional field display (3D fields hidden when spatialBlend == 0), and validation warnings.
// Preview uses Unity's internal AudioUtil via reflection.
[CustomEditor(typeof(SoundDataSO))]
public class SoundDataSOEditor : Editor
{
    // Cached reflection handles to Unity's internal AudioUtil. Resolved once at static init; methods that aren't found stay null and are skipped at call time.
    private static readonly MethodInfo playPreviewClipMethod;
    private static readonly MethodInfo stopAllPreviewClipsMethod;
    private static readonly MethodInfo setPreviewClipVolumeMethod;
    private static readonly MethodInfo setPreviewClipPitchMethod;

    static SoundDataSOEditor()
    {
        Type audioUtil = Type.GetType("UnityEditor.AudioUtil, UnityEditor");
        if (audioUtil == null) return;

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;

        playPreviewClipMethod = audioUtil.GetMethod("PlayPreviewClip", flags, null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        stopAllPreviewClipsMethod = audioUtil.GetMethod("StopAllPreviewClips", flags);
    }

    // Serialized property handles
    private SerializedProperty clipsProp;
    private SerializedProperty mixerGroupProp;
    private SerializedProperty selectionModeProp;
    private SerializedProperty volumeProp;
    private SerializedProperty volumeOffsetRangeProp;
    private SerializedProperty pitchProp;
    private SerializedProperty spatialBlendProp;
    private SerializedProperty minDistanceProp;
    private SerializedProperty maxDistanceProp;
    private SerializedProperty rolloffModeProp;
    private SerializedProperty loopProp;
    private SerializedProperty minIntervalProp;

    private void OnEnable()
    {
        clipsProp             = serializedObject.FindProperty(nameof(SoundDataSO.clips));
        mixerGroupProp        = serializedObject.FindProperty(nameof(SoundDataSO.mixerGroup));
        selectionModeProp     = serializedObject.FindProperty(nameof(SoundDataSO.selectionMode));
        volumeProp            = serializedObject.FindProperty(nameof(SoundDataSO.volume));
        volumeOffsetRangeProp = serializedObject.FindProperty(nameof(SoundDataSO.volumeOffsetRange));
        pitchProp             = serializedObject.FindProperty(nameof(SoundDataSO.pitch));
        spatialBlendProp      = serializedObject.FindProperty(nameof(SoundDataSO.spatialBlend));
        minDistanceProp       = serializedObject.FindProperty(nameof(SoundDataSO.minDistance));
        maxDistanceProp       = serializedObject.FindProperty(nameof(SoundDataSO.maxDistance));
        rolloffModeProp       = serializedObject.FindProperty(nameof(SoundDataSO.rolloffMode));
        loopProp              = serializedObject.FindProperty(nameof(SoundDataSO.loop));
        minIntervalProp       = serializedObject.FindProperty(nameof(SoundDataSO.minInterval));
    }

    private void OnDisable()
    {
        // Stop preview when the inspector closes so audio doesn't linger.
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SoundDataSO sound = (SoundDataSO)target;

        DrawPreviewButtons(sound);
        DrawValidationWarnings(sound);

        EditorGUILayout.Space();
        DrawSection("Clips", () =>
        {
            EditorGUILayout.PropertyField(clipsProp, true);
            EditorGUILayout.PropertyField(selectionModeProp);
        });

        DrawSection("Mixer", () =>
        {
            EditorGUILayout.PropertyField(mixerGroupProp);
        });

        DrawSection("Volume", () =>
        {
            EditorGUILayout.PropertyField(volumeProp);
            EditorGUILayout.PropertyField(volumeOffsetRangeProp,
                new GUIContent("Offset Range", "Random offset added to volume per play (min, max)."));
        });

        DrawSection("Pitch", () =>
        {
            EditorGUILayout.PropertyField(pitchProp,
                new GUIContent("Pitch Range", "Random pitch sampled between min and max per play."));
        });

        DrawSection("Spatial", () =>
        {
            EditorGUILayout.PropertyField(spatialBlendProp);

            // 3D fields only shown when spatial blend is above 0
            if (spatialBlendProp.floatValue > 0f)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(minDistanceProp);
                EditorGUILayout.PropertyField(maxDistanceProp);
                EditorGUILayout.PropertyField(rolloffModeProp);
                EditorGUI.indentLevel--;
            }
        });

        DrawSection("Behaviour", () =>
        {
            EditorGUILayout.PropertyField(loopProp);
            EditorGUILayout.PropertyField(minIntervalProp,
                new GUIContent("Min Interval", "Seconds before this sound can play again. 0 disables."));
        });

        serializedObject.ApplyModifiedProperties();
    }

    // Renders a labelled section with a bold header and the given body.
    private void DrawSection(string label, Action body)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        body();
    }

    // Play and Stop buttons at the top of the inspector.
    private void DrawPreviewButtons(SoundDataSO sound)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            bool hasClips = sound.clips != null && sound.clips.Length > 0;

            using (new EditorGUI.DisabledScope(!hasClips))
            {
                if (GUILayout.Button("▶ Play Preview", GUILayout.Height(24)))
                    PlayPreview(sound);
            }

            if (GUILayout.Button("■ Stop", GUILayout.Height(24)))
                StopPreview();
        }
    }

    // Renders HelpBox warnings for any configuration issues.
    private void DrawValidationWarnings(SoundDataSO sound)
    {
        if (sound.clips == null || sound.clips.Length == 0)
            EditorGUILayout.HelpBox("No clips assigned. This sound won't play anything.", MessageType.Warning);

        if (sound.volumeOffsetRange.x > sound.volumeOffsetRange.y)
            EditorGUILayout.HelpBox("Volume offset range is inverted (min > max).", MessageType.Warning);

        if (sound.pitch.x > sound.pitch.y)
            EditorGUILayout.HelpBox("Pitch range is inverted (min > max).", MessageType.Warning);

        if (sound.spatialBlend > 0f && sound.minDistance > sound.maxDistance)
            EditorGUILayout.HelpBox("Min distance is greater than max distance.", MessageType.Warning);

        // Effective max volume (base + max offset, clamped). Warn if it's zero.
        float effectiveMax = Mathf.Clamp01(sound.volume + Mathf.Max(sound.volumeOffsetRange.x, sound.volumeOffsetRange.y));
        if (effectiveMax <= 0f)
            EditorGUILayout.HelpBox("Effective volume is zero. This sound will be silent.", MessageType.Warning);
    }

    // Plays a representative preview of the sound using Unity's AudioUtil.
    // Picks a clip via the SO's selection mode, samples volume and pitch, and applies them if the running Unity version supports the AudioUtil setters.
    private void PlayPreview(SoundDataSO sound)
    {
        if (playPreviewClipMethod == null) return;

        AudioClip clip = sound.GetClip();
        if (clip == null) return;

        // Always stop anything currently previewing before starting a new one.
        StopPreview();

        playPreviewClipMethod.Invoke(null, new object[] { clip, 0, sound.loop });

        // Best-effort volume/pitch application. If the setters aren't present
        // in this Unity version, the preview plays at default volume/pitch.
        if (setPreviewClipVolumeMethod != null)
        {
            try { setPreviewClipVolumeMethod.Invoke(null, new object[] { clip, sound.GetRandomVolume() }); }
            catch { /* signature mismatch — silently fall back */ }
        }

        if (setPreviewClipPitchMethod != null)
        {
            try { setPreviewClipPitchMethod.Invoke(null, new object[] { clip, sound.GetRandomPitch() }); }
            catch { /* signature mismatch — silently fall back */ }
        }
    }

    // Stops any AudioUtil preview currently playing.
    private void StopPreview()
    {
        stopAllPreviewClipsMethod?.Invoke(null, null);
    }
}
#endif
