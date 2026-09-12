// Summary:
// Committed charge attack. Snapshots the target position, winds up, then launches
// in a straight line through the snapshot. Works on both ground and flying enemies:
//   - NavMeshAgent present: charges via NavMeshAgent.Move(), geometry-constrained.
//   - No NavMeshAgent (Rigidbody): charges via velocity, SphereCast detects geometry.
//
// Hit detection can use either a DamageField prefab or the enemy's own collider via OverlapSphere.
//
// Sequence:
//   1. Snapshot target position.
//   2. Windup: face the snapshot, telegraph.
//   3. Charge: straight line through the snapshot.
//   4. Resolve: on hit, back off from the player. On miss, stop in place.
//   5. Recovery pause, then cooldown.

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAttack_Charge : EnemyAttack_Base
{
    [Header("Hit Detection")]
    [Tooltip("If enabled, uses OverlapSphere for hit detection during the charge instead of a DamageField prefab.")]
    [SerializeField] private bool useBodyCollider;
    [Tooltip("Radius used for hit detection during the charge (OverlapSphere radius or DamageField radius).")]
    [SerializeField] private float hitDetectionRadius = 0.5f;
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Height offset on the target to aim at (e.g. 1.0 for chest height).")]
    [SerializeField] private float targetHeightOffset = 1f;

    [ShowIf("useBodyCollider", false, Header = "Damage Field")]
    [Tooltip("DamageField prefab parented to the enemy during the charge.")]
    [SerializeField] private DamageField damageFieldPrefab;
    [ShowIf("useBodyCollider", false)]
    [SerializeField] private float damageFieldHeight = 1f;

    [Header("Timing")]
    [SerializeField] private float windupDuration = 0.6f;
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Tracking")]
    [Tooltip("Turn speed while facing the snapshot during windup.")]
    [SerializeField] private float windupTurnSpeed = 360f;

    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 14f;
    [Tooltip("How quickly the enemy reaches charge speed. Higher = snappier.")]
    [SerializeField] private float chargeAcceleration = 40f;
    [Tooltip("How far past the snapshotted position the charge continues on a miss.")]
    [SerializeField] private float chargePastDistance = 2f;

    [Header("Back Off")]
    [Tooltip("If enabled, the enemy retreats after landing a hit. Disable when strafe handles repositioning.")]
    [SerializeField] private bool useBackOff = true;
    [ShowIf("useBackOff")]
    [Tooltip("How far the enemy retreats from the player after landing a hit.")]
    [SerializeField] private float backOffDistance = 3f;
    [ShowIf("useBackOff")]
    [SerializeField] private float backOffSpeed = 5f;

    [Header("Damage")]
    [SerializeField] private int damage = 10;

    [Header("Collision (Flying Enemies)")]
    [Tooltip("Radius of the SphereCast used for geometry detection on non-NavMesh enemies.")]
    [SerializeField] private float collisionCastRadius = 0.5f;
    [Tooltip("Layers treated as solid geometry for SphereCast collision.")]
    [SerializeField] private LayerMask geometryLayers;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAttacking;
    private bool isWindingUp;
    private bool chargeLandedHit;
    private Coroutine attackRoutine;
    private DamageField activeDamageField;

    // cached movement references
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private bool useNavMesh;

    public override bool IsAttacking => isAttacking;
    public override bool IsWindingUp => isWindingUp;
    public override float WindupDuration => windupDuration;

    protected override void Awake()
    {
        base.Awake();
        navAgent = GetComponentInParent<NavMeshAgent>();
        rb = GetComponentInParent<Rigidbody>();
        useNavMesh = navAgent != null;
    }

    public override void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode) Debug.LogWarning($"[EnemyAttack_Charge] PerformAttack called while already attacking. Ignored.", this);
            return;
        }
        if (!useBodyCollider && damageFieldPrefab == null)
        {
            Debug.LogError($"[EnemyAttack_Charge] No DamageField prefab assigned and Use Body Collider is off on {gameObject.name}.", this);
            return;
        }
        if (target == null)
        {
            Debug.LogError($"[EnemyAttack_Charge] PerformAttack called with null target on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(target));
    }

    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        CleanupDamageField();
        ChargeStop();
        if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
            navAgent.isStopped = false;
        isAttacking = false;
        isWindingUp = false;
        InvokeAttackCancelled();
        StartCooldown(2f);
        if (debugMode) Debug.Log($"[EnemyAttack_Charge] Attack cancelled on {gameObject.name}.", this);
    }

    private void CleanupDamageField()
    {
        if (activeDamageField != null)
        {
            Destroy(activeDamageField.gameObject);
            activeDamageField = null;
        }
    }


    // Hit Detection
    // checks for IDamageable targets within the hit detection radius
    private void CheckBodyColliderHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitDetectionRadius, targetLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                DamageInfo info = new DamageInfo(damage, transform.position, transform.forward, gameObject);
                damageable.TakeDamage(info);
                chargeLandedHit = true;
                return;
            }
        }
    }


    // Attack Movement
    private void ChargeMove(Vector3 direction, float speed)
    {
        if (useNavMesh && navAgent.isOnNavMesh)
        {
            navAgent.Move(direction * speed * Time.deltaTime);
        }
        else if (rb != null)
        {
            Vector3 desiredVel = direction * speed;
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVel, chargeAcceleration * Time.deltaTime);
        }
        else
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void ChargeStop()
    {
        if (rb != null && !useNavMesh)
            rb.linearVelocity = Vector3.zero;
    }

    // smooth deceleration over a brief window instead of instant zero
    private IEnumerator Decelerate(float duration)
    {
        if (rb == null || useNavMesh) yield break;

        Vector3 startVel = rb.linearVelocity;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rb.linearVelocity = Vector3.Lerp(startVel, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
    }

    // returns true if geometry is within the next frame's movement (flying enemies only)
    private bool GeometryAhead(Vector3 direction, float speed)
    {
        if (useNavMesh) return false;
        float castDistance = speed * Time.deltaTime;
        return Physics.SphereCast(transform.position, collisionCastRadius, direction, out RaycastHit _, castDistance, geometryLayers);
    }

    // returns true if the enemy has reached or passed the endpoint along the charge direction
    private bool PastEndpoint(Vector3 chargeDirection, Vector3 endpoint)
    {
        Vector3 toEndpoint = endpoint - transform.position;
        return Vector3.Dot(toEndpoint, chargeDirection) <= 0f;
    }


    // Attack Sequence
    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;
        isWindingUp = true;
        chargeLandedHit = false;

        // snapshot the target position at chest height
        Vector3 snapshotPos = target.position + Vector3.up * targetHeightOffset;

        // disable NavAgent pathfinding during charge
        if (useNavMesh && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        InvokeWindupStart();
        if (debugMode) Debug.Log($"[EnemyAttack_Charge] Windup started. Target: {snapshotPos}", this);

        // windup: face the snapshot
        float elapsed = 0f;
        while (elapsed < windupDuration)
        {
            Vector3 lookDir = snapshotPos - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, windupTurnSpeed * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        isWindingUp = false;

        // compute charge direction and endpoint
        Vector3 chargeDirection = (snapshotPos - transform.position);
        if (useNavMesh) chargeDirection.y = 0f;
        chargeDirection.Normalize();
        Vector3 chargeEndpoint = snapshotPos + chargeDirection * chargePastDistance;

        // spawn damage field if not using body collider
        if (!useBodyCollider)
        {
            Vector3 fieldPos = transform.position + transform.forward * hitDetectionRadius;
            activeDamageField = Instantiate(damageFieldPrefab, fieldPos, transform.rotation).GetComponent<DamageField>();
            activeDamageField.transform.SetParent(transform);
            activeDamageField.DoDamageField(damage, 999f, hitDetectionRadius, damageFieldHeight, targetLayers, this);
        }

        InvokeStrikeStart();
        if (debugMode) Debug.Log($"[EnemyAttack_Charge] Charging toward {chargeEndpoint}", this);

        // charge: break on hit, endpoint reached, or geometry collision
        while (!chargeLandedHit)
        {
            if (PastEndpoint(chargeDirection, chargeEndpoint)) break;
            if (GeometryAhead(chargeDirection, chargeSpeed)) break;
            ChargeMove(chargeDirection, chargeSpeed);
            yield return null;

            // check for hit after physics step has resolved
            if (useBodyCollider)
                CheckBodyColliderHit();
            else if (activeDamageField != null && activeDamageField.hitRegistered)
                chargeLandedHit = true;
        }

        // smooth stop and clean up
        yield return Decelerate(0.15f);
        CleanupDamageField();
        InvokeStrikeEnd();

        // resolve: back off on hit if enabled, otherwise stay in place
        if (useBackOff && chargeLandedHit && target != null)
        {
            if (debugMode) Debug.Log($"[EnemyAttack_Charge] Hit landed. Backing off.", this);
            yield return BackOff(target);
        }

        // recovery
        yield return new WaitForSeconds(recoveryDuration);

        // re-enable NavAgent pathfinding
        if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
            navAgent.isStopped = false;

        isAttacking = false;
        attackRoutine = null;
        StartCooldown();
        if (debugMode) Debug.Log($"[EnemyAttack_Charge] Attack complete on {gameObject.name}.", this);
    }

    private IEnumerator BackOff(Transform target)
    {
        Vector3 awayFromTarget = (transform.position - target.position);
        awayFromTarget.y = 0f;
        if (awayFromTarget.sqrMagnitude < 0.001f) awayFromTarget = Vector3.back;
        awayFromTarget.Normalize();

        Vector3 startPos = transform.position;
        // timeout prevents infinite loops if geometry blocks the back off
        float timeout = backOffDistance / backOffSpeed + 1f;
        float timer = 0f;

        while (timer < timeout)
        {
            float moved = Vector3.Distance(transform.position, startPos);
            if (moved >= backOffDistance) break;
            ChargeMove(awayFromTarget, backOffSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return Decelerate(0.15f);
    }
}