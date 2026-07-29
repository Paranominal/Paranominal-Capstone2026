using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Drives the Minimised Grimoire's open/close animations.
// When in empty hand mode, the grimoire auto-closes on movement so the
// fear-state book cover is visible. When a weapon is equipped, the grimoire
// stays open so quick slots are visible.
// Animation event callbacks (StartAnimation, EndAnimation, SetOpenTrue,
// SetOpenFalse, SetCastingTrue, SetCastingFalse) are called from the Animator.
public class GrimoireAnimManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator grimoireAnimator;
    [SerializeField] private ALTGrimoire altGrimoire;

    [Header("Input")]
    [SerializeField] private InputActionReference playerMove;
    [SerializeField] private InputActionReference scanAction;
    [SerializeField] private InputActionReference collectAction;

    [Header("Timing")]
    [Tooltip("Duration in seconds the grimoire ignores close after being opened.")]
    [SerializeField] private float stayOpenDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAnimating;
    private bool isOpen;
    private bool isCasting;
    private bool stayOpen;

    // EDIT (weapon system): when true, the grimoire is forced open and auto-close is suppressed.
    private bool weaponEquipped;

    // EDIT (minimised grimoire): when the full grimoire is open, GrimoireAnimManager
    // stops all open/close behaviour to avoid fighting ALTGrimoire's animations.
    private bool fullGrimoireOpen;

    private void OnEnable()
    {
        WeaponManager.OnWeaponModeChanged += HandleWeaponModeChanged;

        if (altGrimoire != null)
            altGrimoire.OnGrimoireToggled += HandleGrimoireToggled;
    }

    private void OnDisable()
    {
        WeaponManager.OnWeaponModeChanged -= HandleWeaponModeChanged;

        if (altGrimoire != null)
            altGrimoire.OnGrimoireToggled -= HandleGrimoireToggled;
    }

    private void Update()
    {
        // When the full grimoire is open, ALTGrimoire owns the book state.
        if (fullGrimoireOpen) return;

        CheckPlayerMovement();
        CheckCast();
        CheckScan();
        CheckCollect();

        if (debugMode) DoDebugLog();
    }

    // ---- Full Grimoire ----

    // EDIT (minimised grimoire): when the full grimoire opens, force the book open.
    // When it closes, restore the correct minimised state based on weapon mode.
    private void HandleGrimoireToggled(bool opened)
    {
        if (opened)
        {
            // Full grimoire opening. Force the book pages open.
            if (!isOpen && !isAnimating)
                grimoireAnimator.SetTrigger("open");
        }

        fullGrimoireOpen = opened;

        if (!opened)
        {
            // Full grimoire just closed. Restore minimised state.
            if (weaponEquipped)
                TryOpen();
            else
                TryClose();
        }
    }

    // ---- Weapon Mode ----

    // EDIT (weapon system): force grimoire open when weapon is equipped,
    // allow normal close behavior when in empty hand.
    private void HandleWeaponModeChanged(bool equipped)
    {
        weaponEquipped = equipped;

        if (weaponEquipped)
            TryOpen();
        else
            TryClose();
    }

    // ---- Open / Close ----

    private void TryOpen()
    {
        if (fullGrimoireOpen) return;
        if (isAnimating) return;
        if (isOpen) return;

        grimoireAnimator.SetTrigger("open");
        StartCoroutine(StayOpenTimer());
    }

    private void TryClose()
    {
        if (fullGrimoireOpen) return;
        if (isAnimating) return;
        if (!isOpen) return;
        if (stayOpen) return;
        if (isCasting) return;
        if (weaponEquipped) return;

        grimoireAnimator.SetTrigger("close");
    }

    private IEnumerator StayOpenTimer()
    {
        stayOpen = true;
        yield return new WaitForSeconds(stayOpenDuration);
        stayOpen = false;
    }

    // ---- Input Checks ----

    private void CheckPlayerMovement()
    {
        bool moving = playerMove.action.ReadValue<Vector2>().sqrMagnitude > 0f;

        if (moving && !isCasting)
            TryClose();
    }

    private void CheckCast()
    {
        grimoireAnimator.SetBool("isCasting", isCasting);

        if (scanAction.action.IsPressed())
        {
            if (!isOpen)
            {
                TryOpen();
                return;
            }
            isCasting = true;
        }
        else
        {
            isCasting = false;
        }
    }

    private void CheckScan()
    {
        if (scanAction.action.WasPressedThisFrame())
            TryOpen();
    }

    private void CheckCollect()
    {
        if (collectAction.action.WasPressedThisFrame())
            TryOpen();
    }

    // ---- Animation Event Callbacks ----
    // Called by the Animator. Do not rename or remove.

    private void StartAnimation() { isAnimating = true; }
    private void EndAnimation() { isAnimating = false; }
    private void SetOpenTrue() { isOpen = true; }
    private void SetOpenFalse() { isOpen = false; }
    private void SetCastingTrue() { isCasting = true; }
    private void SetCastingFalse() { isCasting = false; }

    // ---- Debug ----

    private void DoDebugLog()
    {
        Debug.Log($"[GrimoireAnim] isOpen={isOpen} isAnimating={isAnimating} " +
                  $"isCasting={isCasting} stayOpen={stayOpen} weaponEquipped={weaponEquipped}");
    }
}