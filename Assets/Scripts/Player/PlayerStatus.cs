using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;

    // EDIT (inventory system): player-side references to the new inventory and discovery systems.
    // Add Inventory and DiscoveryLog as components on the same GameObject, or assign in Inspector.
    [Header("Inventory")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private DiscoveryLog discoveryLog;

    public Inventory Inventory => inventory;
    public DiscoveryLog DiscoveryLog => discoveryLog;

    public bool IsInEncounter { get; private set; } // Whether the player is currently inside an active enemy encounter.

    public void SetInEncounter(bool value)
    {
        IsInEncounter = value;
    }

    // EDIT (inventory system): auto-find components if not assigned in Inspector.
    void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        if (discoveryLog == null)
            discoveryLog = GetComponent<DiscoveryLog>();
    }

    public void TakeDamage(DamageInfo info)
    { // super duper basic just to allow the player to get damage
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        fearBar.TakeDamage(info.amount);
    }
}
