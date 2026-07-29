using UnityEngine;

// Summary: Per-ranged-weapon stats. Carries ammo, reload, and hitscan configuration.
// Create one asset per gun via Create > Game > Ranged Weapon Definition.
// EDIT (weapon system): extracted from the old WeaponDefinition, which is now the shared base.
[CreateAssetMenu(fileName = "NewRangedWeapon", menuName = "Game/Ranged Weapon Definition")]
public class RangedWeaponDefinition : WeaponDefinition
{
    [Header("Ammo")]
    public int magazineSize = 6;
    public float reloadDuration = 2f;
    public bool autoReload = true;
    public float postShotReloadDelay = 0.25f;

    [Header("Hitscan")]
    public float hitscanRange = 1000f;

    [Header("Ammo Types")]
    public bool ironBarrelAvailable = true;
    public bool silverBarrelAvailable = true;
}
