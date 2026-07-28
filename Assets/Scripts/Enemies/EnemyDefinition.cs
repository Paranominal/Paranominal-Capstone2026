using UnityEngine;

// Summary: Immutable definition of an enemy type for the Bestiary.
// Mirrors ItemDefinition's role but carries no inventory-related fields.
// Create one asset per enemy type. The 'id' field is the future save key - never rename once shipped.
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Tooltip("Stable unique identifier. Used for save files and debugging. Never change once shipped. Prefix the ID with the deifnition type (i.e. item_, weapon_, recipe_, enemy_)")]
    public string id;

    [Tooltip("Name shown to the player in the Bestiary UI.")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [TextArea(2, 4)]
    public string flavourText;

    [TextArea(2, 4)]
    public string hintText;
}
