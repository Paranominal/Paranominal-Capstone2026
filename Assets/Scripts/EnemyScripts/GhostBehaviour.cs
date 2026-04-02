using UnityEngine;

public class GhostBehaviour : MonoBehaviour
{
    private const float MinVelocitySqr = 0.01f;

    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;

    [Header("Movement")]
    [SerializeField] private float floatSpeed = 4.5f;
    [SerializeField] private float chaseAcceleration = 8f;

    private Transform player;
    private Vector3 velocity;
    private bool hasDetectedPlayer;

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

        if (!hasDetectedPlayer)
        {
            hasDetectedPlayer = vision == null || vision.IsTargetInVision();
        }

        if (!hasDetectedPlayer)
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        Vector3 desiredVelocity = toPlayer.normalized * floatSpeed;
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, chaseAcceleration * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > MinVelocitySqr)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }
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
}
