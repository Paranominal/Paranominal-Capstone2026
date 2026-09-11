// Summary:
// Velocity-based flying movement for non-NavMesh enemies. Uses a non-kinematic Rigidbody with no gravity and frozen rotation.
// Horizontal movement (chase, strafe, retreat, return) and vertical movement (altitude correction, bobbing) are computed independently so neither starves the other's velocity budget.
// Implements IEnemyMovement for use with Enemy.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingMovement : MonoBehaviour, IEnemyMovement
{
    [Header("Chase")]
    [SerializeField] private bool chaseEnabled = true;
    [ShowIf("chaseEnabled")]
    [Tooltip("How close the enemy stops to the player. Also used as the strafe orbit radius.")]
    [SerializeField] private float engagementDistance = 6f;
    [ShowIf("chaseEnabled")]
    [SerializeField] private float chaseSpeed = 8f;
    [ShowIf("chaseEnabled")]
    [Tooltip("How quickly the enemy accelerates horizontally. Higher = snappier, lower = floatier.")]
    [SerializeField] private float acceleration = 10f;
    [ShowIf("chaseEnabled")]
    [SerializeField] private bool onlyChaseIfAttackReady;

    [ShowIf("chaseEnabled", Header = "Chase Territory")]
    [SerializeField] private bool neverGiveUpChase;
    [ShowIf("neverGiveUpChase", false)]
    [Tooltip("Max distance the enemy will chase from its spawn point.")]
    [SerializeField] private float chaseRange = 20f;

    [Header("Return")]
    [SerializeField] private bool returnToOrigin = true;
    [ShowIf("returnToOrigin")]
    [SerializeField] private float returnSpeed = 3f;

    [Header("Retreat")]
    [SerializeField] private bool retreatEnabled;
    [ShowIf("retreatEnabled")]
    [SerializeField] private float retreatDistance = 5f;
    [ShowIf("retreatEnabled")]
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Strafe")]
    [SerializeField] private bool strafeEnabled;
    [ShowIf("strafeEnabled")]
    [SerializeField] private float strafeSpeed = 4f;
    [ShowIf("strafeEnabled")]
    [Tooltip("How often the enemy changes strafe direction in seconds.")]
    [SerializeField] private float strafeDirectionInterval = 2f;

    [Header("Altitude")]
    [Tooltip("Minimum height above the ground.")]
    [SerializeField] private float hoverHeight = 2.5f;
    [Tooltip("Vertical offset above the movement target's Y position.")]
    [SerializeField] private float verticalOffset = 1.5f;
    [Tooltip("How quickly the enemy corrects its altitude. Separate from horizontal acceleration.")]
    [SerializeField] private float altitudeCorrectionSpeed = 5f;
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
    private bool isPaused;

    // strafe
    private float strafeDirection = 1f;
    private float strafeTimer;
    private bool isStrafing;

    public float EngagementDistance => engagementDistance;
    public bool ReturnEnabled => returnToOrigin;
    public bool RetreatEnabled => retreatEnabled;
    public bool StrafeEnabled => strafeEnabled;
    public bool HasReachedTarget => !hasTarget || HorizontalDistanceToTarget() <= currentStopDistance;

    public void Initialize()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }


    // Movement Commands
    public void Chase(Vector3 target)
    {
        isStrafing = false;
        MoveTo(target, chaseSpeed, engagementDistance);
    }

    public void Strafe(Vector3 orbitCenter)
    {
        isStrafing = true;

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection *= -1f;
            strafeTimer = strafeDirectionInterval;
        }

        Vector3 target = ComputeStrafeTarget(orbitCenter, engagementDistance, strafeDirection);

        if (!IsStrafeClear(target))
        {
            target = ComputeStrafeTarget(orbitCenter, engagementDistance, -strafeDirection);
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
        isStrafing = false;
        Vector3 dir = (transform.position - awayFrom);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
        Vector3 retreatTarget = transform.position + dir.normalized * retreatDistance;
        MoveTo(retreatTarget, retreatSpeed, 0.5f);
    }

    public void BeginReturn()
    {
        isStrafing = false;
        MoveTo(spawnPosition, returnSpeed, 0.5f);
    }

    public void Stop()
    {
        isStrafing = false;
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

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        if (paused) Stop();
    }

    public bool ShouldExitChase(bool playerInAggroRange)
    {
        if (neverGiveUpChase) return false;
        float distFromSpawn = (transform.position - spawnPosition).magnitude;
        return !playerInAggroRange || distFromSpawn > chaseRange;
    }

    public bool CanChase(bool anyAttackReady)
    {
        if (!chaseEnabled) return false;
        if (onlyChaseIfAttackReady && !anyAttackReady) return false;
        return true;
    }


    // Physics
    private void FixedUpdate()
    {
        if (isPaused) return;

        // vertical: altitude correction runs independently in every state
        float desiredAlt = ComputeDesiredAltitude();
        float altDiff = desiredAlt - transform.position.y;
        float yVel = Mathf.Clamp(altDiff * altitudeCorrectionSpeed, -altitudeCorrectionSpeed, altitudeCorrectionSpeed);

        if (!hasTarget)
        {
            // no target: preserve horizontal velocity (for knockback), correct altitude only
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, yVel, rb.linearVelocity.z);
            return;
        }

        // strafe skips arrival check for smooth continuous orbiting
        if (!isStrafing && HasReachedTarget)
        {
            hasTarget = false;
            rb.linearVelocity = new Vector3(0f, yVel, 0f);
            return;
        }

        // horizontal: drive toward target XZ at current speed
        Vector3 horizontalDir = new Vector3(targetPosition.x - transform.position.x, 0f, targetPosition.z - transform.position.z);
        if (horizontalDir.sqrMagnitude > 0.01f) horizontalDir.Normalize();
        Vector3 desiredHorizontalVel = horizontalDir * currentSpeed;

        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 newHorizontalVel = Vector3.MoveTowards(currentHorizontalVel, desiredHorizontalVel, acceleration * Time.fixedDeltaTime);

        // combine horizontal and vertical
        rb.linearVelocity = new Vector3(newHorizontalVel.x, yVel, newHorizontalVel.z);
    }


    // Internals
    private void MoveTo(Vector3 target, float speed, float stopDistance)
    {
        targetPosition = target;
        currentSpeed = speed;
        currentStopDistance = stopDistance;
        hasTarget = true;
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

    private float HorizontalDistanceToTarget()
    {
        Vector3 diff = transform.position - targetPosition;
        diff.y = 0f;
        return diff.magnitude;
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