// Summary: Per-interaction snapshot of the player side, passed into IInteractable calls so
// objects can query and spend inventory without knowing about the player directly.
public class InteractionContext
{
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
