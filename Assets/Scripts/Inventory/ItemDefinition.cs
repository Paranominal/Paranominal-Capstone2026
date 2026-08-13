using UnityEngine;

// Summary: Immutable definition of an item. Create one asset per item type.
// These ship with the game and are never modified at runtime.
// Tag-gated composition fields (recipe, throwableData) are shown/hidden
// in the inspector by ItemDefinitionEditor.
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Definition")]
public class ItemDefinition : GameDefinition
{
    [Header("Item")]
    public ItemTag tags;

    [Tooltip("Maximum stack size. 1 for unique items like keys.")]
    public int maxStack = 1;

    [Header("Visuals")]
    public Color tintColor = Color.white;

    // Tag-gated composition fields. Managed by ItemDefinitionEditor.
    [HideInInspector] public Recipe recipe;
    [HideInInspector] public ThrowableDefinition throwableData;

    // Summary: True if the item carries the given tag (or any of the given tags).
    public bool HasTag(ItemTag tag)
    {
        return (tags & tag) != 0;
    }
}
