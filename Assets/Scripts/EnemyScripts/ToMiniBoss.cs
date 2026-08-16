using UnityEngine;
using UnityEngine.AI;

public class ToMiniBoss : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float scaleMultiplier = 1.5f;

    [Header("Stagger & Cycle Settings")]
    [SerializeField] private int hitsToStagger = 5;
    [SerializeField] private int cyclesRequired = 3;

    [Header("Minion Spawn Settings")]
    [SerializeField] private bool enableSpawning = false; //toggle or not
    [SerializeField] private int spawnMinionsOnCycle = 1; //this is an array, 0 would mean after the first cycle
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionsToSpawn = 3;
    [SerializeField] private float spawnRadius = 3f;

    private int currentHits;
    private int currentCycle = 0;
    private bool isStaggered = false;

    private WeakPointManager wpManager;
    private EnemyKnockback knockback;
    private EnemyStagger stagger;

    //public getter setter for the toggle
    public bool EnableSpawning
    {
        get => enableSpawning;
        set => enableSpawning = value;
    }

    private void Awake()
    {
        //scaling
        transform.localScale = transform.localScale * scaleMultiplier;

        currentHits = hitsToStagger;
        wpManager = GetComponentInChildren<WeakPointManager>(true);
        stagger = GetComponentInChildren<EnemyStagger>();

        knockback = GetComponent<EnemyKnockback>();

        HideWeakpoints();

        // //monitors stagger, if it ends, hide weakpoints
        // if (stagger != null)
        // {
        //     stagger.OnStaggerEnd += StaggerTimeout;
        // }
    }

    //prevent leakage 
    private void OnDestroy()
    {
        // if (stagger != null)
        // {
        //     stagger.OnStaggerEnd -= StaggerTimeout;
        // }
    }

    private void HideWeakpoints()
    {
        if (wpManager != null)
        {
            wpManager.enabled = false;
            WeakPoint[] points = wpManager.GetComponentsInChildren<WeakPoint>(true);
            foreach (WeakPoint p in points)
            {
                p.Hide();
            }
        }
    }

    public void TakeDamage(DamageInfo info)
    {
        if (isStaggered) return;

        if (knockback != null)
        {
            knockback.ApplyKnockback();
        }

        currentHits--;
        if (currentHits <= 0)
        {
            Stagger();
        }
    }

    private void Stagger()
    {
        isStaggered = true;
        Debug.Log($"Miniboss staggered! Weakpoints exposed.");

        // gameObject.Trigger();

        if (wpManager != null)
        {
            wpManager.enabled = true;
            //add to wpm next
            wpManager.SetupWeakpoints();
        }
    }

    private void StaggerTimeout()
    {
        //handles the stagger disappearing,
        if (isStaggered)
        {
            isStaggered = false;
            currentHits = hitsToStagger;
            HideWeakpoints();
        }
    }

    //call wpm when cycles are destroyed
    public void OnCycleComplete()
    {
        currentCycle++;
        if (currentCycle >= cyclesRequired)
        {
            //death logic
            EnemyBehaviourBase behaviour = GetComponentInParent<EnemyBehaviourBase>();
            if (behaviour != null)
            {
                behaviour.Die();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            //spawn minions
            if (enableSpawning && currentCycle == spawnMinionsOnCycle)
            {
                SpawnMinions();
            }

            //reset next cycle if still alive
            isStaggered = false;
            currentHits = hitsToStagger;
            HideWeakpoints();
            Debug.Log($"Cycle {currentCycle} complete.");
        }
    }

    private void SpawnMinions()
    {
        if (minionPrefab == null)
        {
            Debug.LogWarning($"Missing minion prefab! Attach it, don't @ me :C.", gameObject);
            return;
        }

        Debug.Log($"[ToMiniBoss] Spawning {minionsToSpawn} minions on cycle {currentCycle}!");

        for (int i = 0; i < minionsToSpawn; i++)
        {
            //circle around miniboss
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 targetSpawnPos = transform.position + spawnOffset;

            //try to prevent spawning in walls by validating navs
            GameObject spawnedMinion = null;
            if (UnityEngine.AI.NavMesh.SamplePosition(targetSpawnPos, out UnityEngine.AI.NavMeshHit hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnedMinion = Instantiate(minionPrefab, hit.position, Quaternion.identity);
            }
            else
            {
                //fallback to positional if navmesh fails
                spawnedMinion = Instantiate(minionPrefab, transform.position, Quaternion.identity);
            }

            //make minion immediately target the player
            if (spawnedMinion != null)
            {
                EnemyVisionSensor visionSensor = spawnedMinion.GetComponentInChildren<EnemyVisionSensor>();
                if (visionSensor != null)
                {
                    visionSensor.AcquirePlayerTarget();
                }
            }
        }
    }
}