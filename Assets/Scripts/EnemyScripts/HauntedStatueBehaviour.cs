using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class HauntedStatueBehaviour : EnemyBehaviourBase
{
    private enum EnemyState { Idle, Chase }

    //stores the vision sensor reference
    [Header("References")]

    //controls how fast the statue turns
    [Header("Turn Speed")]
    [SerializeField] private float turnSpeed = 180f;

    //current idle or chase state
    private EnemyState currentState = EnemyState.Idle;
    private bool isPlayerVisible;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
    }

    //update the statue state each frame
    private void Update()
    {
        if (!HasVisionTarget)
        {
            currentState = EnemyState.Idle;
            return;
        }

        //refresh whether the player is visible
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

            case EnemyState.Chase:
                PerformChase();
                break;
        }
    }

    //use sight to decide the state
    private void UpdateBehaviourState()
    {
        if (isPlayerVisible)
        {
            currentState = EnemyState.Chase;

            return;
        }

        currentState = EnemyState.Idle;
    }

    //check if the player is visible
    private bool IsPlayerDetected()
    {
        return SensorHasVision();
    }

    //idle state
    private void PerformIdle()
    {
    }

    //chase state
    private void PerformChase()
    {
        if (VisionTarget == null)
        {
            return;
        }

        Vector3 lookTarget = VisionTarget.position;
        Vector3 direction = lookTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}