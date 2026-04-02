using UnityEngine;

public class HauntedStatueBehaviour : MonoBehaviour
{
    private const float MinAimDirectionSqr = 0.001f;

    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 16f;

    [Header("Aiming")]
    [SerializeField] private float turnSpeed = 6f;

    private Transform player;

    private void Awake()
    {
        ResolveVisionReference();
        TryAcquirePlayer();
    }

    private void Update()
    {
        if (!TryAcquirePlayer())
        {
            return;
        }

        if (!IsPlayerDetected())
        {
            return;
        }

        RotateTowardPlayer();
    }

    private void ResolveVisionReference()
    {
        if (vision == null)
        {
            vision = GetComponent<EnemyVisionSensor>();
        }
    }

    private bool TryAcquirePlayer()
    {
        if (player != null)
        {
            return true;
        }

        if (vision != null)
        {
            vision.AcquirePlayerTarget();
            player = vision.Target;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        return player != null;
    }

    private bool IsPlayerDetected()
    {
        if (vision != null)
        {
            return vision.IsTargetInVision() || vision.DistanceToTarget() <= vision.ChaseDistance;
        }

        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    private void RotateTowardPlayer()
    {
        Vector3 flatPlayerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 direction = (flatPlayerPos - transform.position).normalized;

        if (direction.sqrMagnitude <= MinAimDirectionSqr)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}
