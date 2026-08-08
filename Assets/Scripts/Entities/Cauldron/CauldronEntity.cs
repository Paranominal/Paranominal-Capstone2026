using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: The world-space cauldron. Implements IInteractable so the focus controller
// detects it and shows "E - Brew". Opening the UI disables player input (no pause).
// Queries the player's RecipeBook for matching recipes rather than holding its own list.
// EDIT (prompt-simplification): surface removed from ResolvePrompt.
public class CauldronEntity : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private Material cauldronMaterial;
    [SerializeField] private Color defaultColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnHeightOffset = 1.5f;

    [Header("UI")]
    [SerializeField] private CauldronUI cauldronUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO brewSuccessSound;
    [SerializeField] private SoundDataSO brewFailSound;

    private Inventory inventory;
    private RecipeBook recipeBook;
    private bool isOpen;

    void Awake()
    {
        if (cauldronUI == null)
            cauldronUI = FindAnyObjectByType<CauldronUI>(FindObjectsInactive.Include);
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (recipeBook == null)
            recipeBook = FindAnyObjectByType<RecipeBook>();

        if (cauldronMaterial != null)
            cauldronMaterial.color = defaultColor;
    }

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        if (isOpen)
            return InteractionPrompt.None;

        return new InteractionPrompt
        {
            label = "Brew",
            actionName = "Collect",
        };
    }

    public void Interact(InteractionContext context)
    {
        if (isOpen) return;

        isOpen = true;

        InputSystem.actions.FindActionMap("Player")?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cauldronUI != null)
            cauldronUI.Open(this, inventory);
    }

    // Summary: Called by CauldronUI when the player closes the panel.
    public void OnUIClosed()
    {
        isOpen = false;
        ResetColor();

        InputSystem.actions.FindActionMap("Player")?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Summary: Called by CauldronUI when the player presses Brew.
    // Checks all recipes in the player's RecipeBook (not just discovered ones).
    // On success, consumes ingredients, spawns reward, and discovers the recipe if new.
    // Returns true on success, false on failure.
    public bool TryBrew(Dictionary<ItemDefinition, int> selectedIngredients)
    {
        if (inventory == null || recipeBook == null) return false;

        Recipe matched = recipeBook.FindMatchingRecipe(selectedIngredients);

        if (matched != null)
        {
            // Consume ingredients from inventory.
            foreach (Recipe.Ingredient req in matched.inputs)
            {
                if (req.item != null)
                    inventory.Remove(req.item, req.quantity);
            }

            // Spawn the reward.
            if (matched.outputPrefab != null)
            {
                Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
                pos += new Vector3(0f, spawnHeightOffset, 0f);
                Instantiate(matched.outputPrefab, pos, Quaternion.identity);
            }

            // Discover the recipe if the player didn't already know it.
            bool newlyDiscovered = recipeBook.Discover(matched);
            if (newlyDiscovered)
                Debug.Log("New recipe discovered through experimentation: " + matched.recipeName);

            if (brewSuccessSound != null && audioSource != null)
                AudioManager.PlaySound(brewSuccessSound, audioSource);

            Debug.Log("Brew success! Recipe: " + matched.recipeName);
            return true;
        }

        if (brewFailSound != null && audioSource != null)
            AudioManager.PlaySound(brewFailSound, audioSource);

        Debug.Log("Brew failed: no matching recipe.");
        return false;
    }

    // Summary: Called by CauldronUI as the player adds/removes ingredients to preview the color.
    public void UpdateColor(Dictionary<ItemDefinition, int> selectedIngredients)
    {
        if (cauldronMaterial == null) return;

        if (selectedIngredients == null || selectedIngredients.Count == 0)
        {
            cauldronMaterial.color = defaultColor;
            return;
        }

        Color blended = Color.black;
        int totalCount = 0;

        foreach (var kvp in selectedIngredients)
        {
            blended += kvp.Key.tintColor * kvp.Value;
            totalCount += kvp.Value;
        }

        if (totalCount > 0)
            blended /= totalCount;

        blended.a = 1f;
        cauldronMaterial.color = blended;
    }

    public void ResetColor()
    {
        if (cauldronMaterial != null)
            cauldronMaterial.color = defaultColor;
    }
}