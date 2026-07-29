using UnityEngine;

// Summary: Immutable definition of a spell. Create one asset per spell type.
// Stats are stubs pending the casting system design.
[CreateAssetMenu(fileName = "NewSpell", menuName = "Game/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    [Tooltip("Stable unique identifier. Prefix with spell_. Never rename once shipped.")]
    public string id;

    [Tooltip("Name shown to the player in the UI.")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [TextArea(2, 4)]
    public string flavourText;

    [TextArea(2, 4)]
    public string hintText;

    public Sprite icon;

    [Header("Stats (Stubs)")]
    [Tooltip("Cooldown in seconds between casts.")]
    public float cooldown = 1f;

    [Tooltip("Spirit resource cost per cast.")]
    public float spiritCost = 10f;

    [Tooltip("Base damage dealt. 0 for non-damage spells.")]
    public int damage = 0;

    [Tooltip("The type of effect this spell applies.")]
    public SpellEffect effect = SpellEffect.None;
}
