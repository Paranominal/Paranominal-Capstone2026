using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Weapons tab of the Full Grimoire. Displays all weapon-tagged items
// from Inventory. Press 1 or 2 to assign the selected weapon to a loadout slot.
// Only loadout weapons appear in the scroll rotation during gameplay.
// Shares the ScrollView Content and DetailView with other panels.
public class GrimoireWeaponsPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    private Inventory inventory;
    private WeaponManager weaponManager;
    private List<WeaponDefinition> currentWeapons = new List<WeaponDefinition>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();
    private int selectedIndex = -1;

    private InputAction slot1Action;
    private InputAction slot2Action;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (weaponManager == null)
            weaponManager = FindAnyObjectByType<WeaponManager>();

        // EDIT (input): find actions from the GrimoireUI map specifically.
        var grimoireMap = InputSystem.actions.FindActionMap("GrimoireUI");
        slot1Action = grimoireMap?.FindAction("QuickSlot1");
        slot2Action = grimoireMap?.FindAction("QuickSlot2");
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += Rebuild;
        if (weaponManager != null)
            weaponManager.OnLoadoutChanged += RefreshAllBadges;

        Rebuild();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Rebuild;
        if (weaponManager != null)
            weaponManager.OnLoadoutChanged -= RefreshAllBadges;

        ClearList();
    }

    private void Update()
    {
        if (selectedIndex < 0 || selectedIndex >= currentWeapons.Count) return;

        if (slot1Action != null && slot1Action.WasPressedThisFrame()) AssignToLoadout(0);
        else if (slot2Action != null && slot2Action.WasPressedThisFrame()) AssignToLoadout(1);
    }

    private void Rebuild()
    {
        ClearList();

        if (inventory == null) return;

        List<ItemDefinition> weapons = inventory.GetItems(ItemTag.Weapon);

        foreach (ItemDefinition item in weapons)
        {
            WeaponDefinition weaponDef = item as WeaponDefinition;
            if (weaponDef != null)
                currentWeapons.Add(weaponDef);
        }

        for (int i = 0; i < currentWeapons.Count; i++)
        {
            int index = i;
            WeaponDefinition weapon = currentWeapons[i];

            GameObject entryObj = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObj.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                entry.Setup(index, weapon.displayName, SelectEntry);
                UpdateSlotBadge(entry, weapon);
                entryButtons.Add(entry);
            }
        }

        if (currentWeapons.Count > 0)
            SelectEntry(0);
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, currentWeapons.Count - 1);

        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == selectedIndex);

        if (detailView != null && selectedIndex < currentWeapons.Count)
        {
            WeaponDefinition weapon = currentWeapons[selectedIndex];
            detailView.SetDetail(
                weapon.displayName,
                weapon.description,
                weapon.flavourText,
                weapon.hintText,
                weapon.icon
            );
        }
    }

    private void AssignToLoadout(int slotIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= currentWeapons.Count) return;
        if (weaponManager == null) return;

        WeaponDefinition weapon = currentWeapons[selectedIndex];

        // Toggle: same weapon in same slot clears it.
        if (weaponManager.GetLoadoutSlot(slotIndex) == weapon)
            weaponManager.ClearLoadoutSlot(slotIndex);
        else
            weaponManager.AssignToLoadout(slotIndex, weapon);

        RefreshAllBadges();
    }

    private void RefreshAllBadges()
    {
        for (int i = 0; i < entryButtons.Count && i < currentWeapons.Count; i++)
            UpdateSlotBadge(entryButtons[i], currentWeapons[i]);
    }

    private void UpdateSlotBadge(GrimoireEntryButton entry, WeaponDefinition weapon)
    {
        if (weaponManager == null)
        {
            entry.SetSlotBadge(-1);
            return;
        }

        int assignedSlot = -1;
        for (int s = 0; s < WeaponManager.LoadoutSize; s++)
        {
            if (weaponManager.GetLoadoutSlot(s) == weapon)
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
        currentWeapons.Clear();
        selectedIndex = -1;
    }
}