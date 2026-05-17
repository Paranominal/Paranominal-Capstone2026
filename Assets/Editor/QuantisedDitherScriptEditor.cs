using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuantisedDitherScript))]
[CanEditMultipleObjects]
public class QuantisedDitherScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty quantisedDitherMaterial = serializedObject.FindProperty("quantisedDitherMaterial");

        SerializedProperty redSteps = serializedObject.FindProperty("redSteps");
        SerializedProperty greenSteps = serializedObject.FindProperty("greenSteps");
        SerializedProperty blueSteps = serializedObject.FindProperty("blueSteps");
        SerializedProperty effectStrength = serializedObject.FindProperty("effectStrength");

        SerializedProperty ditherStrength = serializedObject.FindProperty("ditherStrength");
        SerializedProperty bayerMatrixSize = serializedObject.FindProperty("bayerMatrixSize");
        SerializedProperty usePS1Matrix = serializedObject.FindProperty("usePS1Matrix");

        SerializedProperty usePerceivedBrightness = serializedObject.FindProperty("usePerceivedBrightness");
        SerializedProperty perceptualGamma = serializedObject.FindProperty("perceptualGamma");

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(quantisedDitherMaterial);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Colour Quantisation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(redSteps);
        EditorGUILayout.PropertyField(greenSteps);
        EditorGUILayout.PropertyField(blueSteps);
        EditorGUILayout.PropertyField(effectStrength);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Dithering Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(ditherStrength);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Matrix Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(usePS1Matrix);

        if (!usePS1Matrix.hasMultipleDifferentValues)
        {
            if (!usePS1Matrix.boolValue)
            {
                EditorGUILayout.PropertyField(bayerMatrixSize);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Bayer matrix selection is hidden while PS1 Matrix mode is active.",
                    MessageType.Info
                );
            }
        }
        else
        {
            EditorGUILayout.PropertyField(bayerMatrixSize);
            EditorGUILayout.HelpBox(
                "Multiple selected objects have different PS1 Matrix states, so the Bayer size field is shown.",
                MessageType.None
            );
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Brightness Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(usePerceivedBrightness);

        if (!usePerceivedBrightness.hasMultipleDifferentValues)
        {
            if (usePerceivedBrightness.boolValue)
            {
                EditorGUILayout.PropertyField(perceptualGamma);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(perceptualGamma);
            EditorGUILayout.HelpBox(
                "Multiple selected objects have different Perceived Brightness states, so Perceptual Gamma is shown.",
                MessageType.None
            );
        }

        serializedObject.ApplyModifiedProperties();
    }
}