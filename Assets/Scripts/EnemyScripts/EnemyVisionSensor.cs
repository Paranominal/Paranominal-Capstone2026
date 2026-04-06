using UnityEngine;

public class EnemyVisionSensor : MonoBehaviour
{
    //stores vision range, angle, and blockers
    [Header("Vision")]
    public float viewDistance = 14f;
    [Range(1f, 180f)] public float viewAngle = 90f;
    public float chaseDistance = 12f;
    public LayerMask visionBlockers = ~0;

    //sets the eye and target height offsets
    [Header("Vision Height")]
    public float eyeHeight = 1.6f;
    public float targetHeight = 1.2f;

    //holds the cached target and ray buffer
    private Transform target;
    private RaycastHit[] hitBuffer = new RaycastHit[16];
    private float targetSeenSince = float.NegativeInfinity;
    private bool targetVisibleLastFrame;

    //cache the player when the sensor starts
    private void Awake()
    {
        AcquirePlayerTarget();
    }

    //refresh the cached player when enabled
    private void OnEnable()
    {
        AcquirePlayerTarget();
    }

    //read-only access to the cached target
    public Transform Target => target;

    public float ChaseDistance => chaseDistance;

    public bool HasTarget => target != null;

    //treat sight or close range as detection
    public bool IsTargetDetected()
    {
        return IsTargetInVision() || DistanceToTarget() <= chaseDistance;
    }

    //find the player by tag
    public void AcquirePlayerTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
#if UNITY_EDITOR
            Debug.Log($"EnemyVisionSensor: Acquired player target '{player.name}'.");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("EnemyVisionSensor: No GameObject found with tag 'Player'. Make sure the player is tagged correctly.");
#endif
        }
    }

    //measure distance to the cached target
    public float DistanceToTarget()
    {
        AcquirePlayerTarget();

        if (target == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(target.position, transform.position);
    }

    //check if the target is inside vision
    public bool IsTargetInVision()
    {
        AcquirePlayerTarget();

        bool isVisible = EvaluateTargetInVision();
        UpdateVisibilityTimer(isVisible);
        return isVisible;
    }

    //require vision to stay valid for a while
    public bool IsTargetInVisionForDuration(float requiredDuration)
    {
        AcquirePlayerTarget();

        bool isVisible = EvaluateTargetInVision();
        UpdateVisibilityTimer(isVisible);

        if (!isVisible)
        {
            return false;
        }

        return Time.time - targetSeenSince >= requiredDuration;
    }

    //test distance, angle, then line of sight
    private bool EvaluateTargetInVision()
    {
        if (target == null)
        {
            AcquirePlayerTarget();
        }

        if (target == null)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        Vector3 toTarget = targetPos - origin;
        float sqrDistance = toTarget.sqrMagnitude;

        //ignore targets that are too far away
        if (sqrDistance > viewDistance * viewDistance)
        {
            return false;
        }

        //ignore targets outside the view angle
        float angleToTarget = Vector3.Angle(transform.forward, toTarget.normalized);
        if (angleToTarget > viewAngle * 0.5f)
        {
            return false;
        }

        //ignore targets hidden behind blockers
        float distanceToTarget = Mathf.Sqrt(sqrDistance);
        Vector3 directionToTarget = toTarget / distanceToTarget;
        int hitCount = RaycastToTarget(origin, directionToTarget, distanceToTarget);

        if (hitCount == 0)
        {
            return true;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = hitBuffer[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            //skip our own colliders
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            //skip the player's colliders
            if (hitTransform == target || hitTransform.IsChildOf(target))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    //remember when the target was last visible
    private void UpdateVisibilityTimer(bool isVisible)
    {
        if (isVisible)
        {
            if (!targetVisibleLastFrame)
            {
                targetSeenSince = Time.time;
            }
        }
        else
        {
            targetSeenSince = float.NegativeInfinity;
        }

        targetVisibleLastFrame = isVisible;
    }

    //raycast with a reusable hit buffer
    private int RaycastToTarget(Vector3 origin, Vector3 direction, float distance)
    {
        int hitCount = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, visionBlockers, QueryTriggerInteraction.Ignore);

        if (hitCount == hitBuffer.Length)
        {
            hitBuffer = new RaycastHit[hitBuffer.Length * 2];
            hitCount = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, visionBlockers, QueryTriggerInteraction.Ignore);
        }

        return hitCount;
    }

    // Visualize view cone in editor to help debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Gizmos.DrawWireSphere(origin, viewDistance);

        Quaternion leftRot = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.rotation;
        Quaternion rightRot = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.rotation;
        Vector3 leftDir = leftRot * Vector3.forward * viewDistance;
        Vector3 rightDir = rightRot * Vector3.forward * viewDistance;
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawLine(origin, origin + leftDir);
        Gizmos.DrawLine(origin, origin + rightDir);
    }
}