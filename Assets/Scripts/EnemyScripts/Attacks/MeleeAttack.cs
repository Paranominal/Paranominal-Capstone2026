using System.Collections;
using UnityEngine;

/// Summary:
/// Reusable telegraphed melee attack. Handles the sequence:
///   1. Windup    - owner tracks the target at a slow turn speed (the player's read).
///                  This is the "rear back" phase before the swing commits.
///   2. Strike    - hitbox is activated for a brief window. Whatever direction the
///                  owner is facing at this moment determines where the swing lands.
///   3. Recovery  - hitbox is off, brief vulnerability window before control returns.
///
/// Unlike AoEAttack and SwoopAttack which snapshot a target position at commit,
/// this attack tracks the target during windup at a (typically slow) turn speed.
/// Sidestepping during windup is the player's primary dodge option.
///
/// The hitbox should be a child trigger volume positioned in front of the owner
/// where the strike should land. Hit detection is delegated to the Hitbox component.
public class MeleeAttack : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Hitbox component on a child GameObject, positioned where the strike lands. " +
             "Active only during the strike phase.")]
    [SerializeField] private Hitbox hitbox;

    [Header("Timing")]
    [Tooltip("How long the owner winds up before striking. Main 'feel' knob - " +
             "longer = more readable, easier to dodge.")]
    [SerializeField] private float windupDuration = 0.6f;

    [Tooltip("How long the hitbox is active during the strike. Should be short.")]
    [SerializeField] private float strikeDuration = 0.15f;

    [Tooltip("Brief pause after the strike before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Tracking")]
    [Tooltip("Turn speed used while tracking the target during windup. " +
             "Lower = more dodgeable. Set to 0 to lock facing at commit.")]
    [SerializeField] private float windupTurnSpeed = 90f;

    [Header("Damage")]
    [Tooltip("Damage value passed to the Hitbox when the strike begins.")]
    [SerializeField] private int damage = 10;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isAttacking;
    private Coroutine attackRoutine;

    public bool IsAttacking => isAttacking;

    //feedback hooks - subscribed to by visual feedback components.
    //the events fire in lockstep with the AttackSequence coroutine, so anything
    //listening can drive its own visuals against the same timeline.
    public event System.Action OnWindupStart;
    public event System.Action OnStrikeStart;
    public event System.Action OnStrikeEnd;
    public event System.Action OnAttackCancelled;

    /// <summary>How long the windup phase lasts. Useful for feedback components that need to scale animations to it.</summary>
    public float WindupDuration => windupDuration;
    /// <summary>How long the strike phase lasts.</summary>
    public float StrikeDuration => strikeDuration;

    /// <summary>
    /// Begin the windup -> strike -> recovery sequence.
    /// The owner will track the target during windup at windupTurnSpeed.
    /// </summary>
    public void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode)
                Debug.LogWarning($"[MeleeAttack] PerformAttack called while already attacking. Ignored.", this);
            return;
        }

        if (hitbox == null)
        {
            Debug.LogError($"[MeleeAttack] No Hitbox assigned on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(target));
    }

    /// <summary>
    /// Cancel an in-progress attack. The hitbox is deactivated immediately.
    /// Useful for stagger interruption.
    /// </summary>
    public void CancelAttack()
    {
        if (!isAttacking) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hitbox != null) hitbox.Deactivate();
        isAttacking = false;

        OnAttackCancelled?.Invoke();

        if (debugMode)
            Debug.Log($"[MeleeAttack] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;

        if (debugMode)
            Debug.Log($"[MeleeAttack] Windup started on {gameObject.name}.", this);

        OnWindupStart?.Invoke();

        //windup: track the target while the player reads the tell
        yield return WindupPhase(target);

        if (debugMode)
            Debug.Log($"[MeleeAttack] Strike on {gameObject.name}.", this);

        //strike: activate the hitbox briefly
        Vector3 strikeDir = transform.forward;
        DamageInfo damageInfo = new DamageInfo(damage, transform.position, strikeDir, gameObject);
        hitbox.Activate(damageInfo);

        OnStrikeStart?.Invoke();

        yield return new WaitForSeconds(strikeDuration);

        hitbox.Deactivate();

        OnStrikeEnd?.Invoke();

        //recovery
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;

        if (debugMode)
            Debug.Log($"[MeleeAttack] Attack complete on {gameObject.name}.", this);
    }

    private IEnumerator WindupPhase(Transform target)
    {
        float elapsed = 0f;
        while (elapsed < windupDuration)
        {
            //track the target on the horizontal plane at the configured turn speed.
            //if windupTurnSpeed is 0, this becomes a "facing locked at commit" attack.
            if (target != null && windupTurnSpeed > 0f)
            {
                Vector3 lookDir = target.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                        windupTurnSpeed * Time.deltaTime);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
