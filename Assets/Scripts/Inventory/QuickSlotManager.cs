using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Manages 4 quick-slots that can hold any GameDefinition (items or spells).
// Handles 1-4 key input for using the slotted definition, and fires typed dispatch
// events for external systems (ConsumableHandler, ThrowableHandler, casting system, etc.).
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

    // Summary: Holds the contents of a single quick-slot. Stores a single
    // GameDefinition reference; use pattern matching to determine the type.
    [Serializable]
    public class QuickSlot
    {
        public GameDefinition definition;

        public bool IsEmpty => definition == null;

        public void Clear()
        {
            definition = null;
        }
    }

    private QuickSlot[] slots = new QuickSlot[SlotCount];
    private InputAction[] slotActions = new InputAction[SlotCount];

    // Summary: Fired when the contents of a slot change. Index is 0-3.
    public event Action<int, QuickSlot> OnSlotChanged;

    // Summary: Fired when the player uses a slot containing an item.
    // External systems (ConsumableHandler, ThrowableHandler, etc.) subscribe.
    public event Action<int, ItemDefinition> OnItemUsed;

    // Summary: Fired when the player uses a slot containing a spell.
    // The casting system subscribes to handle the cast.
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

    // Summary: Assign a definition (item or spell) to a slot. Clears any existing
    // contents, and removes the definition from any other slot it was previously in.
    public void Assign(int index, GameDefinition def)
    {
        if (!ValidIndex(index) || def == null) return;

        // Remove from any other slot first.
        for (int i = 0; i < SlotCount; i++)
        {
            if (i == index) continue;
            if (slots[i].definition == def)
            {
                slots[i].Clear();
                OnSlotChanged?.Invoke(i, slots[i]);
            }
        }

        slots[index].Clear();
        slots[index].definition = def;
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

    // Summary: Use the definition in the given slot. Validates availability before
    // firing the appropriate typed event.
    public void UseSlot(int index)
    {
        if (!ValidIndex(index)) return;

        QuickSlot slot = slots[index];
        if (slot.IsEmpty) return;

        if (slot.definition is ItemDefinition item)
        {
            if (!CanUseItem(item)) return;
            OnItemUsed?.Invoke(index, item);
        }
        else if (slot.definition is SpellDefinition spell)
        {
            OnSpellUsed?.Invoke(index, spell);
        }
    }

    // ---- Queries ----

    // Summary: Returns the quantity available for an item slot, or -1 for spells
    // (spells are gated by spirit cost, not inventory count).
    public int GetSlotQuantity(int index)
    {
        if (!ValidIndex(index)) return 0;

        QuickSlot slot = slots[index];
        if (slot.definition is ItemDefinition item && inventory != null)
            return inventory.GetCount(item);
        if (slot.definition is SpellDefinition)
            return -1;

        return 0;
    }

    private bool CanUseItem(ItemDefinition item)
    {
        return inventory != null && inventory.Has(item);
    }

    private bool ValidIndex(int index)
    {
        return index >= 0 && index < SlotCount;
    }
}