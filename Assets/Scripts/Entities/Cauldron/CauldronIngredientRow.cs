using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Summary: A single row in the left-side ingredient list. Shows the ingredient name,
// available quantity, and an Add button that appears on hover.
public class CauldronIngredientRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button addButton;
    [SerializeField] private CanvasGroup addButtonGroup;   // controls visibility without affecting layout
    [SerializeField] private Color quantityColor = new Color(0.6f, 0.6f, 0.6f);

    private ItemDefinition item;
    private CauldronUI owner;

    public void Setup(ItemDefinition itemDef, int availableCount, CauldronUI ui)
    {
        item = itemDef;
        owner = ui;

        nameText.text = itemDef.displayName;
        quantityText.text = "x " + availableCount;
        quantityText.color = quantityColor;

        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(OnAddClicked);

        // Keep the button active for layout, but invisible until hovered.
        SetAddButtonVisible(false);
    }

    public void UpdateQuantity(int availableCount)
    {
        quantityText.text = "x " + availableCount;
    }

    private void OnAddClicked()
    {
        if (owner != null && item != null)
            owner.AddIngredient(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetAddButtonVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetAddButtonVisible(false);
    }

    private void SetAddButtonVisible(bool visible)
    {
        if (addButtonGroup != null)
        {
            addButtonGroup.alpha = visible ? 1f : 0f;
            addButtonGroup.blocksRaycasts = visible;
        }
    }
}