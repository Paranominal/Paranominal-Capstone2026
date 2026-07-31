using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Spells tab of the Full Grimoire. Displays learned spells from SpellBook.
// Selected spells show their detail on the right page.
// Press 1-4 to assign the selected spell to a quick-slot.
// Shares the ScrollView Content and DetailView with other panels.
public class GrimoireSpellsPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    private SpellBook spellBook;
    private QuickSlotManager quickSlotManager;
    private List<SpellDefinition> currentSpells = new List<SpellDefinition>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();
    private int selectedIndex = -1;

    private InputAction slot1Action;
    private InputAction slot2Action;
    private InputAction slot3Action;
    private InputAction slot4Action;

    private void Awake()
    {
        if (spellBook == null)
            spellBook = FindAnyObjectByType<SpellBook>();
        if (quickSlotManager == null)
            quickSlotManager = FindAnyObjectByType<QuickSlotManager>();

        // EDIT (input): find actions from the GrimoireUI map specifically.
        var grimoireMap = InputSystem.actions.FindActionMap("GrimoireUI");
        slot1Action = grimoireMap?.FindAction("QuickSlot1");
        slot2Action = grimoireMap?.FindAction("QuickSlot2");
        slot3Action = grimoireMap?.FindAction("QuickSlot3");
        slot4Action = grimoireMap?.FindAction("QuickSlot4");
    }

    private void OnEnable()
    {
        if (spellBook != null)
            spellBook.OnSpellLearned += OnSpellLearned;

        Rebuild();
    }

    private void OnDisable()
    {
        if (spellBook != null)
            spellBook.OnSpellLearned -= OnSpellLearned;

        ClearList();
    }

    private void Update()
    {
        if (selectedIndex < 0 || selectedIndex >= currentSpells.Count) return;

        if (slot1Action != null && slot1Action.WasPressedThisFrame()) AssignToSlot(0);
        else if (slot2Action != null && slot2Action.WasPressedThisFrame()) AssignToSlot(1);
        else if (slot3Action != null && slot3Action.WasPressedThisFrame()) AssignToSlot(2);
        else if (slot4Action != null && slot4Action.WasPressedThisFrame()) AssignToSlot(3);
    }

    private void OnSpellLearned(SpellDefinition spell)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        ClearList();

        if (spellBook == null) return;

        currentSpells = spellBook.GetLearnedSpells();

        for (int i = 0; i < currentSpells.Count; i++)
        {
            int index = i;
            SpellDefinition spell = currentSpells[i];

            GameObject entryObj = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObj.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                entry.Setup(index, spell.displayName, SelectEntry);
                UpdateSlotBadge(entry, spell);
                entryButtons.Add(entry);
            }
        }

        if (currentSpells.Count > 0)
            SelectEntry(0);
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, currentSpells.Count - 1);

        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == selectedIndex);

        if (detailView != null && selectedIndex < currentSpells.Count)
        {
            SpellDefinition spell = currentSpells[selectedIndex];
            detailView.SetDetail(
                spell.displayName,
                spell.description,
                spell.flavourText,
                spell.hintText,
                spell.icon
            );
        }
    }

    private void AssignToSlot(int slotIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= currentSpells.Count) return;
        if (quickSlotManager == null) return;

        SpellDefinition spell = currentSpells[selectedIndex];
        QuickSlotManager.QuickSlot existing = quickSlotManager.GetSlot(slotIndex);

        if (existing != null && existing.spell == spell)
            quickSlotManager.ClearSlot(slotIndex);
        else
            quickSlotManager.AssignSpell(slotIndex, spell);

        RefreshAllBadges();
    }

    private void RefreshAllBadges()
    {
        for (int i = 0; i < entryButtons.Count && i < currentSpells.Count; i++)
            UpdateSlotBadge(entryButtons[i], currentSpells[i]);
    }

    private void UpdateSlotBadge(GrimoireEntryButton entry, SpellDefinition spell)
    {
        if (quickSlotManager == null)
        {
            entry.SetSlotBadge(-1);
            return;
        }

        int assignedSlot = -1;
        for (int s = 0; s < QuickSlotManager.SlotCount; s++)
        {
            QuickSlotManager.QuickSlot slot = quickSlotManager.GetSlot(s);
            if (slot != null && slot.spell == spell)
            {
                assignedSlot = s;
                break;
            }
        }
        entry.SetSlotBadge(assignedSlot);
    }

    private void ClearList()
    {
        if (listParent != null)
        {
            foreach (Transform child in listParent)
                Destroy(child.gameObject);
        }
        entryButtons.Clear();
        currentSpells.Clear();
        selectedIndex = -1;
    }
}