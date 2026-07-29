using UnityEngine;

// Summary: Per-melee-weapon stats. Carries damage and range configuration.
// Create one asset per melee weapon via Create > Game > Melee Weapon Definition.
// The "Fists" empty-hand asset uses this with damage = 0 and knockback only.
[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "Game/Melee Weapon Definition")]
public class MeleeWeaponDefinition : WeaponDefinition
{
    [Header("Melee Stats")]
    [Tooltip("Damage dealt per hit. 0 for knockback-only attacks like the empty hand.")]
    public int damage = 0;

    [Tooltip("Radius of the hitbox trigger used for hit detection.")]
    public float attackRange = 1.5f;
}
