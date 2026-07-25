using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Summary: A single slot in the right-side cauldron grid. Shows an ingredient icon,
// quantity, and an X button that appears on hover to remove one at a time.
public class CauldronSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button removeButton;
    [SerializeField] private GameObject filledGroup;   // parent of icon + quantity, hidden when empty

    private ItemDefinition item;
    private CauldronUI owner;

    public bool IsEmpty => item == null;
    public ItemDefinition Item => item;

    public void Setup(CauldronUI ui)
    {
        owner = ui;
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(OnRemoveClicked);
        Clear();
    }

    public void Fill(ItemDefinition itemDef, int count)
    {
        item = itemDef;
        filledGroup.SetActive(true);
        iconImage.sprite = itemDef.icon;
        iconImage.preserveAspect = true;
        iconImage.enabled = itemDef.icon != null;
        quantityText.text = count > 1 ? "x " + count : "";
        removeButton.gameObject.SetActive(false);
    }

    public void Clear()
    {
        item = null;
        filledGroup.SetActive(false);
        removeButton.gameObject.SetActive(false);
    }

    private void OnRemoveClicked()
    {
        if (owner != null && item != null)
            owner.RemoveIngredient(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsEmpty)
            removeButton.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        removeButton.gameObject.SetActive(false);
    }
}
