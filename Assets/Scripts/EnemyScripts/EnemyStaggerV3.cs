using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyStaggerV3 : MonoBehaviour
{
    [Header("Stagger Settings")]
    [SerializeField] private float staggerDuration = 1f;
    [SerializeField] private bool debugMode = false;

    [Header("Scan Settings")]
    [SerializeField] private float scanStunDuration = 2f; 
    [SerializeField] private int maxScanStaggers = -1; 
    [SerializeField] private float scanStaggerCooldown = 1.5f; 

    public delegate void OnStaggerEndHandler();
    public event OnStaggerEndHandler OnStaggerEnd;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private EnemyBehaviourBase[] enemyBehaviours;
    private StaggerColorEffect colorEffect;
    private bool isStaggered = false;
    private bool hasBeenScanned = false;
    private int scanStaggerCount = 0;
    private float nextAllowedScanStaggerTime = 0f;
    private Coroutine staggerCoroutine;

    private void Awake()
    {
        //cache the main movement components
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyBehaviours = GetComponents<EnemyBehaviourBase>();
        colorEffect = GetComponent<StaggerColorEffect>();

    }

    //Called by the grimoire scan system to mark this enemy as scanned
    public void OnEnemyScanned()
    {
        if (Time.time < nextAllowedScanStaggerTime)
        {
            if (debugMode)
                Debug.Log($"[EnemyStaggerV3] {gameObject.name} scan stagger is on cooldown for {nextAllowedScanStaggerTime - Time.time:0.00}s.", gameObject);
            return;
        }

        if (maxScanStaggers >= 0 && scanStaggerCount >= maxScanStaggers)
        {
            if (debugMode)
                Debug.Log($"[EnemyStaggerV3] {gameObject.name} reached scan stagger limit ({maxScanStaggers}).", gameObject);
            return;
        }

        hasBeenScanned = true;
        scanStaggerCount++;
        nextAllowedScanStaggerTime = Time.time + Mathf.Max(0f, scanStaggerCooldown);

        if (debugMode)
            Debug.Log($"[EnemyStaggerV3] {gameObject.name} scanned ({scanStaggerCount})! Triggering stun.", gameObject);

        //trigger an initial stun with longer duration when scanned
        TriggerStagger(scanStunDuration);
    }

    //stagger disables enemy movement for a set duration
    private void TriggerStagger(float duration = -1f)
    {
        if (duration <= 0) duration = staggerDuration; //use default if not specified
        if (debugMode)
            Debug.Log($"[EnemyStaggerV3] {gameObject.name} is STAGGERED for {duration}s!", gameObject);

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

        //wait for the duration to end
        yield return new WaitForSeconds(duration);
       
        EnableMovement();
        isStaggered = false;

        if (debugMode)
            Debug.Log($"[EnemyStaggerV3] {gameObject.name} recovered from stagger", gameObject);

        OnStaggerEnd?.Invoke();
    }

    //disables all movement
    private void DisableMovement()
    {
        SetBehavioursEnabled(false);

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

    //re-enables movement
    private void EnableMovement()
    {
        SetBehavioursEnabled(true);

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

    //returns whether the enemy has been scanned
    public bool HasBeenScanned => hasBeenScanned;

    //returns how many scan staggers have been applied
    public int ScanStaggerCount => scanStaggerCount;

    private void SetBehavioursEnabled(bool isEnabled)
    {
        if (enemyBehaviours == null || enemyBehaviours.Length == 0)
            return;

        for (int i = 0; i < enemyBehaviours.Length; i++)
        {
            if (enemyBehaviours[i] != null)
                enemyBehaviours[i].enabled = isEnabled;
        }
    }
}
