using UnityEngine;
using System.Collections.Generic;

public class ScanModeVisuals : MonoBehaviour
{
    public static ScanModeVisuals instance;

    private List<ALTScannableObject> allScannables = new List<ALTScannableObject>();

    public static Color colUnscanned = Color.white;
    public static Color colScanned = Color.black;

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

    public void RegisterScannable(ALTScannableObject s)
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
                s.SetOutlineVisible(active);
                if (active) ApplyOutlineColor(s);
            }
        }
    }

    public void ApplyOutlineColor(ALTScannableObject s) // decides which colour it should be, setoutlinecolor actually applies it
    {
        if (ALTGrimoire.instance.CompareEntry(s.entry))
        {
            s.SetOutlineColor(colScanned);
        }
        else
        {
            s.SetOutlineColor(colUnscanned);
        }
    }
}