using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class HauntedStatueBehaviour_PrototypeAoE : EnemyBehaviourBase
{
    private enum EnemyState { Idle, Chase }

    //controls how fast the statue turns
    [Header("Turn Speed")]
    [SerializeField] private float turnSpeed = 180f;

    //attack configuration
    [Header("Attack")]
    [SerializeField] private AoEAttack aoeAttack;
    [Tooltip("Time between attacks, measured from the end of one attack to the start of the next.")]
    [SerializeField] private float attackCooldown = 2.5f;
    [Tooltip("How closely the statue must be facing the player before it will commit to an attack.")]
    [SerializeField] private float aimToleranceDegrees = 10f;

    //current idle or chase state
    private EnemyState currentState = EnemyState.Idle;
    private float cooldownTimer;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        if (aoeAttack == null) aoeAttack = GetComponent<AoEAttack>();
    }

    //statue has no locomotion to freeze, so pause just resets state on resume to avoid
    //carrying a stale aim across the pause window
    protected override void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            currentState = EnemyState.Idle;
        }
    }

    //update the statue state each frame
    private void Update()
    {
        //paused or dying enemies skip all logic
        if (IsPaused || IsDying) return;

        //tick the cooldown regardless of state so it advances during idle too
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        if (!HasVisionTarget)
        {
            currentState = EnemyState.Idle;
            return;
        }

        //room-based encounters always force active targeting when a target exists
        currentState = EnemyState.Chase;
        //run the active state
        RunCurrentState();
    }

    //call the current state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                PerformIdle();
                break;

            case EnemyState.Chase:
                PerformChase();
                break;
        }
    }

    //idle state
    private void PerformIdle()
    {
    }

    //chase state - face the player and attack when aimed and ready
    private void PerformChase()
    {
        if (VisionTarget == null)
        {
            return;
        }

        //start velocity sampling early so the first attack has history to draw from
        if (aoeAttack != null) aoeAttack.BeginTracking(VisionTarget);

        Vector3 lookTarget = VisionTarget.position;
        Vector3 direction = lookTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        TryAttack(direction);
    }

    //commit to an attack if aimed at the player and the cooldown has elapsed
    private void TryAttack(Vector3 directionToTarget)
    {
        if (aoeAttack == null) return;
        if (aoeAttack.IsAttacking) return;
        if (cooldownTimer > 0f) return;

        //match the aim-tolerance pattern used in testHauntedStatueRangedAttack.cs
        Vector3 forward = transform.forward;
        forward.y = 0f;
        float angle = Vector3.Angle(forward, directionToTarget.normalized);
        if (angle > aimToleranceDegrees) return;

        //hand the target Transform to AoEAttack and let it apply its targeting rules
        //(stationary offset, lead, scatter). the danger zone still locks at commit -
        //the rules just decide *where* that committed position is.
        aoeAttack.PerformAttack(VisionTarget);
        cooldownTimer = attackCooldown;
    }
}
