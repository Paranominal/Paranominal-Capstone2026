using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: Displays the 4 quick-slots on the right page of the minimised grimoire.
// Each slot shows its number, item/spell icon, and quantity (for consumables/throwables).
// Subscribes to QuickSlotManager.OnSlotChanged for updates.
public class MinimisedQuickSlotsUI : MonoBehaviour
{
    [Header("Slot Displays")]
    [SerializeField] private SlotDisplay[] slots = new SlotDisplay[4];

    private QuickSlotManager quickSlotManager;
    private Inventory inventory;

    [System.Serializable]
    public class SlotDisplay
    {
        [Tooltip("The slot number label (displays 1-4).")]
        public TMP_Text numberLabel;
        [Tooltip("The item/spell icon.")]
        public RawImage icon;
        [Tooltip("The quantity label. Hidden for spells and empty slots.")]
        public TMP_Text quantityLabel;
        [Tooltip("Container for the entire slot. Dimmed when empty.")]
        public CanvasGroup canvasGroup;
    }

    private void Awake()
    {
        if (quickSlotManager == null)
            quickSlotManager = FindAnyObjectByType<QuickSlotManager>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
    }

    private void OnEnable()
    {
        if (quickSlotManager != null)
            quickSlotManager.OnSlotChanged += OnSlotChanged;
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (quickSlotManager != null)
            quickSlotManager.OnSlotChanged -= OnSlotChanged;
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshAll;
    }

    private void OnSlotChanged(int index, QuickSlotManager.QuickSlot slot)
    {
        RefreshSlot(index);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < QuickSlotManager.SlotCount && i < slots.Length; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        SlotDisplay display = slots[index];
        if (display == null) return;

        // Always show the slot number.
        if (display.numberLabel != null)
            display.numberLabel.SetText((index + 1).ToString());

        QuickSlotManager.QuickSlot slot = quickSlotManager != null
            ? quickSlotManager.GetSlot(index)
            : null;

        if (slot == null || slot.IsEmpty)
        {
            SetEmpty(display);
            return;
        }

        if (slot.item != null)
        {
            SetIcon(display, slot.item.icon);
            SetQuantity(display, slot.item);
            SetFilled(display);
        }
        else if (slot.spell != null)
        {
            SetIcon(display, slot.spell.icon);
            HideQuantity(display);
            SetFilled(display);
        }
    }

    private void SetIcon(SlotDisplay display, Sprite icon)
    {
        if (display.icon == null) return;

        if (icon != null)
        {
            display.icon.texture = icon.texture;
            display.icon.gameObject.SetActive(true);
        }
        else
        {
            display.icon.gameObject.SetActive(false);
        }
    }

    private void SetQuantity(SlotDisplay display, ItemDefinition item)
    {
        if (display.quantityLabel == null) return;

        bool showQuantity = item.tags.HasFlag(ItemTag.Consumable) || item.tags.HasFlag(ItemTag.Throwable);

        if (showQuantity && inventory != null)
        {
            int count = inventory.GetCount(item);
            display.quantityLabel.SetText(count.ToString());
            display.quantityLabel.gameObject.SetActive(true);
        }
        else
        {
            display.quantityLabel.gameObject.SetActive(false);
        }
    }

    private void HideQuantity(SlotDisplay display)
    {
        if (display.quantityLabel != null)
            display.quantityLabel.gameObject.SetActive(false);
    }

    private void SetFilled(SlotDisplay display)
    {
        if (display.canvasGroup != null)
            display.canvasGroup.alpha = 1f;
    }

    private void SetEmpty(SlotDisplay display)
    {
        if (display.icon != null)
            display.icon.gameObject.SetActive(false);
        HideQuantity(display);

        if (display.canvasGroup != null)
            display.canvasGroup.alpha = 0.3f;
    }
}
