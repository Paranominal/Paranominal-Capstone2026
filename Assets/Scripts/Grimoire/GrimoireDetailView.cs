using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: Displays the details of the currently selected entry on the right page
// of the grimoire. Panels call SetDetail to populate the view, or Clear to blank it.
// Lives on the right-page area of the Grimoire UI.
public class GrimoireDetailView : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text flavourText;
    [SerializeField] private TMP_Text hintText;

    [Header("Image")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private GameObject imageContainer;

    [Header("Recipe Detail")]
    [SerializeField] private GameObject ingredientListContainer;
    [SerializeField] private Transform ingredientListParent;
    [SerializeField] private GameObject ingredientRowPrefab;

    // Summary: Populate the detail view with item or spell data.
    public void SetDetail(string name, string description, string flavour, string hint, Sprite icon)
    {
        ShowTextFields(true);
        HideIngredients();

        if (nameText != null) nameText.SetText(name ?? "");
        if (descriptionText != null) descriptionText.SetText(description ?? "");
        if (flavourText != null) flavourText.SetText(flavour ?? "");
        if (hintText != null) hintText.SetText(hint ?? "");

        if (displayImage != null && imageContainer != null)
        {
            if (icon != null)
            {
                displayImage.texture = icon.texture;
                imageContainer.SetActive(true);
            }
            else
            {
                imageContainer.SetActive(false);
            }
        }
    }

    // Summary: Populate the detail view with a bestiary entry (uses Texture2D snapshot).
    public void SetBestiaryDetail(string name, string description, string flavour, string hint, Texture2D snapshot)
    {
        ShowTextFields(true);
        HideIngredients();

        if (nameText != null) nameText.SetText(name ?? "");
        if (descriptionText != null) descriptionText.SetText(description ?? "");
        if (flavourText != null) flavourText.SetText(flavour ?? "");
        if (hintText != null) hintText.SetText(hint ?? "");

        if (displayImage != null && imageContainer != null)
        {
            if (snapshot != null)
            {
                displayImage.texture = snapshot;
                imageContainer.SetActive(true);
            }
            else
            {
                imageContainer.SetActive(false);
            }
        }
    }

    // Summary: Populate the detail view with recipe data (ingredient list, no quick-slot).
    public void SetRecipeDetail(string name, Recipe recipe)
    {
        ShowTextFields(false);
        if (imageContainer != null) imageContainer.SetActive(false);

        if (nameText != null)
        {
            nameText.gameObject.SetActive(true);
            nameText.SetText(name ?? "");
        }

        ShowIngredients(recipe);
    }

    // Summary: Clear the detail view when no entry is selected.
    public void Clear()
    {
        if (nameText != null) nameText.SetText("");
        if (descriptionText != null) descriptionText.SetText("");
        if (flavourText != null) flavourText.SetText("");
        if (hintText != null) hintText.SetText("");
        if (imageContainer != null) imageContainer.SetActive(false);
        HideIngredients();
    }

    private void ShowTextFields(bool show)
    {
        if (descriptionText != null) descriptionText.gameObject.SetActive(show);
        if (flavourText != null) flavourText.gameObject.SetActive(show);
        if (hintText != null) hintText.gameObject.SetActive(show);
    }

    private void ShowIngredients(Recipe recipe)
    {
        if (ingredientListContainer == null || ingredientListParent == null) return;
        if (ingredientRowPrefab == null) return;

        // Clear existing rows.
        foreach (Transform child in ingredientListParent)
            Destroy(child.gameObject);

        ingredientListContainer.SetActive(true);

        if (recipe == null || recipe.inputs == null) return;

        foreach (Recipe.Ingredient ingredient in recipe.inputs)
        {
            if (ingredient.item == null) continue;

            GameObject row = Instantiate(ingredientRowPrefab, ingredientListParent);
            TMP_Text rowText = row.GetComponentInChildren<TMP_Text>();
            if (rowText != null)
                rowText.SetText($"{ingredient.item.displayName} x{ingredient.quantity}");
        }
    }

    private void HideIngredients()
    {
        if (ingredientListContainer != null)
            ingredientListContainer.SetActive(false);
    }
}
