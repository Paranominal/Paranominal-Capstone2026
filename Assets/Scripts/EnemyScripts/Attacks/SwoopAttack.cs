using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable telegraphed swoop attack. Handles the sequence:
///   1. Windup  - hover in place, face the dive target (the player's read)
///   2. Dive    - move in a straight line through the snapshotted target point.
///                The Hitbox is active during this phase.
///   3. Resolve - on hit, return to the pre-swoop position.
///                on miss, rise straight up from where the dive ended to the
///                pre-swoop altitude.
///   4. Recover - brief pause before the attack is considered finished
///
/// The owning behaviour snapshots the dive target and supplies it. SwoopAttack
/// snapshots its own pre-swoop position internally, so the post-dive resolution
/// is fully encapsulated here. Once committed, the dive target does not move -
/// this is the fairness contract that makes the swoop dodgeable.
///
/// Hit detection is delegated to the Hitbox component, which sits on a child
/// trigger collider and routes hits to IDamageable targets. SwoopAttack
/// subscribes to Hitbox.OnHit during the dive to track whether the swoop landed.
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
    private Rigidbody rb;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Begin the windup -> dive -> resolve -> recovery sequence.
    /// targetPosition should be snapshotted by the caller before this is invoked.
    /// SwoopAttack snapshots its own position internally for the hit-retreat target.
    /// </summary>
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

        if (hitbox != null)
        {
            hitbox.OnContact -= HandleHitboxContact;
            hitbox.Deactivate();
        }
        isAttacking = false;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Attack cancelled on {gameObject.name}.", this);
    }

    //flips the dive-landed flag so the resolve phase knows to use the return-to-snapshot path.
    //subscribed to OnContact rather than OnHit so the swoop registers as "landed" purely from
    //physical contact with a layer-matching collider - this works even before the player has
    //an IDamageable component. once IDamageable exists, this can be swapped to OnHit if we
    //want the retreat to depend on damage actually being dealt (e.g. parries should not count).
    private void HandleHitboxContact(Collider other)
    {
        diveLandedHit = true;
    }

    private IEnumerator AttackSequence(Vector3 targetPosition)
    {
        isAttacking = true;
        diveLandedHit = false;

        //snapshot the pre-swoop position and altitude for use in the resolve phase
        Vector3 preSwoopPosition = transform.position;
        float preSwoopAltitude = preSwoopPosition.y;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Windup started. Target: {targetPosition}, Anchor: {preSwoopPosition}", this);

        //windup: hover in place and face the target
        yield return WindupPhase(targetPosition, windupDuration);

        //commit: lock in the dive endpoint, extending past the target so we pass through it
        Vector3 diveDirection = (targetPosition - transform.position).normalized;
        Vector3 diveEndpoint = targetPosition + diveDirection * divePastDistance;

        if (debugMode)
            Debug.Log($"[SwoopAttack] Diving to {diveEndpoint}", this);

        //subscribe to contact for the duration of the dive so we can branch on hit vs. miss.
        //OnContact fires on any layer-matching collision, which is what we need while the
        //player doesn't yet have an IDamageable component.
        hitbox.OnContact += HandleHitboxContact;

        //activate the hitbox for the duration of the dive
        DamageInfo damageInfo = new DamageInfo(damage, transform.position, diveDirection, gameObject);
        hitbox.Activate(damageInfo);

        //dive
        yield return DivePhase(diveEndpoint);

        //hitbox off and unsubscribe as soon as the dive ends - resolution movement should not damage
        hitbox.Deactivate();
        hitbox.OnContact -= HandleHitboxContact;

        //resolve: on hit, fly back to the pre-swoop position. on miss, rise straight up.
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
        //break out the moment a hit is registered so the bat doesn't push through
        //the player for the rest of the dive distance. the resolve phase then runs
        //immediately, sending the bat back to its pre-swoop position.
        while (Vector3.Distance(transform.position, endpoint) > 0.1f)
        {
            if (diveLandedHit) yield break;

            Vector3 moveDir = (endpoint - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * diveSpeed * Time.deltaTime;

            if (rb != null)
                rb.MovePosition(newPos);
            else
                transform.position = newPos;

            yield return null;
        }
    }

    //fly back to the snapshotted pre-swoop position; used when the dive landed a hit
    private IEnumerator ReturnToPreSwoopPhase(Vector3 preSwoopPosition)
    {
        while (Vector3.Distance(transform.position, preSwoopPosition) > 0.1f)
        {
            Vector3 moveDir = (preSwoopPosition - transform.position).normalized;
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

    //rise straight up from the dive's end position to the pre-swoop altitude
    //while turning back to face the snapshotted target. used on a miss.
    //facing the snapshot point lets the vision sensor re-acquire the player
    //naturally if they're still nearby - the behaviour keeps pestering without
    //any special-case logic in the bat itself.
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
            //rise toward target altitude
            if (stillRising)
            {
                float yStep = Mathf.MoveTowards(transform.position.y, targetAltitude, riseSpeed * Time.deltaTime);
                Vector3 newPos = new Vector3(transform.position.x, yStep, transform.position.z);

                if (rb != null)
                    rb.MovePosition(newPos);
                else
                    transform.position = newPos;

                if (Mathf.Abs(transform.position.y - targetAltitude) <= 0.1f) stillRising = false;
            }

            //turn to face the snapshot position concurrently with the rise
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
