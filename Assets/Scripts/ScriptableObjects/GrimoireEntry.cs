using UnityEngine;

[CreateAssetMenu(fileName = "GrimoireEntry", menuName = "Scriptable Objects/GrimoireEntry")]
public class GrimoireEntry : ScriptableObject
{
    public string entryName;
    public string flavourText;
    public string hintText;
    public string completeText;
}
