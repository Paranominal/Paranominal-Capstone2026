using System.Collections;
using UnityEngine;

public class EyeBatBehaviour : MonoBehaviour
{
    private enum State { Patrolling, Following, Swooping }

    //stores the vision sensor reference
    [Header("References")]
    [SerializeField] private EnemyVisionSensor vision;
    
    //controls how long the bat remembers the player
    [Header("Detection")]
    [SerializeField] private float loseSightMemory = 1.2f;
    [SerializeField] private float sustainedVisionBeforeEngage = 2f;

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
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float keepDistance = 4f;

    //tunes the dive attack
    [Header("Swooping")]
    [SerializeField] private float swoopCooldown = 4f;
    [SerializeField] private float swoopSpeed = 10f;

    private State currentState = State.Patrolling;
    private Vector3 anchorPoint;
    private Vector3 currentTargetPoint;
    private float lastSeenTime = float.NegativeInfinity;
    private float nextSwoopTime;
    private bool isWaiting;
    private bool isPlayerVisible;

    //cache references and build missing parts
    private void Awake()
    {
        anchorPoint = transform.position;
        ResolveVisionReference();
        vision.AcquirePlayerTarget();
        currentTargetPoint = anchorPoint;

        //add a small collider if one is missing
        if (!TryGetComponent<SphereCollider>(out _))
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.2f;
            collider.isTrigger = false;
        }

        //add a kinematic rigidbody for wall collisions
        if (!TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    //update the current flight state each frame
    private void Update()
    {
        vision.AcquirePlayerTarget();
        if (!vision.HasTarget) return;

        Transform player = vision.Target;

        //refresh whether the player is visible
        isPlayerVisible = vision != null && vision.IsTargetInVision();
        if (isPlayerVisible) lastSeenTime = Time.time;

        //swap between patrol, follow, or swoop
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

    //switch back to patrol mode
    private void EnterPatrollingState()
    {
        currentState = State.Patrolling;
    }

    //switch into follow mode
    private void EnterFollowingState()
    {
        currentState = State.Following;
    }

    //switch into swoop mode
    private void EnterSwoopingState()
    {
        currentState = State.Swooping;
    }

    //choose the next state from sight memory
    private void UpdateBehaviourState()
    {
        if (currentState == State.Swooping) return;

        bool recentlySeen = (Time.time - lastSeenTime) <= loseSightMemory;

        if (isPlayerVisible || recentlySeen)
        {
            if (currentState != State.Following)
            {
                EnterFollowingState();
            }

            return;
        }

        if (currentState != State.Patrolling)
        {
            EnterPatrollingState();
        }
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
        Transform player = vision.Target;
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        dirFromPlayer.y = 0;
        
        Vector3 followPos = player.position + (dirFromPlayer * keepDistance) + (Vector3.up * hoverHeight);
        MoveTowards(followPos, followSpeed);
        LookAt(player.position);
    }

    //start a dive when sight has stayed stable
    private void CheckForSwoop()
    {
        if (Time.time >= nextSwoopTime && vision.IsTargetInVisionForDuration(sustainedVisionBeforeEngage))
            StartCoroutine(SwoopRoutine());
    }

    //move with the rigidbody so collisions still matter
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 moveDir = (target - transform.position).normalized;
        Vector3 newPos = transform.position + moveDir * speed * Time.deltaTime;
        
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.MovePosition(newPos);
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

    //dash at the player, then return to start
    private IEnumerator SwoopRoutine()
    {
        EnterSwoopingState();
        Vector3 startPos = transform.position;
        float elapsed = 0;
        Transform player = vision.Target;
        Rigidbody rb = GetComponent<Rigidbody>();

        while (elapsed < 0.6f && player != null)
        {
            Vector3 targetPos = player.position + Vector3.up * 0.5f;
            Vector3 moveDir = (targetPos - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * swoopSpeed * Time.deltaTime;
            
            if (rb != null)
                rb.MovePosition(newPos);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb = GetComponent<Rigidbody>();
        while (Vector3.Distance(transform.position, startPos) > 0.1f)
        {
            Vector3 moveDir = (startPos - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * swoopSpeed * 0.5f * Time.deltaTime;
            
            if (rb != null)
                rb.MovePosition(newPos);
            
            yield return null;
        }

        nextSwoopTime = Time.time + swoopCooldown;
        EnterPatrollingState();
    }

    //find or add the vision sensor
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
}