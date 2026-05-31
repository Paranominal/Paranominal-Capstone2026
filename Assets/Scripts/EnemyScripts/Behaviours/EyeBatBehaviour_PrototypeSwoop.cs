using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class EyeBatBehaviour_PrototypeSwoop : EnemyBehaviourBase
{
    private enum State { Patrolling, Following, Swooping }

    //controls how long the bat remembers the player
    [Header("Detection")]
    [SerializeField] private float detectionTimer = 3f;

    //tunes hover height and turning speed
    [Header("General Movement")]
    [SerializeField] private float hoverHeight = 2.5f;
    [SerializeField] private float turnSpeed = 180f;

    //tunes roaming movement around the anchor
    [Header("Patrolling")]
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float idleWaitTime = 2f;

    //tunes chase distance and speed
    [Header("Following")]
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float keepDistance = 4f;

    //attack configuration
    [Header("Attack")]
    [SerializeField] private SwoopAttack swoopAttack;
    [Tooltip("Time between swoops, measured from the end of one swoop to the start of the next.")]
    [SerializeField] private float swoopCooldown = 4f;

    private State currentState = State.Patrolling;
    private Vector3 anchorPoint;
    private Vector3 currentTargetPoint;
    private float nextSwoopTime;
    private bool isWaiting;

    //cached rigidbody used for movement and pause freezing
    private Rigidbody cachedRigidbody;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        anchorPoint = transform.position;
        currentTargetPoint = anchorPoint;
        if (swoopAttack == null) swoopAttack = GetComponent<SwoopAttack>();
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    //freeze rigidbody motion when paused, restore when resumed
    protected override void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            //stop any in-flight wait coroutine so it doesn't fire when resumed at the wrong time
            StopAllCoroutines();
            isWaiting = false;

            if (cachedRigidbody != null)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    //update the current flight state each frame
    private void Update()
    {
        //paused or dying enemies skip all logic
        if (IsPaused || IsDying) return;

        if (!HasVisionTarget) return;

        //while the swoop attack is running, it owns movement entirely
        if (swoopAttack != null && swoopAttack.IsAttacking)
        {
            currentState = State.Swooping;
            return;
        }

        //the swoop just finished this frame - start the cooldown
        if (currentState == State.Swooping)
        {
            nextSwoopTime = Time.time + swoopCooldown;
            currentState = State.Following;
        }

        //swap between patrol or follow
        UpdateBehaviourState();

        //run the active state logic
        RunCurrentState();
    }

    //dispatch the current state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case State.Patrolling:
                PatrolLogic();
                break;
            case State.Following:
                FollowLogic();
                CheckForSwoop();
                break;
        }
    }

    //choose the next state from current visibility
    private void UpdateBehaviourState()
    {
        if (SensorHasVision())
        {
            currentState = State.Following;
            return;
        }

        currentState = State.Patrolling;
    }

    //hover around the anchor point
    private void PatrolLogic()
    {
        if (isWaiting) return;

        //add a light vertical drift while roaming
        float bob = Mathf.Sin(Time.time * 1.5f) * 0.3f;
        Vector3 moveTarget = currentTargetPoint + Vector3.up * (hoverHeight + bob);
        MoveTowards(moveTarget, patrolSpeed);

        if (Vector3.Distance(transform.position, moveTarget) < 0.5f)
            StartCoroutine(WaitAtPatrolPoint());
    }

    //move toward the player without crowding
    private void FollowLogic()
    {
        //stay a little back from the player
        Transform player = VisionTarget;
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        dirFromPlayer.y = 0;

        Vector3 followPos = player.position + (dirFromPlayer * keepDistance) + (Vector3.up * hoverHeight);
        MoveTowards(followPos, followSpeed);
        LookAt(player.position);
    }

    //start a dive when sight has stayed stable
    private void CheckForSwoop()
    {
        if (swoopAttack == null) return;
        if (Time.time < nextSwoopTime) return;
        if (!SensorHasVisionForDuration(detectionTimer)) return;

        //snapshot the player's position at the moment of commit - this is the
        //fairness contract: the dive target will not move after this point.
        //SwoopAttack handles its own pre-swoop position snapshot internally and
        //decides where to retreat to based on hit vs. miss.
        Vector3 snapshotPos = VisionTarget.position;

        swoopAttack.PerformAttack(snapshotPos);
        currentState = State.Swooping;
    }

    //move with the rigidbody so collisions still matter
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 moveDir = (target - transform.position).normalized;
        Vector3 newPos = transform.position + moveDir * speed * Time.deltaTime;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.MovePosition(newPos);
        }

        LookAt(target);
    }

    //turn smoothly toward the movement target
    private void LookAt(Vector3 target)
    {
        Vector3 lookDir = (target - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    //pause before picking a new roam point
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(idleWaitTime);
        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        currentTargetPoint = anchorPoint + new Vector3(rand.x, 0, rand.y);
        isWaiting = false;
    }
}
