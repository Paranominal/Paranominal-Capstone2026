using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Summary: Inventory tab of the Full Grimoire. Displays held items filtered by
// ItemTag sub-tabs. Selected items show their detail on the right page.
// Press 1-4 to assign the selected item to a quick-slot.
// Shares the ScrollView Content and DetailView with other panels.
public class GrimoireInventoryPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("Sub-Tab Buttons (top of BookL, only visible on this tab)")]
    [SerializeField] private GameObject subTabContainer;
    [SerializeField] private Button allButton;
    [SerializeField] private Button consumableButton;
    [SerializeField] private Button ingredientButton;
    [SerializeField] private Button keyItemButton;
    [SerializeField] private Button throwableButton;

    private Inventory inventory;
    private QuickSlotManager quickSlotManager;
    private List<ItemDefinition> currentItems = new List<ItemDefinition>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();
    private int selectedIndex = -1;
    private ItemTag currentFilter = ItemTag.None;

    private InputAction slot1Action;
    private InputAction slot2Action;
    private InputAction slot3Action;
    private InputAction slot4Action;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (quickSlotManager == null)
            quickSlotManager = FindAnyObjectByType<QuickSlotManager>();

        // EDIT (input): find actions from the GrimoireUI map specifically, since the
        // same action names exist in the Player map for gameplay use.
        var grimoireMap = InputSystem.actions.FindActionMap("GrimoireUI");
        slot1Action = grimoireMap?.FindAction("QuickSlot1");
        slot2Action = grimoireMap?.FindAction("QuickSlot2");
        slot3Action = grimoireMap?.FindAction("QuickSlot3");
        slot4Action = grimoireMap?.FindAction("QuickSlot4");

        SetupSubTabs();
    }

    private void OnEnable()
    {
        if (subTabContainer != null)
            subTabContainer.SetActive(true);

        if (inventory != null)
            inventory.OnInventoryChanged += Rebuild;

        Rebuild();
    }

    private void OnDisable()
    {
        if (subTabContainer != null)
            subTabContainer.SetActive(false);

        if (inventory != null)
            inventory.OnInventoryChanged -= Rebuild;

        ClearList();
    }

    private void Update()
    {
        if (selectedIndex < 0 || selectedIndex >= currentItems.Count) return;

        if (slot1Action != null && slot1Action.WasPressedThisFrame()) AssignToSlot(0);
        else if (slot2Action != null && slot2Action.WasPressedThisFrame()) AssignToSlot(1);
        else if (slot3Action != null && slot3Action.WasPressedThisFrame()) AssignToSlot(2);
        else if (slot4Action != null && slot4Action.WasPressedThisFrame()) AssignToSlot(3);
    }

    private void SetupSubTabs()
    {
        if (allButton != null)
            allButton.onClick.AddListener(() => SetFilter(ItemTag.None));
        if (consumableButton != null)
            consumableButton.onClick.AddListener(() => SetFilter(ItemTag.Consumable));
        if (ingredientButton != null)
            ingredientButton.onClick.AddListener(() => SetFilter(ItemTag.Ingredient));
        if (keyItemButton != null)
            keyItemButton.onClick.AddListener(() => SetFilter(ItemTag.KeyItem));
        if (throwableButton != null)
            throwableButton.onClick.AddListener(() => SetFilter(ItemTag.Throwable));
    }

    private void SetFilter(ItemTag filter)
    {
        currentFilter = filter;
        Rebuild();
    }

    private void Rebuild()
    {
        ClearList();

        if (inventory == null) return;

        if (currentFilter == ItemTag.None)
            currentItems = inventory.GetAllItems();
        else
            currentItems = inventory.GetItems(currentFilter);

        // Filter out weapons since they have their own management system.
        currentItems.RemoveAll(item => item is WeaponDefinition);

        for (int i = 0; i < currentItems.Count; i++)
        {
            int index = i;
            ItemDefinition item = currentItems[i];

            GameObject entryObj = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObj.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                string countSuffix = inventory.GetCount(item) > 1
                    ? $" ({inventory.GetCount(item)})"
                    : "";
                entry.Setup(index, item.displayName + countSuffix, SelectEntry);
                UpdateSlotBadge(entry, item);
                entryButtons.Add(entry);
            }
        }

        if (currentItems.Count > 0)
            SelectEntry(0);
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, currentItems.Count - 1);

        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == selectedIndex);

        if (detailView != null && selectedIndex < currentItems.Count)
        {
            ItemDefinition item = currentItems[selectedIndex];
            detailView.SetDetail(
                item.displayName,
                item.description,
                item.flavourText,
                item.hintText,
                item.icon
            );
        }
    }

    private void AssignToSlot(int slotIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= currentItems.Count) return;
        if (quickSlotManager == null) return;

        ItemDefinition item = currentItems[selectedIndex];
        QuickSlotManager.QuickSlot existing = quickSlotManager.GetSlot(slotIndex);

        // Toggle: same item in same slot clears it.
        if (existing != null && existing.item == item)
            quickSlotManager.ClearSlot(slotIndex);
        else
            quickSlotManager.AssignItem(slotIndex, item);

        RefreshAllBadges();
    }

    private void RefreshAllBadges()
    {
        for (int i = 0; i < entryButtons.Count && i < currentItems.Count; i++)
            UpdateSlotBadge(entryButtons[i], currentItems[i]);
    }

    private void UpdateSlotBadge(GrimoireEntryButton entry, ItemDefinition item)
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
            if (slot != null && slot.item == item)
            {
                assignedSlot = s;
                break;
            }
        }
        entry.SetSlotBadge(assignedSlot);
    }

    private void ClearList()
    {
        // Destroy all children of the shared list parent for a clean slate.
        if (listParent != null)
        {
            foreach (Transform child in listParent)
                Destroy(child.gameObject);
        }
        entryButtons.Clear();
        currentItems.Clear();
        selectedIndex = -1;
    }
}