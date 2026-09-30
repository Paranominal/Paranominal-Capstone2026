// Summary:
// Telegraphed area attack. Spawns a DangerZone visual telegraph, then a DamageField at the snapshotted position. The strike spawns at the committed position, not a re-tracked one. 
// The player's dodge window is the telegraph duration. Targeting rules branch on player speed: stationary targets get an offset toward the caster, moving targets get a lead + scatter. 
// This keeps the AoE readable without being trivially dodgeable.

using System.Collections;
using UnityEngine;

public class EnemyAttack_AoE : EnemyAttack_Base
{
    [Header("Damage Field")]
    [SerializeField] private DamageField damageFieldPrefab;
    [Range(0.5f, 5f)]
    [SerializeField] private float spellRadius = 2f;
    [SerializeField] private float damageFieldHeight = 0.5f;
    [SerializeField] private int damage = 10;
    [Tooltip("Number of seconds the Damage Field persists after appearing.")]
    [SerializeField] private float damageWindow = 1f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Damage Field Options")]
    [Tooltip("If enabled, the damage field stays active for its full duration after hitting instead of deactivating on first contact.")]
    [SerializeField] private bool persistAfterHit;
    [Tooltip("If enabled, deals damage repeatedly while the player stays in the field.")]
    [SerializeField] private bool damageOverTime;
    [ShowIf("damageOverTime")]
    [Tooltip("Time between damage ticks when Damage Over Time is enabled.")]
    [SerializeField] private float damageTickRate = 0.5f;

    [Header("Telegraph")]
    [Tooltip("Visual telegraph spawned at the target position during the windup.")]
    [SerializeField] private DangerZone dangerZonePrefab;

    [Header("Timing")]
    [Tooltip("How long the danger zone is shown before the strike spawns.")]
    [SerializeField] private float telegraphDuration = 1.2f;

    [Tooltip("Brief pause after the strike spawns before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Targeting")]
    [Tooltip("Below this speed (units/sec), the target is considered stationary.")]
    [SerializeField] private float stationaryThreshold = 0.5f;

    [Tooltip("When stationary, distance to offset the AoE back toward the caster.")]
    [SerializeField] private float towardCasterOffset = 1.0f;

    [Tooltip("When the target is moving, seconds of velocity to project ahead.")]
    [SerializeField] private float leadTime = 0.3f;

    [Tooltip("Max random scatter applied when the target is moving.")]
    [SerializeField] private float scatterRadius = 1.5f;

    [Tooltip("How far back to sample position when estimating velocity.")]
    [SerializeField] private float velocityWindowDuration = 0.15f;

    [Header("Usage Condition")]
    [Tooltip("If enabled, ShouldUse only returns true when the target is below stationaryThreshold. " +
             "Lets the behaviour script prefer this attack for stationary targets.")]
    [SerializeField] private bool preferStationaryTargets;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAttacking;
    private bool isWindingUp;
    private DangerZone activeZone;
    private DamageField activeDamageField;
    private Coroutine attackRoutine;

    // velocity sampling
    private Transform trackedTarget;
    private Vector3 sampledPosition;
    private float sampleTime = float.NegativeInfinity;

    public override bool IsAttacking => isAttacking;
    public override bool IsWindingUp => isWindingUp;
    public override float WindupDuration => telegraphDuration;
    public float TelegraphDuration => telegraphDuration;

    private void Update()
    {
        // keep velocity sample fresh so first attack has history
        if (trackedTarget == null) return;
        if (Time.time - sampleTime >= velocityWindowDuration)
        {
            sampledPosition = trackedTarget.position;
            sampleTime = Time.time;
        }
    }

    // start tracking early so velocity estimates are ready by attack time
    public void BeginTracking(Transform target)
    {
        if (trackedTarget == target) return;
        trackedTarget = target;
        sampledPosition = target.position;
        sampleTime = Time.time;
    }

    public override void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode) Debug.LogWarning($"[EnemyAttack_AoE] PerformAttack called while already attacking. Ignored.", this);
            return;
        }
        if (target == null)
        {
            Debug.LogError($"[EnemyAttack_AoE] PerformAttack called with null target on {gameObject.name}.", this);
            return;
        }
        if (damageFieldPrefab == null)
        {
            Debug.LogError($"[EnemyAttack_AoE] No DamageField prefab assigned on {gameObject.name}.", this);
            return;
        }

        BeginTracking(target);
        Vector3 snapshotPos = ComputeAttackPosition(target);
        attackRoutine = StartCoroutine(AttackSequence(snapshotPos));
    }

    // direct position overload for trap tiles, predetermined points, etc.
    public void PerformAttack(Vector3 targetPosition)
    {
        if (isAttacking) return;
        if (damageFieldPrefab == null) return;
        attackRoutine = StartCoroutine(AttackSequence(targetPosition));
    }

    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        if (activeZone != null) { activeZone.Cancel(); activeZone = null; }
        CleanupDamageField();
        isAttacking = false;
        isWindingUp = false;
        InvokeAttackCancelled();
        StartCooldown(2f);
        if (debugMode) Debug.Log($"[EnemyAttack_AoE] Attack cancelled on {gameObject.name}.", this);
    }

    public override bool ShouldUse(Transform target)
    {
        if (!preferStationaryTargets) return true;
        BeginTracking(target);
        Vector3 velocity = EstimateVelocity(target.position);
        velocity.y = 0f;
        return velocity.magnitude < stationaryThreshold;
    }

    private void CleanupDamageField()
    {
        if (activeDamageField != null)
        {
            Destroy(activeDamageField.gameObject);
            activeDamageField = null;
        }
    }


    // Attack Targeting
    private Vector3 ComputeAttackPosition(Transform target)
    {
        Vector3 targetPos = target.position;
        Vector3 velocity = EstimateVelocity(targetPos);
        velocity.y = 0f;
        float speed = velocity.magnitude;

        Vector3 result = speed < stationaryThreshold
            ? ComputeStationaryPosition(targetPos)
            : ComputeMovingPosition(targetPos, velocity);

        if (debugMode) Debug.Log($"[EnemyAttack_AoE] Target speed: {speed:F2} -> snapshot: {result}", this);
        return result;
    }

    private Vector3 EstimateVelocity(Vector3 currentPos)
    {
        float elapsed = Time.time - sampleTime;
        if (elapsed <= 0.0001f) return Vector3.zero;
        return (currentPos - sampledPosition) / elapsed;
    }

    private Vector3 ComputeStationaryPosition(Vector3 targetPos)
    {
        Vector3 toCaster = transform.position - targetPos;
        toCaster.y = 0f;
        if (toCaster.sqrMagnitude < 0.0001f) return targetPos;
        return targetPos + toCaster.normalized * towardCasterOffset;
    }

    private Vector3 ComputeMovingPosition(Vector3 targetPos, Vector3 velocity)
    {
        Vector3 lead = velocity * leadTime;
        Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
        Vector3 scatter = new Vector3(randomCircle.x, 0f, randomCircle.y);
        return targetPos + lead + scatter;
    }


    // Attack Sequence
    private IEnumerator AttackSequence(Vector3 targetPosition)
    {
        isAttacking = true;
        isWindingUp = true;
        InvokeWindupStart();

        // telegraph
        if (dangerZonePrefab != null)
        {
            activeZone = Instantiate(dangerZonePrefab, targetPosition, Quaternion.identity);
            activeZone.Show(targetPosition, spellRadius, telegraphDuration);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[EnemyAttack_AoE] No DangerZone prefab assigned. Attack will have no telegraph.", this);
        }

        if (debugMode) Debug.Log($"[EnemyAttack_AoE] Telegraph started at {targetPosition} (duration {telegraphDuration}s).", this);

        yield return new WaitForSeconds(telegraphDuration);

        isWindingUp = false;
        activeZone = null;

        // strike: spawn DamageField at the snapshotted position
        Vector3 spawnPos = targetPosition + (Vector3.up * damageFieldHeight * 0.5001f);
        activeDamageField = Instantiate(damageFieldPrefab, spawnPos, Quaternion.identity).GetComponent<DamageField>();
        activeDamageField.DoDamageField(damage, damageWindow, spellRadius, damageFieldHeight, targetLayers, this, persistAfterHit, damageOverTime, damageTickRate);
        InvokeStrikeStart();

        if (debugMode) Debug.Log($"[EnemyAttack_AoE] DamageField spawned at {targetPosition}.", this);

        // wait for the damage field to finish
        yield return new WaitUntil(() => activeDamageField == null || activeDamageField.attackComplete);

        CleanupDamageField();
        InvokeStrikeEnd();

        // recovery
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;
        StartCooldown();
        if (debugMode) Debug.Log($"[EnemyAttack_AoE] Attack complete on {gameObject.name}.", this);
    }
}