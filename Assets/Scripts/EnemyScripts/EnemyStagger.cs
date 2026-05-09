using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

public class EnemyStagger : MonoBehaviour
{
    [Header("Stagger Settings")]
    [SerializeField] private float staggerDuration = 2f;
    [SerializeField] private bool debugMode = false;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private bool isStaggered = false;
    private int totalWeakpoints = 0;
    private int weakpointsDestroyed = 0;
    private int staggerThreshold = 0;
    private Coroutine staggerCoroutine;

    [SerializeField] private SpriteRenderer enemySprite;

    private void Awake()
    {
        //cache the main movement components
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        //find the weakpoints on the enemies
        WeakPointManager manager = GetComponentInChildren<WeakPointManager>();
        if (manager != null)
        {
            InitializeStagger(manager);
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[EnemyStagger] No WeakPointManager found on {gameObject.name}. Stagger system disabled.", gameObject);
        }
    }

    public void InitializeStagger(WeakPointManager manager)
    {
        //gets the enemy's total weakpoints from the manager
        totalWeakpoints = manager.GetTotalWeakpoints();

        //if weakpoint < 1 disable stagger, enemies die instantly
        if (totalWeakpoints <= 1)
        {
            if (debugMode)
                Debug.Log($"[EnemyStagger] {gameObject.name} has {totalWeakpoints} weakpoint(s). Stagger mechanic disabled.", gameObject);
            enabled = false;
            return;
        }

        //calculate the poise break totalWeakpoints / 2
        staggerThreshold = Mathf.CeilToInt(totalWeakpoints / 2f);

        if (debugMode)
            Debug.Log($"[EnemyStagger] {gameObject.name} initialized: {totalWeakpoints} total weakpoints, stagger threshold: {staggerThreshold}", gameObject);
    }

    public void OnWeakPointDestroyed()
    {
        if (!enabled || totalWeakpoints <= 1)
            return;

        weakpointsDestroyed++;

        if (debugMode)
            Debug.Log($"[EnemyStagger] {gameObject.name}: {weakpointsDestroyed}/{totalWeakpoints} weakpoints destroyed", gameObject);

        //coroutine to check if the threshold has been reached and the enemy isn't staggered
        if (weakpointsDestroyed == staggerThreshold && !isStaggered)
        {
            TriggerStagger();
        }
    }

    //stagger disables enemy movement for a set duration isStaggered
    private void TriggerStagger()
    {
        if (isStaggered)
            return;

        if (debugMode)
            Debug.Log($"[EnemyStagger] {gameObject.name} is STAGGERED!", gameObject);

        //stops stagger coroutine from running 
        if (staggerCoroutine != null)
            StopCoroutine(staggerCoroutine);

        staggerCoroutine = StartCoroutine(PerformStagger());
    }

    //handles duration and recovery
    private IEnumerator PerformStagger()
    {
        isStaggered = true;


        DisableMovement();

        //wait for the duration to end
        yield return new WaitForSeconds(staggerDuration);
       
        EnableMovement();
        isStaggered = false;

        if (debugMode)
            Debug.Log($"[EnemyStagger] {gameObject.name} recovered from stagger", gameObject);
    }

    //disables all movement
    private void DisableMovement()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    //re-enables movmement
    private void EnableMovement()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }
    }

    //returns whether the enemy is staggerred
    public bool IsStaggered => isStaggered;

    //returns number of destroyed weakpoints
    public int WeakpointsDestroyed => weakpointsDestroyed;
}
