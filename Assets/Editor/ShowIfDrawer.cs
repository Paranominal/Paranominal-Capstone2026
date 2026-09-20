// Summary:
// Property drawer for ShowIfAttribute. Hides fields when their condition is false
// and draws an optional conditional header. This file goes in an Editor folder.

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property)) return 0f;

        float height = EditorGUI.GetPropertyHeight(property, label, true);

        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        if (!string.IsNullOrEmpty(attr.Header))
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property)) return;

        ShowIfAttribute attr = (ShowIfAttribute)attribute;

        if (!string.IsNullOrEmpty(attr.Header))
        {
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, attr.Header, EditorStyles.boldLabel);
            float offset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.y += offset;
            position.height -= offset;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        SerializedProperty condProp = property.serializedObject.FindProperty(attr.ConditionField);

        if (condProp == null) return true;

        bool result;

        if (attr.IsEnum)
        {
            result = condProp.intValue == attr.EnumValue;
        }
        else if (condProp.propertyType == SerializedPropertyType.Boolean)
        {
            result = condProp.boolValue;
        }
        else if (condProp.propertyType == SerializedPropertyType.ObjectReference)
        {
            result = condProp.objectReferenceValue != null;
        }
        else
        {
            result = true;
        }

        return attr.Invert ? !result : result;
    }
}