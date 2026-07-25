using UnityEngine;

// Summary: Per-interaction snapshot of the player side, passed into IInteractable calls so
// objects can query and spend inventory without knowing about it directly.
public class InteractionContext
{
    public Transform player;
    public Camera camera;
    public ALTGrimoire grimoire;

    // EDIT (inventory system): the context now reads from Inventory instead of the Grimoire.
    // This is the source of truth for item ownership. The grimoire reference is kept for
    // legacy scripts (Container, InteractionObject) that still read from it.
    public Inventory inventory;

    // Summary: True if the player is carrying the given item.
    public bool HasKey(ItemDefinition item)
    {
        if (item == null || inventory == null)
            return false;

        return inventory.Has(item);
    }

    // Summary: Removes one of the given item from inventory (spends the key).
    public void ConsumeKey(ItemDefinition item)
    {
        if (item == null || inventory == null)
            return;

        inventory.Remove(item, 1);
    }
}
