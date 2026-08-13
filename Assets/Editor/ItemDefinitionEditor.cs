using UnityEditor;
using UnityEngine;

// Summary: Unified editor for ItemDefinition and all subclasses (WeaponDefinition,
// RangedWeaponDefinition, MeleeWeaponDefinition). Conditionally shows tag-gated
// composition fields based on which ItemTags are set.
[CustomEditor(typeof(ItemDefinition), editorForChildClasses: true)]
public class ItemDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemDefinition item = (ItemDefinition)target;

        bool hasTagGatedFields = item.HasTag(ItemTag.Recipe) || item.HasTag(ItemTag.Throwable);
        if (!hasTagGatedFields) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tag-Gated Data", EditorStyles.boldLabel);

        if (item.HasTag(ItemTag.Recipe))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recipe"), new GUIContent("Recipe"));

        if (item.HasTag(ItemTag.Throwable))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("throwableData"), new GUIContent("Throwable Data"));

        serializedObject.ApplyModifiedProperties();
    }
}
