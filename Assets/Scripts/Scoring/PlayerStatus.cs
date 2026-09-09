using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;
    [SerializeField] private CameraEffects cameraEffects;

    // Michael feature (fear-effects): tracks whether the player is in an active encounter. Set by encounter managers externally.
    public bool IsInEncounter { get; set; }

    // Michael feature (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        if (cameraEffects == null)
            cameraEffects = FindAnyObjectByType<CameraEffects>();
    }

    public void TakeDamage(DamageInfo info)
    {
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        fearBar.TakeDamage(info.amount);
        cameraEffects?.Shake();
    }
}