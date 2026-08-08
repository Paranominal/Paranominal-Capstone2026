using UnityEngine;

// EDIT (cleanup): stripped unused Inventory and DiscoveryLog fields.
// All consumers access those systems via FindAnyObjectByType directly.
// EDIT (camera-shake): triggers CameraEffects.Shake on damage taken.
public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;
    [SerializeField] private CameraEffects cameraEffects;

    public bool IsInEncounter { get; private set; }

    public void SetInEncounter(bool value)
    {
        IsInEncounter = value;
    }

    void Awake()
    {
        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();
        if (cameraEffects == null)
            cameraEffects = GetComponentInChildren<CameraEffects>();
    }

    public void TakeDamage(DamageInfo info)
    {
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        if (fearBar != null)
            fearBar.TakeDamage(info.amount);
        if (cameraEffects != null)
            cameraEffects.Shake();
    }
}