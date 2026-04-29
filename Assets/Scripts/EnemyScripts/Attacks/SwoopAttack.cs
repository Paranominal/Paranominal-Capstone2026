using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable telegraphed swoop attack. Handles the sequence:
///   1. Windup  - hover in place, face the dive target (the player's read)
///   2. Dive    - move in a straight line through the snapshotted target point.
///                The Hitbox is active during this phase.
///   3. Return  - fly back to the supplied return position
///   4. Recover - brief pause before the attack is considered finished
///
/// The owning behaviour snapshots the target position and supplies a return
/// position (typically the bat's anchor + hover offset). Once committed, the
/// dive target does not move - this is the fairness contract that makes the
/// swoop dodgeable.
///
/// Hit detection is delegated to the Hitbox component, which sits on a child
/// trigger collider and routes hits to IDamageable targets.
/// </summary>
public class SwoopAttack : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Hitbox component on a child GameObject. Active only during the dive phase.")]
    [SerializeField] private Hitbox hitbox;

    [Header("Timing")]
    [Tooltip("How long the owner hovers and telegraphs the dive before committing. " +
             "Main 'feel' knob - longer = easier to dodge.")]
    [SerializeField] private float windupDuration = 0.6f;

    [Tooltip("Brief pause after returning to the anchor before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.2f;

    [Header("Movement")]
    [Tooltip("Speed of the dive itself.")]
    [SerializeField] private float diveSpeed = 14f;

    [Tooltip("Speed of the return flight back to the anchor.")]
    [SerializeField] private float returnSpeed = 6f;

    [Tooltip("How far past the snapshotted target the dive continues. Ensures the bat " +
             "physically passes through the target point rather than stopping on top of it.")]
    [SerializeField] private float divePastDistance = 1.5f;

    [Tooltip("Turn speed used when facing the dive target during windup.")]
    [SerializeField] private float windupTurnSpeed = 360f;

    [Header("Damage")]
    [Tooltip("Damage value passed to the Hitbox when the dive begins.")]
    [SerializeField] private int damage = 10;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isAttacking;
    private Coroutine attackRoutine;
    private Rigidbody rb;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Begin the windup -> dive -> return -> recovery sequence.
    /// targetPosition should be snapshotted by the caller before this is invoked.
    /// returnPosition is where the owner flies back to after the dive.
    /// </summary>
    public void PerformAttack(Vector3 targetPosition, Vector3 returnPosition)
    {
        if (isAttacking)
        {
            if (debugMode)
                Debug.LogWarning($"[SwoopAttack] PerformAttack called while already attacking. Ignored.", this);
            return;
        }

        if (hitbox == null)
        {
            Debug.LogError($"[SwoopAttack] No Hitbox assigned on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(targetPosition, returnPosition));
    }

    /// <summary>
    /// Cancel an in-progress swoop. The owner stops where it is and the hitbox is deactivated.
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

        if (debugMode)
            Debug.Log($"[SwoopAttack] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Vector3 targetPosition, Vector3 returnPosition)
    {
        isAttacking = true;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Windup started. Target: {targetPosition}", this);

        //windup: hover in place and face the target
        yield return WindupPhase(targetPosition, windupDuration);

        //commit: lock in the dive endpoint, extending past the target so we pass through it
        Vector3 diveDirection = (targetPosition - transform.position).normalized;
        Vector3 diveEndpoint = targetPosition + diveDirection * divePastDistance;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Diving to {diveEndpoint}", this);

        //activate the hitbox for the duration of the dive
        DamageInfo damageInfo = new DamageInfo(damage, transform.position, diveDirection, gameObject);
        hitbox.Activate(damageInfo);

        //dive
        yield return DivePhase(diveEndpoint);

        //hitbox off as soon as the dive ends - return flight should not damage
        hitbox.Deactivate();

        //return: fly back to the supplied return position
        yield return ReturnPhase(returnPosition);

        //recovery
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Attack complete on {gameObject.name}.", this);
    }

    private IEnumerator WindupPhase(Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            //face the dive target on the horizontal plane
            Vector3 lookDir = targetPosition - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                    windupTurnSpeed * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator DivePhase(Vector3 endpoint)
    {
        while (Vector3.Distance(transform.position, endpoint) > 0.1f)
        {
            Vector3 moveDir = (endpoint - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * diveSpeed * Time.deltaTime;

            if (rb != null)
                rb.MovePosition(newPos);
            else
                transform.position = newPos;

            yield return null;
        }
    }

    private IEnumerator ReturnPhase(Vector3 returnPosition)
    {
        while (Vector3.Distance(transform.position, returnPosition) > 0.1f)
        {
            Vector3 moveDir = (returnPosition - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * returnSpeed * Time.deltaTime;

            if (rb != null)
                rb.MovePosition(newPos);
            else
                transform.position = newPos;

            //face the direction of return travel
            Vector3 lookDir = moveDir;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                    windupTurnSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }
}
