using System.Collections;
using UnityEngine;

public class EyeBatBehaviour : MonoBehaviour
{
    private const float MinDirectionSqr = 0.01f;

    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;

    [Header("Hover Settings")]
    [SerializeField] private float hoverDistance = 4f;
    [SerializeField] private float hoverHeight = 2.5f;
    [SerializeField] private float hoverSmoothing = 8f;

    [Header("Swoop Settings")]
    [SerializeField] private float swoopCooldown = 2.5f;
    [SerializeField] private float swoopSpeed = 12f;
    [SerializeField] private float swoopDuration = 0.45f;

    private Transform player;
    private float nextSwoopTime;
    private bool isSwooping;

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

        if (isSwooping)
        {
            return;
        }

        bool canSeePlayer = vision == null || vision.IsTargetInVision();
        if (!canSeePlayer)
        {
            return;
        }

        MoveToHoverPosition();

        if (Time.time >= nextSwoopTime)
        {
            StartCoroutine(SwoopRoutine());
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

    private void MoveToHoverPosition()
    {
        Vector3 toPlayer = transform.position - player.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < MinDirectionSqr)
        {
            toPlayer = transform.right;
        }

        Vector3 hoverDirection = toPlayer.normalized;
        Vector3 desiredPosition = player.position + hoverDirection * hoverDistance + Vector3.up * hoverHeight;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, hoverSmoothing * Time.deltaTime);

        Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookTarget);
    }

    private IEnumerator SwoopRoutine()
    {
        isSwooping = true;
        nextSwoopTime = Time.time + swoopCooldown;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = player.position + Vector3.up * 1.1f;
        float elapsed = 0f;

        while (elapsed < swoopDuration)
        {
            elapsed += Time.deltaTime;

            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, swoopSpeed * Time.deltaTime);

            if (direction.sqrMagnitude > MinDirectionSqr)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            yield return null;
        }

        transform.position = Vector3.Lerp(transform.position, startPosition, 0.35f);
        isSwooping = false;
    }
}
