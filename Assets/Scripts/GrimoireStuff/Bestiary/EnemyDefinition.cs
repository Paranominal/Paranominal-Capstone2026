using UnityEngine;

// Summary: Immutable definition of an enemy type for the Bestiary. Create one asset per enemy type.
// The 'id' field is the future save key, never rename once shipped.
// EDIT (bestiary-pages): page content now lives in pagePrefab. description, flavourText and hintText removed.
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Tooltip("Stable unique identifier. Used for save files and debugging. Never change once shipped. Prefix with the definition type (i.e. enemy_).")]
    public string id;

    [Tooltip("Name shown in the Bestiary's entry list.")]
    public string displayName;

    // EDIT (bestiary-pages): the page shown on the right side of the Bestiary when this entry is selected.
    [Tooltip("Page prefab instantiated on the detail view when this entry is selected. Holds the heading, flavour text, kill count and sprite.")]
    public BestiaryPage pagePrefab;
}
