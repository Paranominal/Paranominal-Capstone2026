using System.Collections;
using UnityEngine;

/// Summary:
/// Choreographs a telegraphed area attack by spawning two prefabs in sequence:
///   1. DangerZone   - visual telegraph, shown for telegraphDuration
///   2. AoEStrike    - the actual attack with its own visual, hitbox, and damage
/// AoEAttack itself has no opinion about visuals, hitbox shape, or damage values - those live on the spawned prefabs. To create a new attack flavor (lightning,
/// spikes, fire, explosion, etc), make a new AoEStrike prefab and assign it.
/// The fairness contract: the strike spawns at the snapshotted target position, not at a re-tracked one. The player's window to dodge is the telegraph duration.

public class AoEAttack : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Visual telegraph spawned at the target position during the windup.")]
    [SerializeField] private DangerZone dangerZonePrefab;

    [Tooltip("The actual attack spawned at the same position after the telegraph completes. " +
             "Swap this prefab to change the attack flavor (lightning, spikes, fire, etc).")]
    [SerializeField] private AoEStrike strikePrefab;

    [Header("Timing")]
    [Tooltip("How long the danger zone is shown before the strike spawns. Main 'feel' knob.")]
    [SerializeField] private float telegraphDuration = 1.2f;
    public float TelegraphDuration => telegraphDuration;

    [Tooltip("Brief pause after the strike spawns before the attack is considered finished. " +
             "The strike itself manages its own lifetime independently.")]
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Telegraph")]
    [Tooltip("Visual radius passed to the DangerZone for sizing. Should roughly match the " +
             "strike prefab's hitbox so the telegraph reads honestly.")]
    [SerializeField] private float telegraphRadius = 2f;

    [Header("Targeting")]
    [Tooltip("Below this player speed (units/sec), the player is considered stationary " +
             "and the AoE is offset back toward the caster instead of leading them.")]
    [SerializeField] private float stationaryThreshold = 0.5f;

    [Tooltip("When the target is stationary, distance to offset the AoE back toward the caster. " +
             "Keeps the danger zone visible past the player's body.")]
    [SerializeField] private float towardCasterOffset = 1.0f;

    [Tooltip("When the target is moving, how many seconds of velocity to project ahead. " +
             "Small values feel like a gentle bias; larger values feel like the caster is reading minds.")]
    [SerializeField] private float leadTime = 0.3f;

    [Tooltip("Maximum random scatter radius applied to the targeted position when the target is moving. " +
             "Should be similar to or larger than typical lead distance so leading reads as bias, not prediction.")]
    [SerializeField] private float scatterRadius = 1.5f;

    [Tooltip("How far back to sample the target's position when estimating velocity. " +
             "Larger windows are smoother but more laggy.")]
    [SerializeField] private float velocityWindowDuration = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isAttacking;
    private DangerZone activeZone;
    private Coroutine attackRoutine;

    // rolling sample for estimating the target's velocity over a short window
    private Transform trackedTarget;
    private Vector3 sampledPosition;
    private float sampleTime = float.NegativeInfinity;

    public bool IsAttacking => isAttacking;

    private void Update()
    {
        // keep the velocity sample fresh on whatever target was last requested. this means by the time PerformAttack(target) is called, we already have
        // a valid velocity estimate rather than only starting to sample at commit.
        if (trackedTarget == null) return;

        if (Time.time - sampleTime >= velocityWindowDuration)
        {
            sampledPosition = trackedTarget.position;
            sampleTime = Time.time;
        }
    }

    /// Begin the telegraph -> strike sequence at the given world position. Use this overload when the caller wants direct control over the AoE position (trap tiles, predetermined attack points, etc).

    public void PerformAttack(Vector3 targetPosition)
    {
        if (isAttacking)
        {
            if (debugMode)
                Debug.LogWarning($"[AoEAttack] PerformAttack called while already attacking. Ignored.", this);
            return;
        }

        if (strikePrefab == null)
        {
            Debug.LogError($"[AoEAttack] No AoEStrike prefab assigned on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(targetPosition));
    }

    /// Begin the telegraph -> strike sequence aimed at the given target. The actual snapshot position is computed using the targeting rules (stationary offset, lead, scatter).
    /// This is the typical entry point for enemy behaviours.
    public void PerformAttack(Transform target)
    {
        if (target == null)
        {
            Debug.LogError($"[AoEAttack] PerformAttack(Transform) called with null target on {gameObject.name}.", this);
            return;
        }

        //ensure we have at least one velocity sample before computing the position
        BeginTracking(target);

        Vector3 snapshotPos = ComputeAttackPosition(target);
        PerformAttack(snapshotPos);
    }

    /// Start sampling the target's position so velocity estimates are ready by attack time. Call this from the behaviour as soon as a target is acquired (optional - PerformAttack
    /// will also begin tracking on its own, but the first attack will lack velocity history).
    public void BeginTracking(Transform target)
    {
        if (trackedTarget == target) return;

        trackedTarget = target;
        sampledPosition = target.position;
        sampleTime = Time.time;
    }

    // compute the snapshot position using the targeting rules. branches on player speed: toward-caster offset for stationary, lead+scatter for moving.
    private Vector3 ComputeAttackPosition(Transform target)
    {
        Vector3 targetPos = target.position;
        Vector3 velocity = EstimateVelocity(targetPos);
        velocity.y = 0f;
        float speed = velocity.magnitude;

        Vector3 result = speed < stationaryThreshold
            ? ComputeStationaryPosition(targetPos)
            : ComputeMovingPosition(targetPos, velocity);

        if (debugMode)
            Debug.Log($"[AoEAttack] Target speed: {speed:F2} -> snapshot: {result}", this);

        return result;
    }

    private Vector3 EstimateVelocity(Vector3 currentPos)
    {
        float elapsed = Time.time - sampleTime;
        if (elapsed <= 0.0001f) return Vector3.zero;
        return (currentPos - sampledPosition) / elapsed;
    }

    // offset the AoE back toward the caster on the horizontal plane, so the danger zone is visible past the player's body and "back away from caster" is a consistent dodge.
    private Vector3 ComputeStationaryPosition(Vector3 targetPos)
    {
        Vector3 toCaster = transform.position - targetPos;
        toCaster.y = 0f;

        //if the caster is on top of the target, no meaningful direction - just use target pos.
        if (toCaster.sqrMagnitude < 0.0001f) return targetPos;

        Vector3 offset = toCaster.normalized * towardCasterOffset;
        return targetPos + offset;
    }

    // lead the target's velocity slightly, then scatter randomly. the scatter is intentionally larger than typical lead so leading reads as bias, not prediction.
    private Vector3 ComputeMovingPosition(Vector3 targetPos, Vector3 velocity)
    {
        Vector3 lead = velocity * leadTime;

        Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
        Vector3 scatter = new Vector3(randomCircle.x, 0f, randomCircle.y);

        return targetPos + lead + scatter;
    }

    /// Cancel an in-progress attack before the strike spawns. The danger zone is removed. Strikes that have already spawned manage their own lifetime and are not cancelled.
    public void CancelAttack()
    {
        if (!isAttacking) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (activeZone != null)
        {
            activeZone.Cancel();
            activeZone = null;
        }

        isAttacking = false;

        if (debugMode)
            Debug.Log($"[AoEAttack] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Vector3 targetPosition)
    {
        isAttacking = true;

        //telegraph
        if (dangerZonePrefab != null)
        {
            activeZone = Instantiate(dangerZonePrefab, targetPosition, Quaternion.identity);
            activeZone.Show(targetPosition, telegraphRadius, telegraphDuration);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[AoEAttack] No DangerZone prefab assigned - attack will have no telegraph.", this);
        }

        if (debugMode)
            Debug.Log($"[AoEAttack] Telegraph started at {targetPosition} " +
                      $"(duration {telegraphDuration}s).", this);

        yield return new WaitForSeconds(telegraphDuration);

        // strike: spawn the AoEStrike prefab; from here it's self-managing
        AoEStrike strike = Instantiate(strikePrefab, targetPosition, Quaternion.identity);
        strike.SetSource(gameObject);
        activeZone = null;

        if (debugMode)
            Debug.Log($"[AoEAttack] Strike spawned at {targetPosition}.", this);

        // recovery - lets the behaviour's cooldown start at a sensible moment. the strike's own lifetime continues independently in the scene.
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;

        if (debugMode)
            Debug.Log($"[AoEAttack] Attack complete on {gameObject.name}.", this);
    }
}
