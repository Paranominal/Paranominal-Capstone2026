using UnityEngine;

// Summary: Per-interaction snapshot of the player side, passed into IInteractable calls so
// objects can query and spend inventory (currently the Grimoire) without knowing about it directly.
public class InteractionContext
{
    public Transform player;
    public Camera camera;
    public ALTGrimoire grimoire;

    // Summary: True if the Grimoire holds a collected entry whose name matches keyName.
    // Searches all entries, not just the currently-open page, so a prompt can react the
    // instant a key is picked up.
    public bool HasKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName) || grimoire == null || grimoire.entries == null)
            return false;

        foreach (ALTGrimoireEntry e in grimoire.entries)
        {
            if (e != null && e.collected && e.entryName == keyName)
                return true;
        }
        return false;
    }

    // Summary: Marks the matching key entry as no longer collected (spends the key).
    public void ConsumeKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName) || grimoire == null)
            return;

        ALTGrimoireEntry entry = grimoire.GetEntry(keyName);
        if (entry != null)
            grimoire.CollectEntry(entry, false);
    }
}
