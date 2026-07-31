using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: Displays the 4 quick-slots on the right page of the minimised grimoire,
// arranged in a plus shape. Matches the CauldronSlot visual pattern: square background,
// icon image with preserveAspect, quantity in the lower-right corner.
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
        [Tooltip("Always-visible slot number label (1-4).")]
        public TMP_Text numberLabel;
        [Tooltip("Parent of icon + quantity. Hidden when the slot is empty.")]
        public GameObject filledGroup;
        [Tooltip("The item/spell icon. Uses Image with preserveAspect.")]
        public Image icon;
        [Tooltip("Quantity text in the lower-right corner. Shown for consumables/throwables.")]
        public TMP_Text quantityText;
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

        if (display.numberLabel != null)
            display.numberLabel.SetText((index + 1).ToString());

        QuickSlotManager.QuickSlot slot = quickSlotManager != null
            ? quickSlotManager.GetSlot(index)
            : null;

        if (slot == null || slot.IsEmpty)
        {
            if (display.filledGroup != null)
                display.filledGroup.SetActive(false);
            return;
        }

        if (slot.item != null)
        {
            SetIcon(display, slot.item.icon);
            SetQuantity(display, slot.item);
        }
        else if (slot.spell != null)
        {
            SetIcon(display, slot.spell.icon);
            HideQuantity(display);
        }

        if (display.filledGroup != null)
            display.filledGroup.SetActive(true);
    }

    private void SetIcon(SlotDisplay display, Sprite icon)
    {
        if (display.icon == null) return;

        if (icon != null)
        {
            display.icon.sprite = icon;
            display.icon.preserveAspect = true;
            display.icon.enabled = true;
        }
        else
        {
            display.icon.enabled = false;
        }
    }

    private void SetQuantity(SlotDisplay display, ItemDefinition item)
    {
        if (display.quantityText == null) return;

        bool showQuantity = item.tags.HasFlag(ItemTag.Consumable) || item.tags.HasFlag(ItemTag.Throwable);

        if (showQuantity && inventory != null)
        {
            int count = inventory.GetCount(item);
            display.quantityText.SetText("x " + count);
            display.quantityText.gameObject.SetActive(true);
        }
        else
        {
            display.quantityText.gameObject.SetActive(false);
        }
    }

    private void HideQuantity(SlotDisplay display)
    {
        if (display.quantityText != null)
            display.quantityText.gameObject.SetActive(false);
    }
}