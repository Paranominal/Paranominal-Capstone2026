using System.Collections.Generic;
using UnityEngine;

// Summary: Tracks which spells the player has learned.
// Lives on GameSystems alongside Inventory, Bestiary, and RecipeBook.
// Spells are learned through gameplay (pickups, quest rewards, etc.).
public class SpellBook : MonoBehaviour
{
    [Header("All Spells")]
    [Tooltip("Master list of every spell in the game. Assign all SpellDefinition assets here.")]
    [SerializeField] private SpellDefinition[] allSpells;

    private HashSet<SpellDefinition> learnedSpells = new HashSet<SpellDefinition>();

    public event System.Action<SpellDefinition> OnSpellLearned;

    // Summary: Marks a spell as learned. Returns true if newly learned, false if already known.
    public bool Learn(SpellDefinition spell)
    {
        if (spell == null) return false;

        if (learnedSpells.Add(spell))
        {
            Debug.Log("Spell learned: " + spell.displayName);
            OnSpellLearned?.Invoke(spell);
            return true;
        }

        return false;
    }

    // Summary: Returns true if the player has learned this spell.
    public bool HasLearned(SpellDefinition spell)
    {
        return spell != null && learnedSpells.Contains(spell);
    }

    // Summary: Returns all learned spells for display in the Grimoire.
    public List<SpellDefinition> GetLearnedSpells()
    {
        return new List<SpellDefinition>(learnedSpells);
    }

    // Summary: Returns all spells regardless of learned state.
    public SpellDefinition[] GetAllSpells()
    {
        return allSpells;
    }
}
