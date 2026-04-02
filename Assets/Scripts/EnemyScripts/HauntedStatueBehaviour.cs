using UnityEngine;

public class HauntedStatueBehaviour : MonoBehaviour
{
    private enum EnemyState { Idle, Chase }

    //stores the vision sensor and gaze pivot references
    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;
    [SerializeField] private Transform gazePivot;

    //controls how fast the statue turns
    [Header("Turn Speed")]
    [SerializeField] private float turnSpeed = 180f;

    //current idle or chase state
    private EnemyState currentState = EnemyState.Idle;
    private bool isPlayerVisible;

    //cache references before play starts
    private void Awake()
    {
        ResolveVisionReference();
        ResolveGazePivot();
        vision.AcquirePlayerTarget();
    }

    //update the statue state each frame
    private void Update()
    {
        if (vision == null)
        {
            EnterIdleState();
            return;
        }

        vision.AcquirePlayerTarget();

        if (!vision.HasTarget)
        {
            EnterIdleState();
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

    //switch back to idle
    private void EnterIdleState()
    {
        currentState = EnemyState.Idle;
    }

    //switch into chase mode
    private void EnterChaseState()
    {
        currentState = EnemyState.Chase;
    }

    //use sight to decide the state
    private void UpdateBehaviourState()
    {
        if (isPlayerVisible)
        {
            if (currentState != EnemyState.Chase)
            {
                EnterChaseState();
            }

            return;
        }

        if (currentState != EnemyState.Idle)
        {
            EnterIdleState();
        }
    }

    //check if the player is visible
    private bool IsPlayerDetected()
    {
        return vision != null && vision.IsTargetInVision();
    }

    //idle state
    private void PerformIdle()
    {
    }

    //chase state
    private void PerformChase()
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