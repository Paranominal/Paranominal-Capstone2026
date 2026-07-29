using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Handles melee attack input, cooldown, and Hitbox activation.
// Used for both the empty-hand shove (via the "Fists" MeleeWeaponDefinition)
// and dedicated melee weapons. WeaponManager sets the active definition
// and enables/disables this component based on the current weapon mode.
public class MeleeController : MonoBehaviour
{
    [Header("Active Weapon")]
    [SerializeField] private MeleeWeaponDefinition activeWeapon;

    [Header("Timing")]
    [Tooltip("How long the hitbox stays active per swing.")]
    [SerializeField] private float strikeDuration = 0.15f;

    [Header("Input")]
    [SerializeField] private string attackActionName = "ShootIron";

    private Hitbox hitbox;
    private InputAction attackAction;
    private bool onCooldown;
    private bool isAttacking;
    private Coroutine attackRoutine;

    public bool IsAttacking => isAttacking;
    public MeleeWeaponDefinition ActiveWeapon => activeWeapon;

    private void Awake()
    {
        attackAction = InputSystem.actions.FindAction(attackActionName);
    }

    private void Update()
    {
        if (!enabled) return;
        if (activeWeapon == null) return;
        if (onCooldown || isAttacking) return;

        if (attackAction != null && attackAction.WasPressedThisFrame())
        {
            attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    // Summary: Equip a melee weapon definition. Resolves the Hitbox from the weapon's
    // root GameObject (prefab instance or defaultHand). Called by WeaponManager on mode switch.
    public void EquipWeapon(MeleeWeaponDefinition weapon, GameObject weaponRoot)
    {
        CancelAttack();
        activeWeapon = weapon;
        hitbox = weaponRoot != null ? weaponRoot.GetComponentInChildren<Hitbox>() : null;

        if (hitbox == null)
            Debug.LogWarning("MeleeController: no Hitbox found on " +
                (weaponRoot != null ? weaponRoot.name : "null"));
    }

    // Summary: Clear the active weapon and stop any in-progress attack.
    public void UnequipWeapon()
    {
        CancelAttack();
        activeWeapon = null;
        hitbox = null;
    }

    // Summary: Fire a single knockback attack without player input, then invoke
    // the callback when the strike finishes. Used by WeaponManager during the
    // quick shove sequence. Resolves the Hitbox from the weapon root.
    // Does not apply cooldown (the weapon switch animation serves as recovery).
    public void PerformQuickShove(MeleeWeaponDefinition shoveWeapon, GameObject weaponRoot, Action onComplete)
    {
        CancelAttack();
        hitbox = weaponRoot != null ? weaponRoot.GetComponentInChildren<Hitbox>() : null;
        attackRoutine = StartCoroutine(QuickShoveRoutine(shoveWeapon, onComplete));
    }

    public void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopAllCoroutines();
            attackRoutine = null;
        }

        if (hitbox != null && hitbox.IsActive) hitbox.Deactivate();
        isAttacking = false;
        onCooldown = false;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        DamageInfo info = BuildDamageInfo(activeWeapon);
        hitbox.Activate(info);

        yield return new WaitForSeconds(strikeDuration);

        hitbox.Deactivate();
        isAttacking = false;

        // Cooldown
        onCooldown = true;
        yield return new WaitForSeconds(activeWeapon.attackCooldown);
        onCooldown = false;

        attackRoutine = null;
    }

    private IEnumerator QuickShoveRoutine(MeleeWeaponDefinition shoveWeapon, Action onComplete)
    {
        isAttacking = true;

        DamageInfo info = BuildDamageInfo(shoveWeapon);
        hitbox.Activate(info);

        yield return new WaitForSeconds(strikeDuration);

        hitbox.Deactivate();
        isAttacking = false;
        attackRoutine = null;

        onComplete?.Invoke();
    }

    private DamageInfo BuildDamageInfo(MeleeWeaponDefinition weapon)
    {
        return new DamageInfo(
            weapon.damage,
            transform.position,
            transform.forward,
            gameObject,
            weapon.knockbackForce
        );
    }
}