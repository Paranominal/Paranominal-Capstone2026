using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;

    // Michael feature (fear-effects): tracks whether the player is in an active encounter. Set by encounter managers externally.
    public bool IsInEncounter { get; set; }

    public void TakeDamage(DamageInfo info)
    {
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        fearBar.TakeDamage(info.amount);
    }
}
