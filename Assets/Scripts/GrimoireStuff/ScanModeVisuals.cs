using UnityEngine;
using System.Collections.Generic;

public class ScanModeVisuals : MonoBehaviour
{
    public static ScanModeVisuals instance;

    private List<ALTScannableObject> allScannables = new List<ALTScannableObject>();

    public static Color ColObject = Color.green;
    public static Color ColEnemy = Color.red;
    public static Color ColKeyItem = Color.yellow;
    public static Color ColScanned = Color.white;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Uh oh we got multiple ScanModeVisuals in the scene");
        }
        else
        {
            instance = this;
        }
    }

    public void RegisterScannable(ALTScannableObject s) // called by ALTScannableObject
    {
        if (!allScannables.Contains(s))
        {
            allScannables.Add(s);
        }
    }

    public void SetScanMode(bool active)
    {
        foreach (var s in allScannables)
        {
            if (s != null) // otherwise we get nullrefs lmao
            {
                s.outline.enabled = active;
                if (active) ApplyOutlineColor(s);
            }
        }
    }

    public void ApplyOutlineColor(ALTScannableObject s)
    {
        if (ALTGrimoire.instance.CompareEntry(s.entry))
        {
            s.outline.OutlineColor = ColScanned;
            return;
        }
        switch (s.category) // there's probably a cleaner way of doing this but i am dumb
        {
            case ScanCategory.Enemy:
                {
                    s.outline.OutlineColor = ColEnemy; 
                    break;
                }
            case ScanCategory.KeyItem:
                {
                    s.outline.OutlineColor = ColKeyItem; 
                    break;
                }
            default:
                {
                    s.outline.OutlineColor = ColObject; 
                    break;
                }
        }
    }

    public static Color GetCategoryColor(ALTScannableObject s)
    {
        switch (s.category)
        {
            case ScanCategory.Enemy:
                {
                    return ColEnemy;
                }
            case ScanCategory.KeyItem: 
                { 
                    return ColKeyItem; 
                }
            default:
                {
                    return ColObject;
                }
        }
    }
}