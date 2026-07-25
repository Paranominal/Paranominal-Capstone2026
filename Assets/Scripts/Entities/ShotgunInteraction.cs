using UnityEngine;

// EDIT (grimoire migration): uses Inventory + ItemDefinition reference instead of grimoire string matching.
public class ShotgunInteraction : MonoBehaviour
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private ItemDefinition shotgunItem;

    private Inventory inventory;
    private bool collected;

    void Awake()
    {
        if (weaponController == null)
            weaponController = FindAnyObjectByType<WeaponController>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
    }

    void Update()
    {
        if (collected) return;

        if (inventory != null && shotgunItem != null && inventory.Has(shotgunItem))
        {
            weaponController.SetWeaponEnabled(true);
            collected = true;
        }
    }
}
