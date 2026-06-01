using UnityEngine;

public class ShotgunInteraction : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private WeaponStateController stateController;
    private ShotgunCollection currentItem;
    private ALTGrimoireEntry shotgunEntry;
    private bool collected;


    void Update()
    {
        collected = grimoire.entries.Exists(shotgunEntry => shotgunEntry.entryName == "shotgun");
        CollectShotgun();
    }

    void CollectShotgun()
    {
        if (!collected) return;
        stateController.SetWeaponEnabled(true);
    }
}