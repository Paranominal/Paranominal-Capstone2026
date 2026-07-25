using UnityEngine;
using System;

[Serializable]
public class ALTGrimoireEntry
{
    public string entryName;
    public string flavourText;
    public string hintText;
    public string completeText;
    //[System.NonSerialized]  // this is just to prevent people from fucking with it. comment it out if you want to monitor for testing
    public bool collected = false;
    public Texture2D snapshotImage;

    // EDIT (inventory system): backref to the new item definition asset, used to bridge the grimoire UI to the new inventory system. Null for entries created the old way.
    [NonSerialized] public ItemDefinition itemDefinition;
}
