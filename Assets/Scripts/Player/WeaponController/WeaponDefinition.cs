using UnityEngine;

// Summary: Per-weapon-type stats. Extends ItemDefinition so weapons can be stored in the
// Inventory like any other item while also carrying weapon-specific configuration.
// Create one asset per gun via Create > Game > Weapon Definition.
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Definition")]
public class WeaponDefinition : ItemDefinition
{
    [Header("Ammo")]
    public int magazineSize = 6;
    public float reloadDuration = 2f;
    public bool autoReload = true;
    public float postShotReloadDelay = 0.25f;

    [Header("Firing")]
    public float shotCooldown = 0.2f;

    [Header("Hitscan")]
    public float hitscanRange = 1000f;

    [Header("Ammo Types")]
    public bool ironBarrelAvailable = true;
    public bool silverBarrelAvailable = true;

    [Header("Equipment")]
    [Tooltip("The first-person weapon model prefab. Instantiated under the hand transform when equipped.")]
    public GameObject equippedPrefab;
}