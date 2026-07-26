using System.Collections.Generic;
using UnityEngine;

// Summary: Dynamically manages weapon equipping based on Inventory contents.
// When a weapon-tagged item enters Inventory, its equippedPrefab is instantiated
// under the hand transform. Cached instances are activated/deactivated for fast switching.
// No manual slot configuration needed.
public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponParent;       // RHand transform where weapon models spawn
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private GameObject defaultHand;       // empty hand model, shown when no weapon is equipped

    private Inventory inventory;

    // Runtime tracking of instantiated weapon models.
    private class EquippedWeapon
    {
        public WeaponDefinition definition;
        public GameObject instance;
        public GunVisuals gunVisuals;
    }

    private List<EquippedWeapon> equippedWeapons = new List<EquippedWeapon>();
    private int activeIndex = -1;

    void Awake()
    {
        if (weaponController == null)
            weaponController = FindAnyObjectByType<WeaponController>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
    }

    void Start()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += OnInventoryChanged;

        // Show the empty hand by default.
        if (defaultHand != null)
            defaultHand.SetActive(true);

        OnInventoryChanged();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        if (inventory == null) return;

        // Check for any new weapon-tagged items in inventory that we haven't instantiated yet.
        List<ItemDefinition> weapons = inventory.GetItems(ItemTag.Weapon);
        bool addedNew = false;

        foreach (ItemDefinition item in weapons)
        {
            WeaponDefinition weaponDef = item as WeaponDefinition;
            if (weaponDef == null) continue;

            if (FindEquipped(weaponDef) != null) continue;   // already instantiated

            EquippedWeapon equipped = InstantiateWeapon(weaponDef);
            if (equipped != null)
            {
                equippedWeapons.Add(equipped);
                addedNew = true;
            }
        }

        // If nothing was equipped before and we just added something, equip the first one.
        if (activeIndex < 0 && equippedWeapons.Count > 0)
        {
            ActivateWeapon(0);
        }
        else if (addedNew && activeIndex >= 0)
        {
            // New weapon added while another is active. Leave current active.
            // Player can switch to it manually.
        }
    }

    private EquippedWeapon InstantiateWeapon(WeaponDefinition weaponDef)
    {
        if (weaponDef.equippedPrefab == null || weaponParent == null)
        {
            Debug.LogWarning("WeaponManager: no equippedPrefab set on " + weaponDef.displayName);
            return null;
        }

        GameObject instance = Instantiate(weaponDef.equippedPrefab, weaponParent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.SetActive(false);   // starts hidden until activated

        GunVisuals visuals = instance.GetComponentInChildren<GunVisuals>();

        EquippedWeapon equipped = new EquippedWeapon
        {
            definition = weaponDef,
            instance = instance,
            gunVisuals = visuals,
        };

        Debug.Log("WeaponManager: instantiated " + weaponDef.displayName);
        return equipped;
    }

    private void ActivateWeapon(int index)
    {
        if (index < 0 || index >= equippedWeapons.Count) return;

        // Deactivate current weapon.
        if (activeIndex >= 0 && activeIndex < equippedWeapons.Count)
        {
            equippedWeapons[activeIndex].instance.SetActive(false);
        }

        // Hide the default hand when a weapon is equipped.
        if (defaultHand != null)
            defaultHand.SetActive(false);

        activeIndex = index;
        EquippedWeapon weapon = equippedWeapons[activeIndex];

        // Activate the instance first so OnEnable fires on GunVisuals/WeaponAudio.
        weapon.instance.SetActive(true);

        if (weaponController != null)
            weaponController.EquipWeapon(weapon.definition, weapon.gunVisuals);

        Debug.Log("Equipped weapon: " + weapon.definition.displayName);
    }

    // Summary: Switches to the next or previous weapon. direction = 1 for next, -1 for previous.
    public void CycleWeapon(int direction)
    {
        if (equippedWeapons.Count <= 1) return;

        int nextIndex = (activeIndex + direction + equippedWeapons.Count) % equippedWeapons.Count;
        ActivateWeapon(nextIndex);
    }

    // Summary: Equips a specific weapon by its definition. Used if you want direct selection.
    public void EquipSpecific(WeaponDefinition weaponDef)
    {
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            if (equippedWeapons[i].definition == weaponDef)
            {
                ActivateWeapon(i);
                return;
            }
        }
    }

    private EquippedWeapon FindEquipped(WeaponDefinition weaponDef)
    {
        foreach (EquippedWeapon eq in equippedWeapons)
        {
            if (eq.definition == weaponDef)
                return eq;
        }
        return null;
    }
}