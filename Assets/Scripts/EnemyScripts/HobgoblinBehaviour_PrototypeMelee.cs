using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyVisionSensor))]
public class HobgoblinBehaviour_PrototypeMelee : EnemyBehaviourBase
{
    private enum EnemyState { Roam, Chase, Attacking }

    //stores the vision sensor and nav agent references
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;

    //patrol path settings
    [Header("Patrolling")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float idleWaitTime = 2f;

    //follow speed settings
    [Header("Following")]
    [SerializeField] private float followSpeed = 5f;

    //attack configuration
    [Header("Attack")]
    [SerializeField] private MeleeAttack meleeAttack;
    [Tooltip("Distance at which the Hobgoblin will commit to a swing. The agent stops " +
             "pursuing inside this range to avoid crowding the player.")]
    [SerializeField] private float attackRange = 2f;
    [Tooltip("Distance at which the Hobgoblin resumes pursuing after stopping. Should be " +
             "slightly larger than attackRange to prevent jittering at the boundary.")]
    [SerializeField] private float repositionRange = 2.5f;
    [Tooltip("How closely the Hobgoblin must be facing the player before committing to a swing.")]
    [SerializeField] private float aimToleranceDegrees = 30f;
    [Tooltip("Time between attacks, measured from the end of one swing to the start of the next.")]
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Turn speed used when the Hobgoblin is in melee range but not currently attacking. " +
             "Keeps it facing the player while waiting for the cooldown.")]
    [SerializeField] private float idleTurnSpeed = 360f;

    [Header("Status")]
    [SerializeField] private EnemyStagger stagger;

    //current ai state
    private EnemyState currentState = EnemyState.Roam;
    private Vector3 anchorPoint;
    private bool isWaiting;
    private bool isHolding;
    private float cooldownTimer;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        anchorPoint = transform.position;
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (stagger == null) stagger = GetComponent<EnemyStagger>();
        if (meleeAttack == null) meleeAttack = GetComponent<MeleeAttack>();
    }

    //update ai every frame
    private void Update()
    {
        //tick the cooldown regardless of state
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        if (stagger != null && stagger.IsStaggered)
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
            return;
        }

        //while attacking, the MeleeAttack owns rotation and the agent stays stopped
        if (meleeAttack != null && meleeAttack.IsAttacking)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        //the attack just finished this frame - start cooldown and resume movement
        if (currentState == EnemyState.Attacking)
        {
            cooldownTimer = attackCooldown;
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
            }
            currentState = EnemyState.Chase;
        }

        if (!HasVisionTarget)
        {
            return;
        }

        //decide whether to roam or chase
        UpdateBehaviourState();
        //run the active state
        RunCurrentState();
    }

    //call the active state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Roam:
                PerformRoam();
                break;

            case EnemyState.Chase:
                PerformChase();
                break;
        }
    }

    //use sensor detection to choose the state
    private void UpdateBehaviourState()
    {
        if (IsPlayerDetected())
        {
            currentState = EnemyState.Chase;

            return;
        }

        //returning to roam - clear the hold flag so next chase starts fresh
        isHolding = false;
        currentState = EnemyState.Roam;
    }

    //follow the patrol path on the navmesh
    private void PerformRoam()
    {
        if (isWaiting || navAgent == null || !navAgent.isOnNavMesh) return;

        navAgent.speed = patrolSpeed;

        if (!navAgent.hasPath || navAgent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    //pursue the player when out of range; hold position and face them when in range
    private void PerformChase()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        if (VisionTarget == null) return;

        Vector3 toTarget = VisionTarget.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        //hysteresis: stop pursuing inside attackRange, resume only when the player
        //gets past repositionRange. without this gap, the agent would jitter at the boundary.
        if (isHolding)
        {
            if (distance > repositionRange) isHolding = false;
        }
        else
        {
            if (distance <= attackRange) isHolding = true;
        }

        if (isHolding)
        {
            //planted: stop the agent and turn to face the player while waiting for cooldown
            navAgent.isStopped = true;
            navAgent.ResetPath();
            FacePlayer(toTarget);
        }
        else
        {
            //pursue
            navAgent.isStopped = false;
            navAgent.speed = followSpeed;
            navAgent.SetDestination(VisionTarget.position);
        }

        TryAttack();
    }

    //rotate toward the player at idleTurnSpeed while planted
    private void FacePlayer(Vector3 toTarget)
    {
        if (toTarget.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
            idleTurnSpeed * Time.deltaTime);
    }

    //commit to a swing if range, aim, and cooldown all check out
    private void TryAttack()
    {
        if (meleeAttack == null) return;
        if (meleeAttack.IsAttacking) return;
        if (cooldownTimer > 0f) return;

        Vector3 toTarget = VisionTarget.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        //range check
        if (distance > attackRange) return;

        //aim check (generous by default since we'll track during windup)
        Vector3 forward = transform.forward;
        forward.y = 0f;
        float angle = Vector3.Angle(forward, toTarget.normalized);
        if (angle > aimToleranceDegrees) return;

        //commit: stop the agent so it doesn't walk through its own swing,
        //then hand off to the MeleeAttack which will track the target during windup
        navAgent.isStopped = true;
        navAgent.ResetPath();

        meleeAttack.PerformAttack(VisionTarget);
        currentState = EnemyState.Attacking;
    }

    //pause before picking a new patrol point
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(idleWaitTime);

        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        Vector3 nextPoint = anchorPoint + new Vector3(rand.x, 0, rand.y);

        if (navAgent != null && navAgent.isOnNavMesh) navAgent.SetDestination(nextPoint);

        isWaiting = false;
    }

    //check if the player is in sight or range
    private bool IsPlayerDetected()
    {
        return SensorDetectsTarget();
    }
}