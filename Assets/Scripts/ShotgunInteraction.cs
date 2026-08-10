using UnityEngine;

public class ShotgunInteraction : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private WeaponStateController stateController;
    [SerializeField] private string shotgunName;
    private bool collected;


    void Update()
    {
        collected = grimoire.entries.Exists(shotgunEntry => shotgunEntry.entryName == shotgunName);
        CollectShotgun();
    }

    void CollectShotgun()
    {
        if (!collected) return;
        stateController.SetWeaponEnabled(true);
    }
}