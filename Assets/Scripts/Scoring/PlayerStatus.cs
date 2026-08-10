using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;

    public void TakeDamage(DamageInfo info)
    { // super duper basic just to allow the player to get damage
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        fearBar.TakeDamage(info.amount);
    }
}
