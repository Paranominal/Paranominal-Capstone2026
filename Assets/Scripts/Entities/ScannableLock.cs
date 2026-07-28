using UnityEngine;

// Summary: Unlocks a door when the player discovers a specific item.
// EDIT (event-driven): subscribes to DiscoveryLog.OnDiscoveryChanged instead of polling every frame.
public class ScannableLock : MonoBehaviour
{
    public ItemDefinition requiredDiscovery;
    public Door targetDoor;
    public bool destroysItem;
    [SerializeField] private GameObject itemToDestroy;

    private DiscoveryLog discoveryLog;
    private bool hasFired = false;

    void Start()
    {
        if (discoveryLog == null)
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
        if (targetDoor == null)
            targetDoor = GetComponentInChildren<Door>();

        if (discoveryLog != null)
            discoveryLog.OnDiscoveryChanged += CheckDiscovery;
    }

    void OnDestroy()
    {
        if (discoveryLog != null)
            discoveryLog.OnDiscoveryChanged -= CheckDiscovery;
    }

    private void CheckDiscovery()
    {
        if (hasFired || targetDoor == null || requiredDiscovery == null || discoveryLog == null)
            return;

        if (discoveryLog.HasDiscovered(requiredDiscovery))
        {
            targetDoor.Unlock();
            hasFired = true;

            if (destroysItem && itemToDestroy != null)
                Destroy(itemToDestroy);
        }
    }
}
