using UnityEngine;

public class EnemyVisionSensor : MonoBehaviour
{
    //configs
    [Header("Vision")]
    public float viewDistance = 14f;
    [Range(1f, 180f)] public float viewAngle = 90f;
    public float chaseDistance = 12f;
    public LayerMask visionBlockers = ~0;

    [Header("Vision Height Offsets")]
    public float eyeHeight = 1.6f;
    public float targetHeight = 1.2f;

    //activate runtime env
    private Transform target;
    private RaycastHit[] hitBuffer = new RaycastHit[16];

    //read-only accessors
    public Transform Target => target;

    public float ChaseDistance => chaseDistance;

    public bool HasTarget => target != null;

    //have to manually tag the player object first for this section to grab the player entity
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
        }
    }

    //move distance calculations
    public float DistanceToTarget()
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(target.position, transform.position);
    }

    //get vision then determine the angle to see if there is anything in sight, although haven't been tested with a wall
    public bool IsTargetInVision()
    {
        if (target == null)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        Vector3 toTarget = targetPos - origin;
        float sqrDistance = toTarget.sqrMagnitude;

        if (sqrDistance > viewDistance * viewDistance)
        {
            return false;
        }

        float angleToTarget = Vector3.Angle(transform.forward, toTarget.normalized);
        if (angleToTarget > viewAngle * 0.5f)
        {
            return false;
        }

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

            //ignore own colliders
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            //ignore target colliders
            if (hitTransform == target || hitTransform.IsChildOf(target))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    //optimisation mechanism to store hits into a bugger rather than creating a new detection array each time
    private int RaycastToTarget(Vector3 origin, Vector3 direction, float distance)
    {
        int hitCount = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, visionBlockers, QueryTriggerInteraction.Ignore);

        //increase allocation in case there are nearby blockers that aren't supposed to be truncated
        if (hitCount == hitBuffer.Length)
        {
            hitBuffer = new RaycastHit[hitBuffer.Length * 2];
            hitCount = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, visionBlockers, QueryTriggerInteraction.Ignore);
        }

        return hitCount;
    }
}
