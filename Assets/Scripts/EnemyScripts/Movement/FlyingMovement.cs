// Summary:
// Velocity-based flying movement for non-NavMesh enemies. Uses a non-kinematic Rigidbody with no gravity and frozen rotation. 
// Maintains hover altitude above the ground, adjusts to target altitude, and bobs vertically for visual life.
// Implements IEnemyMovement for use with Enemy.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingMovement : MonoBehaviour, IEnemyMovement
{
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 8f;
    [SerializeField] private float chaseStopDistance = 6f;
    [Tooltip("How quickly the enemy accelerates toward its target velocity. " +
             "Higher = snappier, lower = floatier.")]
    [SerializeField] private float acceleration = 10f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 3f;

    [Header("Retreat")]
    [SerializeField] private float retreatDistance = 5f;
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Strafe")]
    [SerializeField] private float strafeSpeed = 4f;
    [Tooltip("How often the enemy changes strafe direction in seconds.")]
    [SerializeField] private float strafeDirectionInterval = 2f;

    [Header("Altitude")]
    [Tooltip("Minimum height above the ground.")]
    [SerializeField] private float hoverHeight = 2.5f;
    [Tooltip("Vertical offset above the movement target's Y position.")]
    [SerializeField] private float verticalOffset = 1.5f;
    [Tooltip("Layers treated as ground for hover height raycasting.")]
    [SerializeField] private LayerMask groundLayers;
    [Tooltip("Max distance to raycast downward when finding the ground.")]
    [SerializeField] private float groundCheckDistance = 50f;

    [Header("Bobbing")]
    [Tooltip("Amplitude of the vertical bobbing motion.")]
    [SerializeField] private float bobAmplitude = 0.3f;
    [Tooltip("Speed of the bobbing oscillation.")]
    [SerializeField] private float bobFrequency = 2f;

    private Rigidbody rb;
    private Vector3 spawnPosition;
    private Vector3 targetPosition;
    private float currentSpeed;
    private float currentStopDistance = 0.5f;
    private bool hasTarget;

    // altitude management is disabled during kamikaze chase
    private bool useAltitudeManagement = true;

    // strafe
    private float strafeDirection = 1f;
    private float strafeTimer;

    public float ChaseStopDistance => chaseStopDistance;
    public bool HasReachedTarget => !hasTarget || DistanceToTarget() <= currentStopDistance;

    public void Initialize()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }


    // ==================== MOVEMENT COMMANDS ====================

    public void Chase(Vector3 target, float stopDistance)
    {
        MoveTo(target, chaseSpeed, stopDistance);
    }

    public void Strafe(Vector3 orbitCenter, float orbitRadius)
    {
        // update direction timer
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection *= -1f;
            strafeTimer = strafeDirectionInterval;
        }

        Vector3 target = ComputeStrafeTarget(orbitCenter, orbitRadius, strafeDirection);

        if (!IsStrafeClear(target))
        {
            target = ComputeStrafeTarget(orbitCenter, orbitRadius, -strafeDirection);
            if (!IsStrafeClear(target))
            {
                Stop();
                return;
            }
        }

        MoveTo(target, strafeSpeed, 0.5f);
    }

    public void BeginRetreat(Vector3 awayFrom)
    {
        Vector3 dir = (transform.position - awayFrom);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
        Vector3 retreatTarget = transform.position + dir.normalized * retreatDistance;
        MoveTo(retreatTarget, retreatSpeed, 0.5f);
    }

    public void BeginReturn()
    {
        MoveTo(spawnPosition, returnSpeed, 0.5f);
    }

    public void Stop()
    {
        hasTarget = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    public void FaceTarget(Vector3 target)
    {
        if (rb == null) return;
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            rb.MoveRotation(Quaternion.LookRotation(dir));
    }

    public void SetDirectChase(bool direct)
    {
        useAltitudeManagement = !direct;
    }

    public void SetPaused(bool paused)
    {
        if (paused) Stop();
    }


    // ==================== PHYSICS ====================

    private void FixedUpdate()
    {
        if (!hasTarget)
        {
            if (useAltitudeManagement) MaintainAltitude();
            return;
        }

        if (HasReachedTarget)
        {
            rb.linearVelocity = Vector3.zero;
            hasTarget = false;
            if (useAltitudeManagement) MaintainAltitude();
            return;
        }

        Vector3 moveTarget = useAltitudeManagement
            ? new Vector3(targetPosition.x, ComputeDesiredAltitude(), targetPosition.z)
            : targetPosition;

        Vector3 direction = (moveTarget - transform.position).normalized;
        Vector3 desiredVelocity = direction * currentSpeed;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
    }


    // ==================== INTERNALS ====================

    private void MoveTo(Vector3 target, float speed, float stopDistance)
    {
        targetPosition = target;
        currentSpeed = speed;
        currentStopDistance = stopDistance;
        hasTarget = true;
    }

    private void MaintainAltitude()
    {
        float desiredAlt = ComputeDesiredAltitude();
        float altDiff = desiredAlt - transform.position.y;
        float yVel = altDiff * acceleration;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, yVel, rb.linearVelocity.z);
    }

    private float ComputeDesiredAltitude()
    {
        float groundHeight = 0f;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers))
            groundHeight = hit.point.y;

        float minAltitude = groundHeight + hoverHeight;
        float targetAltitude = hasTarget ? targetPosition.y + verticalOffset : minAltitude;
        float baseAltitude = Mathf.Max(minAltitude, targetAltitude);
        float bob = bobAmplitude * Mathf.Sin(Time.time * bobFrequency);

        return baseAltitude + bob;
    }

    private float DistanceToTarget()
    {
        if (useAltitudeManagement)
        {
            Vector3 diff = transform.position - targetPosition;
            diff.y = 0f;
            return diff.magnitude;
        }
        return (transform.position - targetPosition).magnitude;
    }

    private Vector3 ComputeStrafeTarget(Vector3 center, float radius, float direction)
    {
        Vector3 toEnemy = transform.position - center;
        toEnemy.y = 0f;
        if (radius < 0.1f) return transform.position;

        Vector3 lateral = Vector3.Cross(Vector3.up, toEnemy.normalized) * direction;
        Vector3 aheadOnArc = transform.position + lateral * 2f;
        Vector3 fromCenter = aheadOnArc - center;
        fromCenter.y = 0f;

        return center + fromCenter.normalized * radius;
    }

    private bool IsStrafeClear(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        return !Physics.Raycast(transform.position, dir.normalized, dist, groundLayers);
    }
}
