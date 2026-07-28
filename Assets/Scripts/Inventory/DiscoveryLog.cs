using System.Collections.Generic;
using UnityEngine;

// Summary: Tracks which items the player has discovered (scanned/encountered).
// Separate from Inventory: you can discover something without carrying it (e.g. enemies, lore).
// Stores per-entry runtime data like snapshots.
public class DiscoveryLog : MonoBehaviour
{
    // Summary: Per-discovery runtime record. Not saved in pass 1.
    public class DiscoveryEntry
    {
        public ItemDefinition item;
        public Texture2D snapshot;
    }

    private Dictionary<ItemDefinition, DiscoveryEntry> entries = new Dictionary<ItemDefinition, DiscoveryEntry>();

    public event System.Action OnDiscoveryChanged;

    // Summary: Register an item as discovered. If already discovered, does nothing.
    // Pass a snapshot texture if one was taken (scan), or null (e.g. enemy first-encounter).
    public DiscoveryEntry Add(ItemDefinition item, Texture2D snapshot = null)
    {
        if (item == null) return null;

        if (entries.ContainsKey(item))
            return entries[item];

        DiscoveryEntry entry = new DiscoveryEntry
        {
            item = item,
            snapshot = snapshot,
        };
        entries[item] = entry;

        OnDiscoveryChanged?.Invoke();
        return entry;
    }

    // Summary: True if the player has discovered this item.
    public bool HasDiscovered(ItemDefinition item)
    {
        return item != null && entries.ContainsKey(item);
    }

    // Summary: Returns the discovery entry for an item, or null if not discovered.
    public DiscoveryEntry GetEntry(ItemDefinition item)
    {
        if (item == null) return null;
        return entries.TryGetValue(item, out DiscoveryEntry entry) ? entry : null;
    }

    // Summary: Returns all discovered items matching the given tag filter.
    public List<DiscoveryEntry> GetEntries(ItemTag filter)
    {
        List<DiscoveryEntry> result = new List<DiscoveryEntry>();
        foreach (var kvp in entries)
        {
            if (kvp.Key.HasTag(filter))
                result.Add(kvp.Value);
        }
        return result;
    }

    // Summary: Returns all discovered items.
    public List<DiscoveryEntry> GetAllEntries()
    {
        return new List<DiscoveryEntry>(entries.Values);
    }
}
