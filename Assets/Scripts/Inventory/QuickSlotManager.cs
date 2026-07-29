using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Manages 4 quick-slots that can hold either an ItemDefinition or a SpellDefinition.
// Handles 1-4 key input for using the slotted item/spell, and fires dispatch events
// for external systems to handle the actual effects (ConsumableHandler, ThrowableHandler,
// casting system, etc.).
// Lives on the player alongside Inventory.
public class QuickSlotManager : MonoBehaviour
{
    public const int SlotCount = 4;

    [Header("References")]
    [SerializeField] private Inventory inventory;

    [Header("Input")]
    [SerializeField] private string slot1ActionName = "QuickSlot1";
    [SerializeField] private string slot2ActionName = "QuickSlot2";
    [SerializeField] private string slot3ActionName = "QuickSlot3";
    [SerializeField] private string slot4ActionName = "QuickSlot4";

    // Summary: Holds the contents of a single quick-slot. Only one of item/spell
    // should be non-null at a time.
    [Serializable]
    public class QuickSlot
    {
        public ItemDefinition item;
        public SpellDefinition spell;

        public bool IsEmpty => item == null && spell == null;

        public void Clear()
        {
            item = null;
            spell = null;
        }
    }

    private QuickSlot[] slots = new QuickSlot[SlotCount];
    private InputAction[] slotActions = new InputAction[SlotCount];

    // Summary: Fired when the contents of a slot change. Index is 0-3.
    public event Action<int, QuickSlot> OnSlotChanged;

    // Summary: Fired when the player uses a slot containing an item.
    // External systems (ConsumableHandler, ThrowableHandler, etc.) subscribe
    // and handle the actual effect based on ItemTag.
    public event Action<int, ItemDefinition> OnItemUsed;

    // Summary: Fired when the player uses a slot containing a spell.
    // The casting system (once built) subscribes to handle the cast.
    public event Action<int, SpellDefinition> OnSpellUsed;

    private void Awake()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = new QuickSlot();

        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        string[] actionNames = { slot1ActionName, slot2ActionName, slot3ActionName, slot4ActionName };
        for (int i = 0; i < SlotCount; i++)
            slotActions[i] = InputSystem.actions.FindAction(actionNames[i]);
    }

    private void Update()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slotActions[i] != null && slotActions[i].WasPressedThisFrame())
            {
                UseSlot(i);
                break;
            }
        }
    }

    // ---- Assignment ----

    // Summary: Assign an item to a slot. Clears any existing contents.
    public void AssignItem(int index, ItemDefinition item)
    {
        if (!ValidIndex(index)) return;

        slots[index].Clear();
        slots[index].item = item;
        OnSlotChanged?.Invoke(index, slots[index]);
    }

    // Summary: Assign a spell to a slot. Clears any existing contents.
    public void AssignSpell(int index, SpellDefinition spell)
    {
        if (!ValidIndex(index)) return;

        slots[index].Clear();
        slots[index].spell = spell;
        OnSlotChanged?.Invoke(index, slots[index]);
    }

    // Summary: Clear a slot.
    public void ClearSlot(int index)
    {
        if (!ValidIndex(index)) return;

        slots[index].Clear();
        OnSlotChanged?.Invoke(index, slots[index]);
    }

    // Summary: Get the contents of a slot. Returns null if index is out of range.
    public QuickSlot GetSlot(int index)
    {
        return ValidIndex(index) ? slots[index] : null;
    }

    // ---- Use ----

    // Summary: Use the item/spell in the given slot. Validates availability before firing.
    public void UseSlot(int index)
    {
        if (!ValidIndex(index)) return;

        QuickSlot slot = slots[index];
        if (slot.IsEmpty) return;

        if (slot.item != null)
        {
            if (!CanUseItem(slot.item)) return;
            OnItemUsed?.Invoke(index, slot.item);
        }
        else if (slot.spell != null)
        {
            OnSpellUsed?.Invoke(index, slot.spell);
        }
    }

    // ---- Queries ----

    // Summary: Returns the quantity available for an item slot, or -1 for spells
    // (spells are gated by spirit cost, not inventory count).
    public int GetSlotQuantity(int index)
    {
        if (!ValidIndex(index)) return 0;

        QuickSlot slot = slots[index];
        if (slot.item != null && inventory != null)
            return inventory.GetCount(slot.item);
        if (slot.spell != null)
            return -1;

        return 0;
    }

    // Summary: Returns true if the item is in inventory and available to use.
    private bool CanUseItem(ItemDefinition item)
    {
        return inventory != null && inventory.Has(item);
    }

    private bool ValidIndex(int index)
    {
        return index >= 0 && index < SlotCount;
    }
}
