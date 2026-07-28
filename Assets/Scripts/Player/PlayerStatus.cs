using UnityEngine;

// EDIT (cleanup): stripped unused Inventory and DiscoveryLog fields.
// All consumers access those systems via FindAnyObjectByType directly.
public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;

    public bool IsInEncounter { get; private set; }

    public void SetInEncounter(bool value)
    {
        IsInEncounter = value;
    }

    void Awake()
    {
        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();
    }

    public void TakeDamage(DamageInfo info)
    {
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        if (fearBar != null)
            fearBar.TakeDamage(info.amount);
    }
}
