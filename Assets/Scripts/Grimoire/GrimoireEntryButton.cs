using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: Individual entry in a grimoire list panel. Displays the item/spell name,
// handles selection highlighting, and shows a 1-4 slot badge when assigned to a quick-slot.
// Instantiated from a prefab by the panel scripts.
public class GrimoireEntryButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text slotBadge;
    [SerializeField] private Button button;
    [SerializeField] private Image background;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.15f);

    private int entryIndex;
    private System.Action<int> onSelected;

    // Summary: Initialize the entry button with its display name and selection callback.
    public void Setup(int index, string displayName, System.Action<int> selectCallback)
    {
        entryIndex = index;
        onSelected = selectCallback;

        if (nameLabel != null)
            nameLabel.SetText(displayName);

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(entryIndex));
        }

        SetSlotBadge(-1);
    }

    // Summary: Show or hide the quick-slot assignment badge.
    // Pass -1 to hide, 0-3 for slot numbers (displayed as 1-4).
    public void SetSlotBadge(int slotIndex)
    {
        if (slotBadge == null) return;

        if (slotIndex >= 0)
        {
            slotBadge.gameObject.SetActive(true);
            slotBadge.SetText((slotIndex + 1).ToString());
        }
        else
        {
            slotBadge.gameObject.SetActive(false);
        }
    }

    // Summary: Visual highlight for the currently selected entry.
    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}
