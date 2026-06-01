using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;
    [SerializeField] private SpiritBar spiritBar;

    public void TakeDamage(DamageInfo info)
    { // super duper basic just to allow the player to get damage
        Debug.Log($"[PlayerStatus] Player hit for {info.amount}.");
        fearBar.TakeDamage();
        spiritBar.ModifySpirit(-info.amount);
    }
}
