using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: A single entry in a Full Interface list (Inventory, Bestiary). Displays a label, handles selection highlighting,
// and reports clicks back to its panel. Instantiated from a prefab by the panel scripts.
public class GrimoireEntryButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Button button;
    [SerializeField] private Image background;

    [Header("Colours")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.15f);

    private int entryIndex;
    private System.Action<int> onSelected;

    // Summary: Sets the label and the callback fired with this entry's index when clicked.
    public void Setup(int index, string label, System.Action<int> selectCallback)
    {
        entryIndex = index;
        onSelected = selectCallback;

        if (nameLabel != null)
            nameLabel.SetText(label);

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(entryIndex));
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}
