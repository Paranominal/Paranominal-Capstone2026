using UnityEngine;

// Summary: Defines a crafting recipe. A list of required ingredients (with quantities)
// and an output prefab to spawn. What the output does (wards, quests, etc.) is defined
// by the game world, not by the recipe.
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Game/Recipe")]
public class Recipe : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public ItemDefinition item;
        public int quantity;
    }

    public string recipeName;
    public Ingredient[] inputs;
    public GameObject outputPrefab;

    // Summary: Returns true if the given ingredient set satisfies this recipe.
    public bool Matches(System.Collections.Generic.Dictionary<ItemDefinition, int> selectedIngredients)
    {
        if (inputs == null || inputs.Length == 0) return false;

        foreach (Ingredient req in inputs)
        {
            if (req.item == null) continue;

            if (!selectedIngredients.ContainsKey(req.item))
                return false;

            if (selectedIngredients[req.item] < req.quantity)
                return false;
        }

        return true;
    }
}
