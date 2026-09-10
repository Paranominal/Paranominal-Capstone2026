// Summary:
// Conditionally shows/hides a field in the inspector based on the value of another serialized field.
// Supports bools (show when true/false), enums (show when matching value), and object references (show when assigned).
// Set the Header property to draw a bold header above the field that also hides when the condition is false.

using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ShowIfAttribute : PropertyAttribute
{
    public readonly string ConditionField;
    public readonly int EnumValue;
    public readonly bool IsEnum;
    public readonly bool Invert;

    // optional header text drawn above the field when visible
    public string Header;

    // show when bool field is true
    public ShowIfAttribute(string conditionField)
    {
        ConditionField = conditionField;
    }

    // show when bool field matches expectedValue
    public ShowIfAttribute(string conditionField, bool expectedValue)
    {
        ConditionField = conditionField;
        Invert = !expectedValue;
    }

    // show when enum field equals enumValue (pass as int, e.g. (int)EnemyClass.Champion)
    public ShowIfAttribute(string conditionField, int enumValue)
    {
        ConditionField = conditionField;
        EnumValue = enumValue;
        IsEnum = true;
    }
}
