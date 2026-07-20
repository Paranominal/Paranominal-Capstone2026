using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<GameObject> inventory;
    [SerializeField] private bool doDuplicateEntries;

    public void Add(GameObject item) //similar to old scan, adds entry but keeps item in-world
    {
        if (inventory.Contains(item) && !doDuplicateEntries) return;
        inventory.Add(item);
    }
    public void Add(GameObject item, bool cache) //if cache is true, hides object as child. if false, destroys it.
    {
        if (inventory.Contains(item) && !doDuplicateEntries) return;
        inventory.Add(item);

        if (cache)
        {
            item.gameObject.SetActive(false);
            item.gameObject.transform.SetParent(transform);
        }
        else Destroy(item.gameObject);
    }
    public void Remove(GameObject item)
    {
        if (!inventory.Contains(item)) return;
        inventory.Remove(item);
    }
}
