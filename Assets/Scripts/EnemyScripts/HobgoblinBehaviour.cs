using UnityEngine;
using UnityEngine.AI;

public class HobgoblinBehaviour : MonoBehaviour
{
    private const float MinLookDirectionSqr = 0.001f;

    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Chase")]
    [SerializeField] private float minDetectRange = 12f;
    [SerializeField] private float meleeRange = 2f;

    private Transform player;

    private void Awake()
    {
        ResolveReferences();
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
            StopNavigation();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool inMeleeRange = distanceToPlayer <= meleeRange;

        if (inMeleeRange)
        {
            StopNavigation();
            FacePlayer();
            return;
        }

        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(player.position);
        }
    }

    private void ResolveReferences()
    {
        if (vision == null)
        {
            vision = GetComponent<EnemyVisionSensor>();
        }

        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
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

    private void StopNavigation()
    {
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
    }

    private bool IsPlayerDetected()
    {
        if (vision != null)
        {
            return vision.IsTargetInVision() || vision.DistanceToTarget() <= vision.ChaseDistance;
        }

        return Vector3.Distance(transform.position, player.position) <= minDetectRange;
    }

    private void FacePlayer()
    {
        Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 lookDirection = (lookTarget - transform.position).normalized;

        if (lookDirection.sqrMagnitude > MinLookDirectionSqr)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 12f * Time.deltaTime);
        }
    }
}
