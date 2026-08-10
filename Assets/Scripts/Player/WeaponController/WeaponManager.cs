using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Manages weapon equipping, loadout, mode switching, and input routing.
// All weapons entering inventory are instantiated and tracked. Only weapons assigned
// to the 2-slot loadout are available in the scroll rotation during gameplay.
// Q toggles empty hand. Scroll cycles loadout weapons.
// EDIT (loadout system): scroll rotation now driven by loadout, not the full weapons list.
// Auto-assigns to empty loadout slots on pickup. Validates loadout on grimoire close.
public class WeaponManager : MonoBehaviour
{
    public const int LoadoutSize = 2;

    [Header("References")]
    [SerializeField] private Transform weaponParent;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private MeleeController meleeController;
    [SerializeField] private WeaponSwitchAnimator switchAnimator;
    [SerializeField] private GameObject defaultHand;

    [Header("Input")]
    [SerializeField] private string holsterActionName = "Holster";
    [SerializeField] private string scrollActionName = "ScrollWeapon";

    // Summary: Fires when the weapon mode changes. true = weapon equipped, false = empty hand.
    // GrimoireAnimManager and ScanController subscribe to this.
    // Does not fire during the quick shove sequence (not a real mode change).
    public static event Action<bool> OnWeaponModeChanged;

    // Summary: Fires when the loadout changes. GrimoireWeaponsPanel subscribes for badge refresh.
    public event Action OnLoadoutChanged;

    private Inventory inventory;
    private InputAction holsterAction;
    private InputAction scrollAction;

    private class EquippedWeapon
    {
        public WeaponDefinition definition;
        public GameObject instance;
        public GunVisuals gunVisuals;
        public int cachedAmmo = -1;
    }

    // All instantiated weapons, whether loadout-assigned or not.
    private List<EquippedWeapon> allWeapons = new List<EquippedWeapon>();

    // The 2-slot loadout. Null entries = empty slot. Scroll only cycles these.
    private EquippedWeapon[] loadout = new EquippedWeapon[LoadoutSize];

    // Index into the loadout array (not allWeapons).
    private int activeLoadoutIndex = -1;
    private bool isEmptyHand = true;
    private bool isTransitioning;

    void Awake()
    {
        if (weaponController == null)
            weaponController = FindAnyObjectByType<WeaponController>();
        if (meleeController == null)
            meleeController = FindAnyObjectByType<MeleeController>();
        if (switchAnimator == null)
            switchAnimator = GetComponentInChildren<WeaponSwitchAnimator>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        holsterAction = InputSystem.actions.FindAction(holsterActionName);
        scrollAction = InputSystem.actions.FindAction(scrollActionName);
    }

    void Start()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += OnInventoryChanged;

        ALTGrimoire grimoire = FindAnyObjectByType<ALTGrimoire>();
        if (grimoire != null)
            grimoire.OnGrimoireToggled += OnGrimoireToggled;

        EnterEmptyHand(animated: false);
        OnInventoryChanged();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;

        ALTGrimoire grimoire = FindAnyObjectByType<ALTGrimoire>();
        if (grimoire != null)
            grimoire.OnGrimoireToggled -= OnGrimoireToggled;
    }

    void Update()
    {
        if (isTransitioning) return;

        // Q: toggle holster.
        if (holsterAction != null && holsterAction.WasPressedThisFrame())
        {
            if (isEmptyHand)
            {
                int index = FindFirstLoadoutWeapon();
                if (index >= 0)
                    StartCoroutine(SwitchToLoadoutSlot(index));
            }
            else
            {
                StartCoroutine(SwitchToEmptyHand());
            }
        }

        // Scroll: cycle through loadout weapons.
        if (scrollAction != null)
        {
            float scrollValue = scrollAction.ReadValue<float>();
            if (scrollValue > 0f)
                CycleWeapon(1);
            else if (scrollValue < 0f)
                CycleWeapon(-1);
        }
    }

    // ---- Loadout API (called by GrimoireWeaponsPanel) ----

    // Summary: Assign a weapon to a loadout slot. If the weapon is already in another slot,
    // it moves to the new one.
    public void AssignToLoadout(int slotIndex, WeaponDefinition weaponDef)
    {
        if (slotIndex < 0 || slotIndex >= LoadoutSize) return;

        EquippedWeapon equipped = FindEquipped(weaponDef);
        if (equipped == null) return;

        // Remove from any other slot first.
        for (int i = 0; i < LoadoutSize; i++)
        {
            if (loadout[i] == equipped)
                loadout[i] = null;
        }

        loadout[slotIndex] = equipped;
        OnLoadoutChanged?.Invoke();
    }

    // Summary: Clear a loadout slot.
    public void ClearLoadoutSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= LoadoutSize) return;

        loadout[slotIndex] = null;
        OnLoadoutChanged?.Invoke();
    }

    // Summary: Get the weapon definition in a loadout slot, or null if empty.
    public WeaponDefinition GetLoadoutSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= LoadoutSize) return null;
        return loadout[slotIndex]?.definition;
    }

    // ---- Inventory ----

    private void OnInventoryChanged()
    {
        if (inventory == null) return;

        List<ItemDefinition> weapons = inventory.GetItems(ItemTag.Weapon);

        foreach (ItemDefinition item in weapons)
        {
            WeaponDefinition weaponDef = item as WeaponDefinition;
            if (weaponDef == null) continue;
            if (FindEquipped(weaponDef) != null) continue;

            EquippedWeapon equipped = InstantiateWeapon(weaponDef);
            if (equipped == null) continue;

            allWeapons.Add(equipped);

            // Auto-assign to the first empty loadout slot.
            int emptySlot = FindEmptyLoadoutSlot();
            if (emptySlot >= 0)
            {
                loadout[emptySlot] = equipped;
                OnLoadoutChanged?.Invoke();

                // Auto-equip if currently in empty hand.
                if (isEmptyHand && !isTransitioning)
                    StartCoroutine(SwitchToLoadoutSlot(emptySlot));
            }
        }
    }

    private EquippedWeapon InstantiateWeapon(WeaponDefinition weaponDef)
    {
        if (weaponDef.equippedPrefab == null)
        {
            Debug.Log("WeaponManager: registered " + weaponDef.displayName + " (no prefab)");
            return new EquippedWeapon
            {
                definition = weaponDef,
                instance = null,
                gunVisuals = null,
            };
        }

        if (weaponParent == null)
        {
            Debug.LogWarning("WeaponManager: no weaponParent assigned.");
            return null;
        }

        GameObject instance = Instantiate(weaponDef.equippedPrefab, weaponParent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.SetActive(false);

        GunVisuals visuals = instance.GetComponentInChildren<GunVisuals>();

        Debug.Log("WeaponManager: instantiated " + weaponDef.displayName);
        return new EquippedWeapon
        {
            definition = weaponDef,
            instance = instance,
            gunVisuals = visuals,
        };
    }

    // ---- Grimoire Close Validation ----

    // Summary: On grimoire close, check if the currently held weapon was removed from
    // the loadout. If so, animate a switch to the next available loadout weapon or empty hand.
    private void OnGrimoireToggled(bool grimoireOpen)
    {
        if (grimoireOpen) return;
        if (isEmptyHand) return;

        EquippedWeapon active = GetActiveEquippedWeapon();
        if (active != null && IsInLoadout(active)) return;

        // Active weapon was removed from loadout. Switch away.
        int nextSlot = FindFirstLoadoutWeapon();
        if (nextSlot >= 0)
            StartCoroutine(SwitchToLoadoutSlot(nextSlot));
        else
            StartCoroutine(SwitchToEmptyHand());
    }

    // ---- Animated Transitions ----

    private IEnumerator SwitchToLoadoutSlot(int loadoutIndex)
    {
        if (loadoutIndex < 0 || loadoutIndex >= LoadoutSize) yield break;
        if (loadout[loadoutIndex] == null) yield break;

        isTransitioning = true;

        bool lowerDone = false;
        if (switchAnimator != null)
        {
            switchAnimator.Lower(() => lowerDone = true);
            while (!lowerDone) yield return null;
        }

        DeactivateCurrentVisuals();
        if (defaultHand != null) defaultHand.SetActive(false);

        activeLoadoutIndex = loadoutIndex;
        isEmptyHand = false;
        EquippedWeapon weapon = loadout[activeLoadoutIndex];

        if (weapon.instance != null) weapon.instance.SetActive(true);
        RouteToController(weapon);

        bool raiseDone = false;
        if (switchAnimator != null)
        {
            switchAnimator.Raise(() => raiseDone = true);
            while (!raiseDone) yield return null;
        }

        isTransitioning = false;
        OnWeaponModeChanged?.Invoke(true);
        Debug.Log("Equipped weapon: " + weapon.definition.displayName);
    }

    private IEnumerator SwitchToEmptyHand()
    {
        isTransitioning = true;

        bool lowerDone = false;
        if (switchAnimator != null)
        {
            switchAnimator.Lower(() => lowerDone = true);
            while (!lowerDone) yield return null;
        }

        DeactivateCurrentVisuals();
        EnterEmptyHandState();

        bool raiseDone = false;
        if (switchAnimator != null)
        {
            switchAnimator.Raise(() => raiseDone = true);
            while (!raiseDone) yield return null;
        }

        isTransitioning = false;
        OnWeaponModeChanged?.Invoke(false);
        Debug.Log("Entered empty hand mode.");
    }

    // ---- State Helpers ----

    private void EnterEmptyHand(bool animated = true)
    {
        DeactivateCurrentVisuals();
        EnterEmptyHandState();

        if (!animated && switchAnimator != null)
            switchAnimator.SnapToRest();

        OnWeaponModeChanged?.Invoke(false);
    }

    private void EnterEmptyHandState()
    {
        isEmptyHand = true;

        if (defaultHand != null) defaultHand.SetActive(true);

        if (weaponController != null)
        {
            weaponController.UnequipWeapon();
            weaponController.enabled = false;
        }

        if (meleeController != null)
        {
            meleeController.UnequipWeapon();
            meleeController.enabled = false;
        }
    }

    private void RouteToController(EquippedWeapon weapon)
    {
        if (weapon.definition is RangedWeaponDefinition rangedDef)
        {
            if (meleeController != null)
            {
                meleeController.UnequipWeapon();
                meleeController.enabled = false;
            }

            if (weaponController != null)
            {
                weaponController.enabled = true;

                if (weapon.cachedAmmo >= 0)
                    weaponController.ResumeWeapon(rangedDef, weapon.gunVisuals, weapon.cachedAmmo);
                else
                    weaponController.EquipWeapon(rangedDef, weapon.gunVisuals);
            }
        }
        else if (weapon.definition is MeleeWeaponDefinition meleeDef)
        {
            if (weaponController != null)
            {
                weaponController.UnequipWeapon();
                weaponController.enabled = false;
            }

            if (meleeController != null)
            {
                meleeController.enabled = true;
                meleeController.EquipWeapon(meleeDef, weapon.instance);
            }
        }
    }

    private void DeactivateCurrentVisuals()
    {
        EquippedWeapon active = GetActiveEquippedWeapon();
        if (active == null) return;

        if (active.definition is RangedWeaponDefinition && weaponController != null)
            active.cachedAmmo = weaponController.CurrentAmmo;

        if (active.instance != null)
            active.instance.SetActive(false);
    }

    // ---- Cycling ----

    public void CycleWeapon(int direction)
    {
        if (isTransitioning) return;

        int occupied = CountLoadoutWeapons();
        if (occupied == 0) return;

        if (isEmptyHand)
        {
            int slot = activeLoadoutIndex >= 0 && loadout[activeLoadoutIndex] != null
                ? activeLoadoutIndex
                : FindFirstLoadoutWeapon();
            if (slot >= 0)
                StartCoroutine(SwitchToLoadoutSlot(slot));
            return;
        }

        if (occupied <= 1) return;

        int nextSlot = FindNextLoadoutSlot(activeLoadoutIndex, direction);
        if (nextSlot >= 0 && nextSlot != activeLoadoutIndex)
            StartCoroutine(SwitchToLoadoutSlot(nextSlot));
    }

    // ---- Lookup Helpers ----

    private EquippedWeapon GetActiveEquippedWeapon()
    {
        if (isEmptyHand) return null;
        if (activeLoadoutIndex < 0 || activeLoadoutIndex >= LoadoutSize) return null;
        return loadout[activeLoadoutIndex];
    }

    private bool IsInLoadout(EquippedWeapon weapon)
    {
        for (int i = 0; i < LoadoutSize; i++)
        {
            if (loadout[i] == weapon) return true;
        }
        return false;
    }

    private int FindEmptyLoadoutSlot()
    {
        for (int i = 0; i < LoadoutSize; i++)
        {
            if (loadout[i] == null) return i;
        }
        return -1;
    }

    private int FindFirstLoadoutWeapon()
    {
        for (int i = 0; i < LoadoutSize; i++)
        {
            if (loadout[i] != null) return i;
        }
        return -1;
    }

    private int FindNextLoadoutSlot(int currentSlot, int direction)
    {
        for (int step = 1; step < LoadoutSize; step++)
        {
            int candidate = (currentSlot + direction * step + LoadoutSize) % LoadoutSize;
            if (loadout[candidate] != null) return candidate;
        }
        return -1;
    }

    private int CountLoadoutWeapons()
    {
        int count = 0;
        for (int i = 0; i < LoadoutSize; i++)
        {
            if (loadout[i] != null) count++;
        }
        return count;
    }

    private EquippedWeapon FindEquipped(WeaponDefinition weaponDef)
    {
        foreach (EquippedWeapon eq in allWeapons)
        {
            if (eq.definition == weaponDef)
                return eq;
        }
        return null;
    }
}