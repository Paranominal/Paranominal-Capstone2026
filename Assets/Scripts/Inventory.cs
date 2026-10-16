using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<CollectibleObject> inventory;
    [SerializeField] private bool doDuplicates;
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private UIPullFocus UIPuller;
    

    public void Add(CollectibleObject item) //similar to old scan, adds entry but keeps item in-world
    {
        if (inventory.Contains(item) && !doDuplicates) return;
        inventory.Add(item);
        AddToGrimoire(item);
    }
    public void Add(CollectibleObject item, bool cache) //if cache is true, hides object as child. if false, destroys it.
    {
        if (inventory.Contains(item) && !doDuplicates) return;
        inventory.Add(item);
        AddToGrimoire(item);

        // Trigger pickup spawns before hiding or destroying the collected item.
        CollectibleObject collectible = item.GetComponent<CollectibleObject>();
        if (collectible != null) collectible.OnPickedUp();

        if (cache)
        {
            item.gameObject.SetActive(false);
            item.gameObject.transform.SetParent(transform);
        }
        else Destroy(item.gameObject);
    }
    public void Add(CollectibleObject item, bool cache, Dialogue pickupDialogue) //if cache is true, hides object as child. if false, destroys it.
    {
        if (inventory.Contains(item) && !doDuplicates) return;
        inventory.Add(item);
        AddToGrimoire(item);

        // Trigger pickup spawns before hiding or destroying the collected item.
        CollectibleObject collectible = item.GetComponent<CollectibleObject>();
        if (collectible != null) collectible.OnPickedUp();

        if (cache)
        {
            item.gameObject.SetActive(false);
            item.gameObject.transform.SetParent(transform);
        }
        else Destroy(item.gameObject);

        DoDialogue(pickupDialogue);
    }
    private void AddToGrimoire(CollectibleObject item)
    {
        if (grimoire != null)
        {
            ALTGrimoireEntry entry = item.grimoireEntry;
            if (entry != null && entry.entryName != "") grimoire.AddEntry(entry, true); //makes no entry if it isnt named.
            else Debug.Log($"[{this}] Item Collected [{item}] has no named Entry");
        }
    }
    private void DoDialogue(Dialogue pickupDialogue)
    {
        dialogueManager.StartDialogue(pickupDialogue);
        UIPuller.PullFocus();
    }
    public void Remove(CollectibleObject item)
    {
        if (!inventory.Contains(item)) return;
        inventory.Remove(item);
    }

    public bool HasItem(CollectibleObject item)
    {
        return item != null && inventory != null && inventory.Contains(item);
    }

    public bool ConsumeItem(CollectibleObject item)
    {
        if (!HasItem(item)) return false;

        inventory.Remove(item);
        Destroy(item);
        return true;
    }
}
