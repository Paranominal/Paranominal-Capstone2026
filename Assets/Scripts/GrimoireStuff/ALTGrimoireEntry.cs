using UnityEngine;
using UnityEngine.Serialization;
using System;

[Serializable]
public class ALTGrimoireEntry
{
    public string entryName;
    public string flavourText;
    public string hintText;
    public string completeText;
    [System.NonSerialized]  // this is just to prevent people from fucking with it. comment it out if you want to monitor for testing
    public bool collected = false;
    // EDIT (bestiary-pages): replaces snapshotImage. Set by designers instead of taken at runtime. Shown in the polaroid.
    [Tooltip("Image of the item, shown in the Grimoire's polaroid.")]
    [FormerlySerializedAs("snapshotImage")]
    public Texture2D image;

}
