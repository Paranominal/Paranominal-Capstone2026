using System.Collections;
using UnityEngine;

// The Eye-bat's Swoop Attack:
//   1. Windup - Mover in place, face the target.
//   2. Dive - Move in a straight line through the snapshotted target point. The Hitbox is active during this phase.
//   3. Resolve - On hit, return to the pre-swoop position -> On miss, rise straight up from where the dive ended to the pre-swoop altitude.
//   4. Recover - Brief pause before the attack is considered finished.

// The Eye-Bat behaviour script snapshots the player's position and supplies it. SwoopAttack snapshots the Bat's own pre-swoop position internally, so the post-dive resolution
// is fully handled here. Once committed, the target position does not move - this is what makes the swoop dodgeable.

// Hit detection is delegated to the Hitbox component, which sits on a child trigger collider and routes hits to IDamageable targets.
// SwoopAttack subscribes to Hitbox.OnHit during the dive to track whether the swoop landed.
public class SwoopAttack : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Hitbox component on a child GameObject. Active only during the dive phase.")]
    [SerializeField] private Hitbox hitbox;

    [Header("Timing")]
    [Tooltip("How long the owner hovers and telegraphs the dive before committing. " +
             "Main 'feel' knob - longer = easier to dodge.")]
    [SerializeField] private float windupDuration = 0.6f;

    [Tooltip("Brief pause after resolving the dive before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.2f;

    [Header("Movement")]
    [Tooltip("Speed of the dive itself.")]
    [SerializeField] private float diveSpeed = 14f;

    [Tooltip("Speed of the return flight back to the pre-swoop position on a hit.")]
    [SerializeField] private float returnSpeed = 6f;

    [Tooltip("Speed of the rise to hover altitude on a miss.")]
    [SerializeField] private float riseSpeed = 4f;

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
    private bool diveLandedHit;
    private Coroutine attackRoutine;

    public bool IsAttacking => isAttacking;

    // Begin the windup -> dive -> resolve -> recovery sequence.
    public void PerformAttack(Vector3 targetPosition)
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

        attackRoutine = StartCoroutine(AttackSequence(targetPosition));
    }

    // Cancel an in-progress swoop. The owner stops where it is and the hitbox is deactivated.
    public void CancelAttack()
    {
        if (!isAttacking) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hitbox != null)
        {
            hitbox.OnContact -= HandleHitboxContact;
            hitbox.Deactivate();
        }
        isAttacking = false;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Attack cancelled on {gameObject.name}.", this);
    }

    // Flips the dive-landed flag so the resolve phase knows to use the return-to-snapshot path.
    private void HandleHitboxContact(Collider other)
    {
        diveLandedHit = true;
    }

    private IEnumerator AttackSequence(Vector3 targetPosition)
    {
        isAttacking = true;
        diveLandedHit = false;

        // Snapshot the pre-swoop position and altitude for use in the resolve phase
        Vector3 preSwoopPosition = transform.position;
        float preSwoopAltitude = preSwoopPosition.y;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Windup started. Target: {targetPosition}, Anchor: {preSwoopPosition}", this);

        //windup: hover in place and face the target
        yield return WindupPhase(targetPosition, windupDuration);

        // Commit: lock in the dive endpoint, extending past the target so we pass through it
        Vector3 diveDirection = (targetPosition - transform.position).normalized;
        Vector3 diveEndpoint = targetPosition + diveDirection * divePastDistance;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Diving to {diveEndpoint}", this);

        // Subscribe to contact for the duration of the dive so we can branch on hit vs. miss.
        hitbox.OnContact += HandleHitboxContact;

        // Activate the hitbox for the duration of the dive
        DamageInfo damageInfo = new DamageInfo(damage, transform.position, diveDirection, gameObject);
        hitbox.Activate(damageInfo);

        // Dive
        yield return DivePhase(diveEndpoint);

        // Hitbox off and unsubscribe as soon as the dive ends - resolution movement should not damage
        hitbox.Deactivate();
        hitbox.OnContact -= HandleHitboxContact;

        // Resolve: on hit, fly back to the pre-swoop position. on miss, rise straight up.
        if (diveLandedHit)
        {
            if (debugMode)
                Debug.Log($"[SwoopAttack] Hit landed. Returning to pre-swoop position {preSwoopPosition}.", this);
            yield return ReturnToPreSwoopPhase(preSwoopPosition);
        }
        else
        {
            if (debugMode)
                Debug.Log($"[SwoopAttack] Miss. Rising to altitude {preSwoopAltitude} and turning back toward snapshot {targetPosition}.", this);
            yield return RiseToAltitudePhase(preSwoopAltitude, targetPosition);
        }

        // Recovery
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
            // Face the dive target on the horizontal plane
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
        // Break out the moment a hit is registered so the bat doesn't push through the player for the rest of the dive distance
        while (Vector3.Distance(transform.position, endpoint) > 0.1f)
        {
            if (diveLandedHit) yield break;

            Vector3 moveDir = (endpoint - transform.position).normalized;
            transform.position += moveDir * diveSpeed * Time.deltaTime;

            yield return null;
        }
    }

    // fly back to the snapshotted pre-swoop position; used when the dive landed a hit
    private IEnumerator ReturnToPreSwoopPhase(Vector3 preSwoopPosition)
    {
        while (Vector3.Distance(transform.position, preSwoopPosition) > 0.1f)
        {
            Vector3 moveDir = (preSwoopPosition - transform.position).normalized;
            transform.position += moveDir * returnSpeed * Time.deltaTime;

            // Face the direction of return travel
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

    // rise straight up from the dive's end position to the pre-swoop altitude while turning back to face the snapshotted target - used on a miss.
    private IEnumerator RiseToAltitudePhase(float targetAltitude, Vector3 lookBackTarget)
    {
        Vector3 lookDir = lookBackTarget - transform.position;
        lookDir.y = 0f;
        Quaternion targetRot = lookDir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
            : transform.rotation;

        bool stillRising = true;
        bool stillTurning = true;

        while (stillRising || stillTurning)
        {
            // Rise toward target altitude
            if (stillRising)
            {
                float yStep = Mathf.MoveTowards(transform.position.y, targetAltitude, riseSpeed * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, yStep, transform.position.z);

                if (Mathf.Abs(transform.position.y - targetAltitude) <= 0.1f) stillRising = false;
            }

            // Turn to face the snapshot position concurrently with the rise
            if (stillTurning)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                    windupTurnSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, targetRot) <= 0.5f) stillTurning = false;
            }

            yield return null;
        }
    }
}
