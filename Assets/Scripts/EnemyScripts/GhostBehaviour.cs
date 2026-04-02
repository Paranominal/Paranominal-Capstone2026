using System.Collections;
using UnityEngine;

public class GhostBehaviour : MonoBehaviour
{
    private enum EnemyState { Roam, Chase, Search }
    //stores the vision sensor reference
    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;

    //controls how long the ghost remembers the player
    [Header("Detection")]
    [SerializeField] private float loseSightMemory = 2.0f;
    [SerializeField] private float searchDuration = 5.0f;  

    //base movement tuning
    [Header("General Movement")]
    [SerializeField] private float floatHeight = 0.6f;   
    [SerializeField] private float acceleration = 5f;     
    [SerializeField] private float turnSpeed = 120f;      

    //roam movement settings
    [Header("Wandering")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 7f;
    [SerializeField] private float idleWaitTime = 3f;

    //follow movement settings
    [Header("Following")]
    [SerializeField] private float followSpeed = 3.5f;
    [SerializeField] private float keepDistance = 2.0f;

    //current ai state
    private EnemyState currentState = EnemyState.Roam;
    private Vector3 velocity;
    private Vector3 anchorPoint;
    private Vector3 currentTargetPoint;
    private Vector3 lastKnownPos;

    private float lastSeenTime = float.NegativeInfinity;
    private bool isWaiting;
    private bool isPlayerVisible;

    //cache references before play starts
    private void Awake()
    {
        anchorPoint = transform.position;
        currentTargetPoint = anchorPoint;
        if (vision == null) vision = GetComponent<EnemyVisionSensor>();
        if (vision == null) vision = gameObject.AddComponent<EnemyVisionSensor>();
        vision.AcquirePlayerTarget();

        //make the collider a trigger so walls are ignored
        if (TryGetComponent<Collider>(out Collider col)) col.isTrigger = true;
    }

    //update the ghost state each frame
    private void Update()
    {
        vision.AcquirePlayerTarget();
        if (!vision.HasTarget) return;

        Transform player = vision.Target;

        //update last known player position
        isPlayerVisible = vision.IsTargetInVision();
        if (isPlayerVisible)
        {
            lastSeenTime = Time.time;
            lastKnownPos = player.position;
        }

        //choose roam, chase, or search
        UpdateBehaviourState();
        //run the active state
        RunCurrentState();
    }

    //call the current state behavior
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

            case EnemyState.Search:
                PerformSearch();
                break;
        }
    }

    //switch back to roaming
    private void EnterRoamState()
    {
        currentState = EnemyState.Roam;
    }

    //switch into chase mode
    private void EnterChaseState()
    {
        currentState = EnemyState.Chase;
    }

    //switch into search mode
    private void EnterSearchState()
    {
        currentState = EnemyState.Search;
    }

    //use sight memory to decide the state
    private void UpdateBehaviourState()
    {
        float timeSinceSeen = Time.time - lastSeenTime;

        if (isPlayerVisible)
        {
            if (currentState != EnemyState.Chase)
            {
                EnterChaseState();
            }

            return;
        }

        if (timeSinceSeen <= searchDuration)
        {
            if (currentState != EnemyState.Search)
            {
                EnterSearchState();
            }

            return;
        }

        if (currentState != EnemyState.Roam)
        {
            EnterRoamState();
        }
    }

    //float around the anchor point
    private void PerformRoam()
    {
        if (isWaiting) return;

        //add a small hover drift while roaming
        float bob = Mathf.Sin(Time.time * 1.2f) * 0.15f;
        Vector3 moveTarget = currentTargetPoint + Vector3.up * (floatHeight + bob);
        
        MoveTowards(moveTarget, wanderSpeed);

        if (Vector3.Distance(transform.position, moveTarget) < 0.5f)
            StartCoroutine(WaitAtWanderPoint());
    }

    //trail the player at a safe distance
    private void PerformChase()
    {
        Transform player = vision.Target;
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        Vector3 followPos = player.position + (dirFromPlayer * keepDistance) + (Vector3.up * floatHeight);
        
        MoveTowards(followPos, followSpeed);
        LookAt(player.position);
    }

    //move to the last seen spot and scan
    private void PerformSearch()
    {
        //drift toward the last known position
        float bob = Mathf.Sin(Time.time * 2f) * 0.2f;
        Vector3 searchPos = lastKnownPos + (Vector3.up * (floatHeight + bob));
        
        MoveTowards(searchPos, wanderSpeed);

        //keep turning while the ghost searches
        float angle = Mathf.Sin(Time.time) * 45f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, angle, 0), turnSpeed * Time.deltaTime);
    }

    //move using acceleration for a floaty feel
    private void MoveTowards(Vector3 target, float maxSpeed)
    {
        //ease into the desired velocity
        Vector3 toTarget = target - transform.position;
        Vector3 desiredVelocity = toTarget.normalized * maxSpeed;
        
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, acceleration * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.01f) LookAt(transform.position + velocity);
    }

    //turn toward the movement direction
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

    //pause before selecting a new roam point
    private IEnumerator WaitAtWanderPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(idleWaitTime);
        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        currentTargetPoint = anchorPoint + new Vector3(rand.x, 0, rand.y);
        isWaiting = false;
    }
}