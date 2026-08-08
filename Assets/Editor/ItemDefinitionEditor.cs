using UnityEditor;
using UnityEngine;

// Summary: Custom editor for ItemDefinition. Conditionally shows the Recipe
// reference field only when the Recipe tag is set.
[CustomEditor(typeof(ItemDefinition))]
public class ItemDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemDefinition item = (ItemDefinition)target;

        if (item.HasTag(ItemTag.Recipe))
        {
            SerializedProperty recipeProp = serializedObject.FindProperty("recipe");
            EditorGUILayout.PropertyField(recipeProp, new GUIContent("Recipe"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}