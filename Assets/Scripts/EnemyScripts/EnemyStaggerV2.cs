using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyStaggerV2 : MonoBehaviour
{
    //short stun duration for this iteration
    [Header("Stagger Settings")]
    [SerializeField] private float staggerDuration = 0.5f; 
    [SerializeField] private bool debugMode = false;

    public delegate void OnStaggerEndHandler();
    public event OnStaggerEndHandler OnStaggerEnd;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private StaggerColorEffect colorEffect;
    private bool isStaggered = false;
    private int totalWeakpoints = 0;
    private int weakpointsDestroyed = 0;
    private Coroutine staggerCoroutine;

    private void Awake()
    {
        //cache the main movement components
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        colorEffect = GetComponent<StaggerColorEffect>();

        //find the weakpoints on the enemies
        WeakPointManager manager = GetComponentInChildren<WeakPointManager>();
        if (manager != null)
        {
            InitializeStagger(manager);
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[EnemyStaggerV2] No WeakPointManager found on {gameObject.name}. Stagger system disabled.", gameObject);
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
                Debug.Log($"[EnemyStaggerV2] {gameObject.name} has {totalWeakpoints} weakpoint(s). Stagger mechanic disabled.", gameObject);
            enabled = false;
            return;
        }

        if (debugMode)
            Debug.Log($"[EnemyStaggerV2] {gameObject.name} initialized: {totalWeakpoints} total weakpoints. Stagger triggers on every weakpoint hit.", gameObject);
    }

    public void OnWeakPointHit()
    {
        if (!enabled || totalWeakpoints <= 1)
            return;

        weakpointsDestroyed++;

        if (debugMode)
            Debug.Log($"[EnemyStaggerV2] {gameObject.name}: {weakpointsDestroyed}/{totalWeakpoints} weakpoints destroyed. Triggering stagger!", gameObject);

        //v2: stun on every weakpoint hit
        TriggerStagger();
    }

    public void OnWeakPointDestroyed()
    {
        //compatibility path: destroyed weakpoints are still valid hits
        OnWeakPointHit();
    }

    //stagger disables enemy movement for a set duration
    private void TriggerStagger(float duration = -1f)
    {
        if (duration <= 0) duration = staggerDuration; 
        
        if (debugMode)
            Debug.Log($"[EnemyStaggerV2] {gameObject.name} is STAGGERED!", gameObject);

        //stops stagger coroutine from running 
        if (staggerCoroutine != null)
            StopCoroutine(staggerCoroutine);

        staggerCoroutine = StartCoroutine(PerformStagger(duration));
    }

    //handles duration and recovery
    private IEnumerator PerformStagger(float duration)
    {
        isStaggered = true;

        DisableMovement();

        //wait for the short duration to end
        yield return new WaitForSeconds(duration);
       
        EnableMovement();
        isStaggered = false;

        if (debugMode)
            Debug.Log($"[EnemyStaggerV2] {gameObject.name} recovered from stagger", gameObject);

        OnStaggerEnd?.Invoke();
    }

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

        //apply stagger color effect
        if (colorEffect != null)
        {
            colorEffect.ApplyStaggerColor();
        }
    }

    private void EnableMovement()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        //restore original sprite color
        if (colorEffect != null)
        {
            colorEffect.RestoreOriginalColor();
        }
    }

    //returns whether the enemy is staggered
    public bool IsStaggered => isStaggered;

    //returns number of destroyed weakpoints
    public int WeakpointsDestroyed => weakpointsDestroyed;
}
