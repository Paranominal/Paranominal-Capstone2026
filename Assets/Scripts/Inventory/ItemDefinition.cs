using UnityEngine;

// Summary: Immutable definition of an item. Create one asset per item type.
// These ship with the game and are never modified at runtime.
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Tooltip("Stable unique identifier. Used for save files and debugging. Never change once shipped.")]
    public string id;

    [Tooltip("Name shown to the player in the UI.")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [TextArea(2, 4)]
    public string flavourText;

    public Sprite icon;

    public ItemTag tags;

    [Tooltip("Maximum stack size. 1 for unique items like keys.")]
    public int maxStack = 1;

    // Summary: True if the item carries the given tag (or any of the given tags).
    public bool HasTag(ItemTag tag)
    {
        return (tags & tag) != 0;
    }
}
