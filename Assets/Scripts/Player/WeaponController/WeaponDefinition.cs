using UnityEngine;

// Summary: Shared base for all weapon types. Extends ItemDefinition so weapons
// can be stored in Inventory like any other item. Subclassed by
// RangedWeaponDefinition and MeleeWeaponDefinition for type-specific stats.
// EDIT (weapon system): stripped ranged-specific fields into RangedWeaponDefinition.
public class WeaponDefinition : ItemDefinition
{
    [Header("Shared Weapon Stats")]
    [Tooltip("Minimum time between attacks.")]
    public float attackCooldown = 0.2f;

    [Tooltip("Knockback force applied to targets on hit. Consumed by EnemyKnockback.")]
    public float knockbackForce = 0f;

    [Header("Equipment")]
    [Tooltip("The first-person weapon model prefab. Instantiated under the hand transform when equipped.")]
    public GameObject equippedPrefab;
}
