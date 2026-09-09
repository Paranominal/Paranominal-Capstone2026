// Summary:
// Velocity-based flying movement for non-NavMesh enemies. Uses a non-kinematic Rigidbody with no gravity and frozen rotation. 
// Maintains hover altitude above the ground, adjusts to target altitude, and bobs vertically for visual life.
// Implements IEnemyMovement for use with FlyingEnemyBehaviour.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingMovement : MonoBehaviour, IEnemyMovement
{
    [Header("Movement")]
    [Tooltip("How quickly the enemy accelerates toward its target velocity. " +
             "Higher = snappier, lower = floatier.")]
    [SerializeField] private float acceleration = 10f;

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
    private Vector3 targetPosition;
    private float currentSpeed;
    private float currentStopDistance = 0.5f;
    private bool hasTarget;

    // set by FlyingEnemyBehaviour when kamikaze chasing, disables altitude adjustments so the enemy flies directly at the target
    [HideInInspector] public bool useAltitudeManagement = true;

    public bool HasReachedTarget => !hasTarget || DistanceToTarget() <= currentStopDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void MoveTo(Vector3 target, float speed, float stopDistance)
    {
        targetPosition = target;
        currentSpeed = speed;
        currentStopDistance = stopDistance;
        hasTarget = true;
    }

    public void Stop()
    {
        hasTarget = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (!hasTarget)
        {
            // no horizontal target: maintain altitude without touching horizontal velocity
            // this allows external forces (knockback) to work while the enemy stays at hover height
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

        // when altitude management is off, fly directly at the raw target position
        Vector3 moveTarget = useAltitudeManagement
            ? new Vector3(targetPosition.x, ComputeDesiredAltitude(), targetPosition.z)
            : targetPosition;

        Vector3 direction = (moveTarget - transform.position).normalized;
        Vector3 desiredVelocity = direction * currentSpeed;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
    }

    // correct vertical position without touching horizontal velocity
    private void MaintainAltitude()
    {
        float desiredAlt = ComputeDesiredAltitude();
        float altDiff = desiredAlt - transform.position.y;
        float yVel = altDiff * acceleration;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, yVel, rb.linearVelocity.z);
    }

    private float ComputeDesiredAltitude()
    {
        // raycast down to find the ground
        float groundHeight = transform.position.y - hoverHeight; // fallback if nothing below
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers))
            groundHeight = hit.point.y;

        // never go below hover height above the ground
        float minAltitude = groundHeight + hoverHeight;

        // if we have a target, factor in its Y + vertical offset
        float targetAltitude = hasTarget ? targetPosition.y + verticalOffset : minAltitude;

        float baseAltitude = Mathf.Max(minAltitude, targetAltitude);

        // add bobbing
        float bob = bobAmplitude * Mathf.Sin(Time.time * bobFrequency);

        return baseAltitude + bob;
    }

    private float DistanceToTarget()
    {
        if (useAltitudeManagement)
        {
            // horizontal only when altitude is managed separately
            Vector3 diff = transform.position - targetPosition;
            diff.y = 0f;
            return diff.magnitude;
        }
        return (transform.position - targetPosition).magnitude;
    }
}