using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Summary: Drives the cauldron crafting panel. Left side shows available ingredients
// from the player's inventory. Right side shows 6 slots for selected ingredients.
// Selections are visual only; inventory is consumed on Brew.
public class CauldronUI : MonoBehaviour
{
    [Header("Left Panel: Ingredients List")]
    [SerializeField] private Transform ingredientListContent;
    [SerializeField] private CauldronIngredientRow ingredientRowPrefab;

    [Header("Right Panel: Cauldron Slots")]
    [SerializeField] private CauldronSlot[] slots = new CauldronSlot[6];

    [Header("Buttons")]
    [SerializeField] private Button brewButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private float warningDuration = 1.5f;

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    // Pending ingredients: what the player has "added" to the cauldron visually.
    // Not consumed from Inventory until Brew.
    private Dictionary<ItemDefinition, int> pendingIngredients = new Dictionary<ItemDefinition, int>();

    // Active row instances keyed by item for quick updates.
    private Dictionary<ItemDefinition, CauldronIngredientRow> activeRows = new Dictionary<ItemDefinition, CauldronIngredientRow>();

    private CauldronEntity cauldron;
    private Inventory inventory;
    private Coroutine warningCoroutine;

    private const int MaxSlots = 6;

    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (brewButton != null)
            brewButton.onClick.AddListener(OnBrewClicked);
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAll);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        foreach (CauldronSlot slot in slots)
        {
            if (slot != null)
                slot.Setup(this);
        }

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    // Summary: Opens the cauldron UI and populates the ingredient list.
    public void Open(CauldronEntity entity, Inventory inv)
    {
        cauldron = entity;
        inventory = inv;

        pendingIngredients.Clear();

        // Ensure all slots are active in case they were individually deactivated.
        foreach (CauldronSlot slot in slots)
        {
            if (slot != null)
                slot.gameObject.SetActive(true);
        }

        PopulateIngredientList();
        RefreshSlots();

        if (panel != null)
            panel.SetActive(true);
    }

    // Summary: Closes the panel, clears selections, and notifies the cauldron entity.
    public void Close()
    {
        pendingIngredients.Clear();

        if (panel != null)
            panel.SetActive(false);

        if (cauldron != null)
            cauldron.OnUIClosed();

        cauldron = null;
        inventory = null;
    }

    // Summary: Adds one of the given ingredient to the pending selection.
    public void AddIngredient(ItemDefinition item)
    {
        if (item == null || inventory == null) return;

        int available = GetAvailableCount(item);
        if (available <= 0)
        {
            ShowWarning("Not enough!");
            return;
        }

        // Check if we'd exceed the slot limit (unique ingredient types, not total quantity).
        if (!pendingIngredients.ContainsKey(item) && pendingIngredients.Count >= MaxSlots)
        {
            ShowWarning("No empty slots!");
            return;
        }

        if (pendingIngredients.ContainsKey(item))
            pendingIngredients[item]++;
        else
            pendingIngredients[item] = 1;

        RefreshDisplay();
    }

    // Summary: Removes one of the given ingredient from the pending selection.
    public void RemoveIngredient(ItemDefinition item)
    {
        if (item == null) return;
        if (!pendingIngredients.ContainsKey(item)) return;

        pendingIngredients[item]--;
        if (pendingIngredients[item] <= 0)
            pendingIngredients.Remove(item);

        RefreshDisplay();
    }

    // Summary: Clears all pending ingredient selections.
    public void ClearAll()
    {
        pendingIngredients.Clear();
        RefreshDisplay();
    }

    private void OnBrewClicked()
    {
        if (cauldron == null || pendingIngredients.Count == 0) return;

        bool success = cauldron.TryBrew(pendingIngredients);

        if (success)
        {
            // Refresh the list since inventory changed.
            pendingIngredients.Clear();
            PopulateIngredientList();
            RefreshSlots();
        }
        else
        {
            ShowWarning("Invalid recipe!");
        }
    }

    // Summary: Returns how many of an item the player can still add (inventory count minus pending).
    private int GetAvailableCount(ItemDefinition item)
    {
        int held = inventory != null ? inventory.GetCount(item) : 0;
        int pending = pendingIngredients.ContainsKey(item) ? pendingIngredients[item] : 0;
        return held - pending;
    }

    // Summary: Builds the left-side ingredient rows from the player's inventory.
    private void PopulateIngredientList()
    {
        Debug.Log($"[CauldronUI] Inventory={inventory} Ingredient count={inventory?.GetItems(ItemTag.Ingredient).Count}");
        // Clear old rows.
        foreach (Transform child in ingredientListContent)
            Destroy(child.gameObject);

        activeRows.Clear();

        if (inventory == null) return;

        List<ItemDefinition> ingredients = inventory.GetItems(ItemTag.Ingredient);
        foreach (ItemDefinition item in ingredients)
        {
            int available = GetAvailableCount(item);
            CauldronIngredientRow row = Instantiate(ingredientRowPrefab, ingredientListContent);
            row.Setup(item, available, this);
            Debug.Log($"[CauldronUI] Created row for {item.displayName}, active={row.gameObject.activeSelf}, parent={row.transform.parent.name}");
            activeRows[item] = row;
        }
    }

    // Summary: Updates the right-side slots from pendingIngredients.
    private void RefreshSlots()
    {
        int slotIndex = 0;

        foreach (var kvp in pendingIngredients)
        {
            if (slotIndex >= slots.Length) break;

            if (slots[slotIndex] != null)
                slots[slotIndex].Fill(kvp.Key, kvp.Value);

            slotIndex++;
        }

        // Clear remaining slots.
        for (int i = slotIndex; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Clear();
        }
    }

    // Summary: Updates both sides and notifies the cauldron for color preview.
    private void RefreshDisplay()
    {
        // Update left side quantities.
        foreach (var kvp in activeRows)
        {
            int available = GetAvailableCount(kvp.Key);
            kvp.Value.UpdateQuantity(available);
        }

        RefreshSlots();

        if (cauldron != null)
            cauldron.UpdateColor(pendingIngredients);
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningCoroutine = StartCoroutine(WarningRoutine(message));
    }

    private IEnumerator WarningRoutine(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(warningDuration);
        warningText.gameObject.SetActive(false);
        warningCoroutine = null;
    }
}