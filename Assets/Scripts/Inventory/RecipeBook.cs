using System.Collections.Generic;
using UnityEngine;

// Summary: Holds every recipe in the game and tracks which the player has discovered.
// Discovery happens via successful experimentation at a cauldron, or by finding
// a recipe in the world. Lives on GameSystems (persistent).
public class RecipeBook : MonoBehaviour
{
    [Header("All Recipes")]
    [Tooltip("Master list of every recipe in the game. Assign all Recipe assets here.")]
    [SerializeField] private Recipe[] allRecipes;

    private HashSet<Recipe> discoveredRecipes = new HashSet<Recipe>();

    public event System.Action<Recipe> OnRecipeDiscovered;

    // Summary: Returns true if the player has discovered this recipe.
    public bool IsDiscovered(Recipe recipe)
    {
        return recipe != null && discoveredRecipes.Contains(recipe);
    }

    // Summary: Marks a recipe as discovered. Returns true if it was newly discovered,
    // false if it was already known.
    public bool Discover(Recipe recipe)
    {
        if (recipe == null) return false;

        if (discoveredRecipes.Add(recipe))
        {
            Debug.Log("Recipe discovered: " + recipe.recipeName);
            OnRecipeDiscovered?.Invoke(recipe);
            return true;
        }

        return false;
    }

    // Summary: Tries to match selected ingredients against all recipes (not just discovered ones).
    // Returns the matching recipe, or null if no match. The cauldron uses this for brewing.
    public Recipe FindMatchingRecipe(Dictionary<ItemDefinition, int> selectedIngredients)
    {
        if (allRecipes == null || selectedIngredients == null) return null;

        foreach (Recipe recipe in allRecipes)
        {
            if (recipe != null && recipe.Matches(selectedIngredients))
                return recipe;
        }

        return null;
    }

    // Summary: Returns all discovered recipes for display in the Grimoire.
    public List<Recipe> GetDiscoveredRecipes()
    {
        return new List<Recipe>(discoveredRecipes);
    }

    // Summary: Returns all recipes regardless of discovery state.
    public Recipe[] GetAllRecipes()
    {
        return allRecipes;
    }
}
