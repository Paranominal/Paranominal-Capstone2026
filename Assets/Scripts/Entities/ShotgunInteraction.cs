using UnityEngine;

// EDIT (weapon consolidation): references WeaponController instead of WeaponStateController.
// NOTE: still uses string matching against grimoire entries. Should migrate to Inventory/ItemDefinition
// in a future pass.
public class ShotgunInteraction : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private WeaponController weaponController;
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
        weaponController.SetWeaponEnabled(true);
    }
}
