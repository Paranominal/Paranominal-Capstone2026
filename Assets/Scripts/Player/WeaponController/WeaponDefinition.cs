using UnityEngine;

// Summary: Per-weapon-type stats. Create one asset per gun. The WeaponController reads from
// whichever definition is currently equipped. Adding a new gun is "create SO, set up prefab, assign."
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Tooltip("Stable unique identifier for save/load.")]
    public string id;

    public string displayName;

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
}
