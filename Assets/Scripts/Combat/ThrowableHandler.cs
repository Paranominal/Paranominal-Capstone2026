using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Manages "throwable mode" for quick-slotted throwable items.
// Subscribes to QuickSlotManager.OnItemUsed, enters throwable mode when a
// Throwable-tagged item is used, spawns projectiles on fire input, and exits
// on holster/scroll or when the last item is thrown.
// Lives on the Player alongside QuickSlotManager.
public class ThrowableHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private QuickSlotManager quickSlotManager;
    [SerializeField] private Inventory inventory;

    [Header("Input")]
    [SerializeField] private string fireActionName = "Fire";
    [SerializeField] private string holsterActionName = "Holster";
    [SerializeField] private string scrollActionName = "ScrollWeapon";

    private InputAction fireAction;
    private InputAction holsterAction;
    private InputAction scrollAction;

    private bool inThrowableMode;
    private ItemDefinition activeItem;
    private ThrowableDefinition activeDefinition;

    // Summary: True while the player is in throwable mode. External systems can query this.
    public bool InThrowableMode => inThrowableMode;

    private void Awake()
    {
        if (weaponManager == null)
            weaponManager = FindAnyObjectByType<WeaponManager>();
        if (quickSlotManager == null)
            quickSlotManager = FindAnyObjectByType<QuickSlotManager>();
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        fireAction = InputSystem.actions.FindAction(fireActionName);
        holsterAction = InputSystem.actions.FindAction(holsterActionName);
        scrollAction = InputSystem.actions.FindAction(scrollActionName);
    }

    private void OnEnable()
    {
        if (quickSlotManager != null)
            quickSlotManager.OnItemUsed += OnItemUsed;
    }

    private void OnDisable()
    {
        if (quickSlotManager != null)
            quickSlotManager.OnItemUsed -= OnItemUsed;

        if (inThrowableMode)
            ExitThrowableMode();
    }

    private void Update()
    {
        if (!inThrowableMode) return;

        // Fire: throw one
        if (fireAction != null && fireAction.WasPressedThisFrame())
            Throw();

        // Q: exit throwable mode, re-equip weapon
        if (holsterAction != null && holsterAction.WasPressedThisFrame())
            ExitThrowableMode();

        // Scroll: exit throwable mode, re-equip weapon
        if (scrollAction != null)
        {
            float scrollValue = scrollAction.ReadValue<float>();
            if (scrollValue != 0f)
                ExitThrowableMode();
        }
    }

    // ---- Mode Entry/Exit ----

    private void OnItemUsed(int slotIndex, ItemDefinition item)
    {
        if (!item.tags.HasFlag(ItemTag.Throwable)) return;

        // EDIT (throwable system): ItemDefinition needs a ThrowableDefinition field
        // (similar to the Recipe field). Adjust this access to match your field name.
        ThrowableDefinition definition = item.throwableData;
        if (definition == null)
        {
            Debug.LogWarning("ThrowableHandler: item " + item.displayName
                + " is tagged Throwable but has no ThrowableDefinition.");
            return;
        }

        if (definition.projectilePrefab == null)
        {
            Debug.LogWarning("ThrowableHandler: ThrowableDefinition on " + item.displayName
                + " has no projectile prefab.");
            return;
        }

        EnterThrowableMode(item, definition);
    }

    private void EnterThrowableMode(ItemDefinition item, ThrowableDefinition definition)
    {
        activeItem = item;
        activeDefinition = definition;
        inThrowableMode = true;

        // Lock WeaponManager input and holster the weapon
        if (weaponManager != null)
        {
            weaponManager.InputLocked = true;
            weaponManager.RequestHolster();
        }
    }

    private void ExitThrowableMode()
    {
        inThrowableMode = false;
        activeItem = null;
        activeDefinition = null;

        // Unlock WeaponManager and re-equip
        if (weaponManager != null)
        {
            weaponManager.InputLocked = false;
            weaponManager.RequestUnholster();
        }
    }

    // ---- Throwing ----

    private void Throw()
    {
        if (activeItem == null || activeDefinition == null) return;
        if (inventory == null || !inventory.Has(activeItem)) return;

        // Spawn at the throw point, launch along the camera's look direction
        Vector3 origin = throwPoint != null ? throwPoint.position : transform.position;
        Vector3 direction = Camera.main != null ? Camera.main.transform.forward : transform.forward;

        GameObject projectileObj = Instantiate(activeDefinition.projectilePrefab, origin, Quaternion.identity);
        ThrownProjectile projectile = projectileObj.GetComponent<ThrownProjectile>();

        if (projectile != null)
            projectile.Initialize(activeDefinition, direction, gameObject);

        // Consume one from inventory
        inventory.Remove(activeItem, 1);

        // If none left, exit throwable mode
        if (!inventory.Has(activeItem))
            ExitThrowableMode();
    }
}