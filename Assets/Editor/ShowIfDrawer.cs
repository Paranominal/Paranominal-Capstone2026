// Summary:
// Property drawer for ShowIfAttribute. Hides fields when their condition is false
// and draws an optional conditional header. This file goes in an Editor folder.

using UnityEditor;
// EDIT (showif-uitk): UI Toolkit support for Unity 6 inspectors.
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    // EDIT (showif-uitk): UI Toolkit path, used by default inspectors in Unity 6.
    // Wraps the existing IMGUI drawing in a container we control, hides it with display none (no leftover gap),
    // and refreshes as soon as the condition field changes instead of waiting for a reselect.
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        SerializedProperty prop = property.Copy();

        IMGUIContainer container = new IMGUIContainer(() =>
        {
            if (prop.serializedObject == null || prop.serializedObject.targetObject == null) return;

            prop.serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            // runs through OnGUI below, which draws the header and the field
            EditorGUILayout.PropertyField(prop, true);
            if (EditorGUI.EndChangeCheck()) prop.serializedObject.ApplyModifiedProperties();
        });

        void Refresh()
        {
            container.style.display = ShouldShow(prop) ? DisplayStyle.Flex : DisplayStyle.None;
            container.MarkDirtyRepaint();
        }

        Refresh();

        SerializedProperty condProp = FindConditionProperty(prop);
        if (condProp != null) container.TrackPropertyValue(condProp, _ => Refresh());

        return container;
    }

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

    // EDIT (showif-uitk): looks for the condition next to the field first (so nested classes/structs work), then falls back to the root.
    private SerializedProperty FindConditionProperty(SerializedProperty property)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        string path = property.propertyPath;
        int lastDot = path.LastIndexOf('.');

        if (lastDot >= 0)
        {
            SerializedProperty sibling = property.serializedObject.FindProperty(path.Substring(0, lastDot + 1) + attr.ConditionField);
            if (sibling != null) return sibling;
        }

        return property.serializedObject.FindProperty(attr.ConditionField);
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        SerializedProperty condProp = FindConditionProperty(property); // EDIT (showif-uitk)

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