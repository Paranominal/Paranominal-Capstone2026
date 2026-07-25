using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<GameObject> inventory;
    [SerializeField] private bool doDuplicates;
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private DialogueManager dialogueManager;
    

    public void Add(GameObject item) //similar to old scan, adds entry but keeps item in-world
    {
        if (inventory.Contains(item) && !doDuplicates) return;
        inventory.Add(item);
        AddToGrimoire(item);
    }
    public void Add(GameObject item, bool cache) //if cache is true, hides object as child. if false, destroys it.
    {
        if (inventory.Contains(item) && !doDuplicates) return;
        inventory.Add(item);
        AddToGrimoire(item);

        if (cache)
        {
            item.gameObject.SetActive(false);
            item.gameObject.transform.SetParent(transform);
        }
        else Destroy(item.gameObject);
    }
    public void Add(GameObject item, bool cache, GameObject pickupDialogue) //if cache is true, hides object as child. if false, destroys it.
    {
        if (inventory.Contains(item) && !doDuplicates) return;
        inventory.Add(item);
        AddToGrimoire(item);

        if (cache)
        {
            item.gameObject.SetActive(false);
            item.gameObject.transform.SetParent(transform);
        }
        else Destroy(item.gameObject);

        DoDialogue(pickupDialogue);
    }
    private void AddToGrimoire(GameObject item)
    {
        if (grimoire != null)
        {
            ALTGrimoireEntry entry = item.GetComponent<CollectibleObject>().grimoireEntry;
            if (entry.entryName != "") grimoire.AddEntry(entry, true); //makes no entry if it isnt named.
            else Debug.Log($"[{this}] Item Collected [{item}] has no named Entry");
        }
    }
    private void DoDialogue(GameObject pickupDialogue)
    {
        dialogueManager.StartDialogue(pickupDialogue);
    }
    public void Remove(GameObject item)
    {
        if (!inventory.Contains(item)) return;
        inventory.Remove(item);
    }
}
