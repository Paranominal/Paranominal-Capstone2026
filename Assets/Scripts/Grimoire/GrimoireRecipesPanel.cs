using System.Collections.Generic;
using UnityEngine;

// Summary: Recipes tab of the Full Grimoire. Displays discovered recipes from RecipeBook.
// Selected recipes show their ingredient list on the right page.
// Read-only: no quick-slot assignment.
// Shares the ScrollView Content and DetailView with other panels.
public class GrimoireRecipesPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    private RecipeBook recipeBook;
    private List<Recipe> currentRecipes = new List<Recipe>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();
    private int selectedIndex = -1;

    private void Awake()
    {
        if (recipeBook == null)
            recipeBook = FindAnyObjectByType<RecipeBook>();
    }

    private void OnEnable()
    {
        if (recipeBook != null)
            recipeBook.OnRecipeDiscovered += OnRecipeDiscovered;

        Rebuild();
    }

    private void OnDisable()
    {
        if (recipeBook != null)
            recipeBook.OnRecipeDiscovered -= OnRecipeDiscovered;

        ClearList();
    }

    private void OnRecipeDiscovered(Recipe recipe)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        ClearList();

        if (recipeBook == null) return;

        currentRecipes = recipeBook.GetDiscoveredRecipes();

        for (int i = 0; i < currentRecipes.Count; i++)
        {
            int index = i;
            Recipe recipe = currentRecipes[i];

            GameObject entryObj = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObj.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                entry.Setup(index, recipe.recipeName, SelectEntry);
                entryButtons.Add(entry);
            }
        }

        if (currentRecipes.Count > 0)
            SelectEntry(0);
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, currentRecipes.Count - 1);

        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == selectedIndex);

        if (detailView != null && selectedIndex < currentRecipes.Count)
        {
            Recipe recipe = currentRecipes[selectedIndex];
            detailView.SetRecipeDetail(recipe.recipeName, recipe);
        }
    }

    private void ClearList()
    {
        if (listParent != null)
        {
            foreach (Transform child in listParent)
                Destroy(child.gameObject);
        }
        entryButtons.Clear();
        currentRecipes.Clear();
        selectedIndex = -1;
    }
}
