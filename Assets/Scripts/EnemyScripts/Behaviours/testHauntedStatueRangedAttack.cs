using UnityEngine;

public class testHauntedStatueRangedAttack : MonoBehaviour
{
    private enum EnemyState { Idle, Aggro }

    //stores the vision sensor and gaze pivot references
    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;
    [SerializeField] private Transform gazePivot;

    [Header("Ranged Attack")]
    [SerializeField] private RangedAttack rangedAttack;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float aimToleranceDegrees = 8f;

    //controls how fast the statue turns
    [Header("Turn Speed")]
    [SerializeField] private float turnSpeed = 180f;

    //current idle or chase state
    private EnemyState currentState = EnemyState.Idle;
    private bool isPlayerVisible;

    //attack timer
    private float cooldownTimer;

    //cache references before play starts
    private void Awake()
    {
        ResolveVisionReference();
        ResolveGazePivot();
        vision.AcquirePlayerTarget();

        // If no explicit launchPoint assigned, default to the gazePivot.
        if (launchPoint == null)
        {
            launchPoint = gazePivot;
        }

        // Try to auto-resolve a RangedAttack component on the same GameObject if none assigned.
        if (rangedAttack == null)
        {
            rangedAttack = GetComponent<RangedAttack>();
        }
    }

    //update the statue state each frame
    private void Update()
    {
        if (vision == null)
        {
            EnterIdleState();
            return;
        }

        // tick cooldown
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        vision.AcquirePlayerTarget();

        if (!vision.HasTarget)
        {
            EnterIdleState();
            return;
        }

        //refresh whether the player is visible
        // changed to use IsTargetDetected() so the statue can also chase when player is within chaseDistance
        isPlayerVisible = IsPlayerDetected();
        //choose idle or chase
        UpdateBehaviourState();
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

            case EnemyState.Aggro:
                PerformAggro();
                break;
        }
    }

    //switch back to idle
    private void EnterIdleState()
    {
        currentState = EnemyState.Idle;
    }

    //switch into chase mode
    private void EnterAggroState()
    {
        currentState = EnemyState.Aggro;
    }

    //use sight to decide the state
    private void UpdateBehaviourState()
    {
        if (isPlayerVisible)
        {
            if (currentState != EnemyState.Aggro)
            {
                EnterAggroState();
            }

            return;
        }

        if (currentState != EnemyState.Idle)
        {
            EnterIdleState();
        }
    }

    //check if the player is visible or in chase range
    private bool IsPlayerDetected()
    {
        return vision != null && vision.IsTargetDetected();
    }

    //idle state
    private void PerformIdle()
    {
    }

    //chase state - rotate to face player and fire when aimed
    private void PerformAggro()
    {
        if (gazePivot == null || vision == null || vision.Target == null)
        {
            return;
        }

        Vector3 lookTarget = vision.Target.position;
        Vector3 direction = lookTarget - gazePivot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        gazePivot.rotation = Quaternion.RotateTowards(gazePivot.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // Fire when roughly aimed at the player and cooldown elapsed
        if (CanFireAtTarget(direction))
        {
            TryFire();
        }
    }

    private bool CanFireAtTarget(Vector3 directionToTarget)
    {
        if (rangedAttack == null)
        {
            return false;
        }

        // compute angle between pivot forward and the direction to target
        Vector3 forward = gazePivot.forward;
        forward.y = 0f;
        Vector3 aimDir = directionToTarget.normalized;
        float angle = Vector3.Angle(forward, aimDir);

        return angle <= Mathf.Abs(aimToleranceDegrees) && cooldownTimer <= 0f;
    }

    private void TryFire()
    {
        if (rangedAttack == null)
        {
            Debug.LogWarning("testHauntedStatueRangedAttack: RangedAttack component not assigned.");
            return;
        }

        // Use explicit launchPoint if set, otherwise use gazePivot
        Transform usedLaunch = launchPoint != null ? launchPoint : gazePivot;
        if (usedLaunch == null)
        {
            Debug.LogWarning("testHauntedStatueRangedAttack: No valid launch point to fire from.");
            return;
        }

        rangedAttack.PerformAttack(usedLaunch);
        cooldownTimer = attackCooldown;
    }

    //find or add the sensor
    private void ResolveVisionReference()
    {
        if (vision == null)
        {
            vision = GetComponent<EnemyVisionSensor>();
        }

        if (vision == null)
        {
            vision = gameObject.AddComponent<EnemyVisionSensor>();
        }
    }

    //default the gaze pivot to this object
    private void ResolveGazePivot()
    {
        if (gazePivot == null)
        {
            gazePivot = transform;
        }
    }
}