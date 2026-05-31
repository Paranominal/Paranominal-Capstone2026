// Summary: Custom editor for EnemyEncounterManager.
// Hides standard or arena-specific fields based on the selected EncounterMode,
// keeping the inspector clean for designers.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyEncounterManager))]
public class EnemyEncounterManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty encounterModeProp = serializedObject.FindProperty("encounterMode");
        SerializedProperty maxWavesProp = serializedObject.FindProperty("maxWaves");
        SerializedProperty timeBetweenWavesProp = serializedObject.FindProperty("timeBetweenWaves");
        SerializedProperty spawnPointsProp = serializedObject.FindProperty("spawnPoints");
        SerializedProperty enemyPoolProp = serializedObject.FindProperty("enemyPool");
        SerializedProperty startingWaveProp = serializedObject.FindProperty("startingWave");
        SerializedProperty budgetPerWaveProp = serializedObject.FindProperty("budgetPerWave");
        SerializedProperty maxEnemiesPerWaveProp = serializedObject.FindProperty("maxEnemiesPerWave");
        SerializedProperty spawnTimingModeProp = serializedObject.FindProperty("spawnTimingMode");
        SerializedProperty waveDurationProp = serializedObject.FindProperty("waveDuration");
        SerializedProperty spawnLocationModeProp = serializedObject.FindProperty("spawnLocationMode");
        SerializedProperty randomiseSpawnPointProp = serializedObject.FindProperty("randomiseSpawnPoint");
        SerializedProperty randomSpawnRadiusProp = serializedObject.FindProperty("randomSpawnRadius");
        SerializedProperty groundLayerProp = serializedObject.FindProperty("groundLayer");
        SerializedProperty resetCounterProp = serializedObject.FindProperty("resetCounter");

        EnemyEncounterManager.EncounterMode currentMode =
            (EnemyEncounterManager.EncounterMode)encounterModeProp.enumValueIndex;

        EnemyEncounterManager.SpawnLocationMode currentSpawnLocationMode =
            (EnemyEncounterManager.SpawnLocationMode)spawnLocationModeProp.enumValueIndex;

        EnemyEncounterManager.SpawnTimingMode currentSpawnTimingMode =
            (EnemyEncounterManager.SpawnTimingMode)spawnTimingModeProp.enumValueIndex;

        // Encounter mode selector.
        EditorGUILayout.LabelField("Encounter Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(encounterModeProp);
        EditorGUILayout.Space();

        // Shared wave settings.
        EditorGUILayout.LabelField("Wave Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxWavesProp);
        EditorGUILayout.PropertyField(timeBetweenWavesProp);
        EditorGUILayout.Space();

        // Standard mode fields.
        if (currentMode == EnemyEncounterManager.EncounterMode.Standard)
        {
            EditorGUILayout.LabelField("Spawn Points", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnPointsProp, true);
            EditorGUILayout.Space();
        }

        // Arena mode fields.
        if (currentMode == EnemyEncounterManager.EncounterMode.Arena)
        {
            EditorGUILayout.LabelField("Enemy Pool", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enemyPoolProp, true);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Wave Budget", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startingWaveProp);
            EditorGUILayout.PropertyField(budgetPerWaveProp);
            EditorGUILayout.PropertyField(maxEnemiesPerWaveProp);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Spawn Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnTimingModeProp);

            if (currentSpawnTimingMode == EnemyEncounterManager.SpawnTimingMode.OverWaveDuration)
            {
                EditorGUILayout.PropertyField(waveDurationProp);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Spawn Locations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnLocationModeProp);

            if (currentSpawnLocationMode == EnemyEncounterManager.SpawnLocationMode.SpawnPoints)
            {
                EditorGUILayout.PropertyField(spawnPointsProp, true);
                EditorGUILayout.PropertyField(randomiseSpawnPointProp);
            }
            else
            {
                EditorGUILayout.PropertyField(randomSpawnRadiusProp);
                EditorGUILayout.PropertyField(groundLayerProp);
            }

            EditorGUILayout.Space();
        }

        // Shared encounter state.
        EditorGUILayout.LabelField("Encounter State", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(resetCounterProp);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
