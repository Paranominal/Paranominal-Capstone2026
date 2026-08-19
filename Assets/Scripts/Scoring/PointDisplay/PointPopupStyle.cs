using UnityEngine;

// leah, why is this a struct?
// because i like the idea and i want it to be easy to add more popup styles & variables
// and i've written so many structs today i don't know how to do anything else
[System.Serializable]
public struct PointPopupStyle
{
    public string label;   // inspector readability only
    public int minPrecision;
    public string suffix;
    public Color colour;
    public float scaleMultiplier;  // unrequested but also cool
}
