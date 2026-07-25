using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;

    [Header("Inventory")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private DiscoveryLog discoveryLog;

    public Inventory Inventory => inventory;
    public DiscoveryLog DiscoveryLog => discoveryLog;

    public bool IsInEncounter { get; private set; }

    public void SetInEncounter(bool value)
    {
        IsInEncounter = value;
    }

    // EDIT (auto-resolve): all cross-prefab references use FindAnyObjectByType fallbacks
    // so they survive being on different prefabs or across scene loads.
    void Awake()
    {
        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (discoveryLog == null)
            discoveryLog = FindAnyObjectByType<DiscoveryLog>();
    }

    public void TakeDamage(DamageInfo info)
    { // super duper basic just to allow the player to get damage
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        if (fearBar != null)
            fearBar.TakeDamage(info.amount);
    }
}
