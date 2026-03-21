using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrimoireCodex", menuName = "Scriptable Objects/GrimoireCodex")]
public class GrimoireCodex : ScriptableObject
{
    public List<GrimoireEntry> currentLogs;
}
