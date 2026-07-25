using System.Collections.Generic;
using UnityEngine;

// Summary: Simple inventory storing ItemDefinitions with quantities.
// Lives on the player. All ownership checks (keys, ingredients, etc.) route through here.
public class Inventory : MonoBehaviour
{
    // Summary: Internal storage. Keyed by ItemDefinition asset reference, so lookups are
    // reference-equal and immune to string typos.
    private Dictionary<ItemDefinition, int> items = new Dictionary<ItemDefinition, int>();

    public event System.Action OnInventoryChanged;

    // Summary: Add count of an item, clamped to its maxStack.
    public void Add(ItemDefinition item, int count = 1)
    {
        if (item == null || count <= 0) return;

        if (items.ContainsKey(item))
            items[item] = Mathf.Min(items[item] + count, item.maxStack);
        else
            items[item] = Mathf.Min(count, item.maxStack);

        OnInventoryChanged?.Invoke();
    }

    // Summary: Remove count of an item. Returns true if the item was present and removed.
    // If the quantity hits zero, the entry is deleted entirely.
    public bool Remove(ItemDefinition item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        if (!items.ContainsKey(item)) return false;

        items[item] -= count;
        if (items[item] <= 0)
            items.Remove(item);

        OnInventoryChanged?.Invoke();
        return true;
    }

    // Summary: True if the player is carrying at least one of this item.
    public bool Has(ItemDefinition item)
    {
        return item != null && items.ContainsKey(item) && items[item] > 0;
    }

    // Summary: Returns the quantity held, or 0 if not carried.
    public int GetCount(ItemDefinition item)
    {
        if (item == null) return 0;
        return items.TryGetValue(item, out int count) ? count : 0;
    }

    // Summary: Returns all held items whose definition carries any of the given tags.
    public List<ItemDefinition> GetItems(ItemTag filter)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        foreach (var kvp in items)
        {
            if (kvp.Key.HasTag(filter) && kvp.Value > 0)
                result.Add(kvp.Key);
        }
        return result;
    }

    // Summary: Returns all held items regardless of tag.
    public List<ItemDefinition> GetAllItems()
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        foreach (var kvp in items)
        {
            if (kvp.Value > 0)
                result.Add(kvp.Key);
        }
        return result;
    }

    // Summary: Removes everything from the inventory.
    public void Clear()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
    }
}
