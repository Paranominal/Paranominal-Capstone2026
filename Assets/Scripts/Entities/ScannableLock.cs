using UnityEngine;

// Summary: A world event that unlocks a door when the player has discovered a specific item.
// EDIT (grimoire migration): checks DiscoveryLog instead of grimoire.CompareEntry.
public class ScannableLock : MonoBehaviour
{
    // EDIT (grimoire migration): was ALTScannableObject. Now holds an ItemDefinition directly,
    // since we're checking discovery status, not grimoire entries.
    public ItemDefinition requiredDiscovery;
    public Door targetDoor;
    public bool destroysItem;
    [SerializeField] private GameObject itemToDestroy;   // the world object to destroy (if destroysItem is true)

    private DiscoveryLog discoveryLog;
    private bool hasFired = false;

    void Start()
    {
        if (discoveryLog == null)
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
        if (targetDoor == null)
            targetDoor = GetComponentInChildren<Door>();
    }

    void Update()
    {
        if (hasFired || targetDoor == null || requiredDiscovery == null || discoveryLog == null)
        {
            return;
        }

        if (discoveryLog.HasDiscovered(requiredDiscovery))
        {
            targetDoor.Unlock();
            hasFired = true;

            if (destroysItem && itemToDestroy != null)
            {
                Destroy(itemToDestroy);
            }
        }
    }
}
