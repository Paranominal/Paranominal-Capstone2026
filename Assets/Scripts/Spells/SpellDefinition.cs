using UnityEngine;

// Summary: Immutable definition of a spell. Create one asset per spell type.
// Stats are stubs pending the casting system design.
[CreateAssetMenu(fileName = "NewSpell", menuName = "Game/Spell Definition")]
public class SpellDefinition : GameDefinition
{
    [Header("Stats")]
    [Tooltip("Cooldown in seconds between casts.")]
    public float cooldown = 1f;

    [Tooltip("Spirit resource cost per cast.")]
    public float spiritCost = 10f;

    [Tooltip("Base damage dealt. 0 for non-damage spells.")]
    public int damage = 0;

    [Tooltip("The type of effect this spell applies.")]
    public SpellEffect effect = SpellEffect.None;
}
