using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class EyeBatBehaviour_PrototypeSwoop : EnemyBehaviourBase
{
    private enum State { Following, Swooping }

    //tunes hover height and turning speed
    [Header("General Movement")]
    [SerializeField] private float hoverHeight = 2.5f;
    [SerializeField] private float turnSpeed = 180f;

    //tunes chase distance and speed
    [Header("Following")]
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float followSmoothTime = 0.2f;
    [SerializeField] private float keepDistance = 4f;

    //attack configuration
    [Header("Attack")]
    [SerializeField] private SwoopAttack swoopAttack;
    [Tooltip("Time between swoops, measured from the end of one swoop to the start of the next.")]
    [SerializeField] private float swoopCooldown = 4f;

    private State currentState = State.Following;
    private float nextSwoopTime;

    // SmoothDamp requires a persistent velocity reference
    private Vector3 followVelocity;

    // cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        if (swoopAttack == null) swoopAttack = GetComponent<SwoopAttack>();
    }

    // OnPauseStateChanged: zero out follow velocity so the bat doesn't lurch when unpaused
    protected override void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
            followVelocity = Vector3.zero;
    }

    // update the current flight state each frame
    private void Update()
    {
        // paused or dying enemies skip all logic
        if (IsPaused || IsDying) return;

        if (!HasVisionTarget) return;

        // while the swoop attack is running, it owns movement entirely
        if (swoopAttack != null && swoopAttack.IsAttacking)
        {
            currentState = State.Swooping;
            return;
        }

        // the swoop just finished this frame - start the cooldown
        if (currentState == State.Swooping)
        {
            nextSwoopTime = Time.time + swoopCooldown;
            currentState = State.Following;
        }

        // run the active state logic
        RunCurrentState();
    }

    // dispatch the current state behaviour
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case State.Following:
                FollowLogic();
                CheckForSwoop();
                break;
        }
    }

    // smoothly move toward the hover position behind and above the player
    private void FollowLogic()
    {
        Transform player = VisionTarget;
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        dirFromPlayer.y = 0f;

        Vector3 followTarget = player.position + (dirFromPlayer * keepDistance) + (Vector3.up * hoverHeight);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            followTarget,
            ref followVelocity,
            followSmoothTime,
            followSpeed
        );

        LookAt(player.position);
    }

    // snapshot the player's current position and hand it off to SwoopAttack
    private void CheckForSwoop()
    {
        if (swoopAttack == null) return;
        if (Time.time >= nextSwoopTime && SensorHasVision())
        {
            currentState = State.Swooping;
            swoopAttack.PerformAttack(VisionTarget.position);
        }
    }

    // turn smoothly toward a target point on the horizontal plane
    private void LookAt(Vector3 target)
    {
        Vector3 lookDir = target - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}
