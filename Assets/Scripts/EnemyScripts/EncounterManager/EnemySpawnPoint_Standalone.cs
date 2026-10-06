using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Summary:
// Self-managed enemy spawn point that works without an EnemyEncounterManager.
// Spawns a single enemy once, either when the player enters the detection radius (if enabled)
// or when SpawnEnemy() is called externally. Acts as the spawned enemy's owner spawner.
// EDIT (boss-doors): boss spawners lock their doors on spawn and unlock them when the boss dies.
public class EnemySpawnPoint_Standalone : MonoBehaviour, IEnemySpawner
{
    [Header("Boss Spawner")]
    [Tooltip("Does this spawn a Boss Enemy? Leave unticked if it spawns a Sub-Boss.")]
    [SerializeField] private bool isBossSpawner;
    [ShowIf("isBossSpawner")]
    [SerializeField] private Door[] bossDoors;

    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Player Detection")]
    [Tooltip("If enabled, spawns the enemy when the player comes within the detection radius. If disabled, the spawn point waits for SpawnEnemy() to be called externally.")]
    [SerializeField] private bool useRadiusDetection = false;
    [SerializeField] private float detectionRadius = 10f;

    [Header("Spawn Visuals")]
    [Tooltip("Seconds to wait before showing the spawned enemy, giving the animator time to set the spawn pose.")]
    [SerializeField] private float spawnVisualDelay = 0.05f;

    private Transform playerTransform;
    private bool hasSpawned;
    private Enemy spawnedEnemy;

    public bool HasSpawned => hasSpawned;
    public Enemy SpawnedEnemy => spawnedEnemy;

    // Keeps the detection radius from going negative.
    private void OnValidate()
    {
        if (detectionRadius < 0f)
        {
            detectionRadius = 0f;
        }
    }

    // Caches the player on load if radius detection is in use.
    private void Start()
    {
        if (useRadiusDetection)
        {
            TryFindPlayer();
        }
    }

    // Checks the player's distance each frame until the enemy has spawned.
    private void Update()
    {
        if (!useRadiusDetection || hasSpawned)
        {
            return;
        }

        // Retry in case the player wasn't loaded yet when Start ran.
        if (playerTransform == null && !TryFindPlayer())
        {
            return;
        }

        float sqrDistance = (playerTransform.position - transform.position).sqrMagnitude;

        if (sqrDistance <= detectionRadius * detectionRadius)
        {
            SpawnEnemy();
        }
    }

    // Finds and caches the player transform via the Player tag. Returns true if found.
    private bool TryFindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }

        return playerTransform != null;
    }

    // Spawns the assigned enemy if it hasn't already been spawned. Safe to call externally.
    public Enemy SpawnEnemy()
    {
        if (hasSpawned)
        {
            return null;
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] EnemySpawnPoint_Standalone has no enemy prefab assigned.");
            return null;
        }

        hasSpawned = true;

        GameObject spawnedObject = Instantiate(enemyPrefab, transform.position, transform.rotation);

        // Snapshot which renderers are active, hide them, then restore after a short delay so the animator can set the spawn pose.
        Renderer[] allRenderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
        List<Renderer> activeRenderers = new List<Renderer>();

        foreach (var r in allRenderers)
        {
            if (r.enabled)
            {
                activeRenderers.Add(r);
                r.enabled = false;
            }
        }

        StartCoroutine(EnableRenderersDelayed(activeRenderers));

        NavMeshAgent navAgent = spawnedObject.GetComponent<NavMeshAgent>();

        if (navAgent != null)
        {
            navAgent.enabled = false;
            spawnedObject.transform.position = transform.position;
            spawnedObject.transform.rotation = transform.rotation;
            navAgent.enabled = true;
            navAgent.Warp(transform.position);
        }

        Enemy enemyBehaviour = spawnedObject.GetComponent<Enemy>();

        if (enemyBehaviour == null)
        {
            Debug.LogWarning($"{spawnedObject.name} is missing an Enemy component.");
            return null;
        }

        enemyBehaviour.SetOwnerSpawner(this);
        spawnedEnemy = enemyBehaviour;

        // EDIT (boss-doors): moved above the return so it actually runs
        if (isBossSpawner) LockBossDoors();

        return enemyBehaviour;
    }

    // Waits for the configured delay, then re-enables only the renderers that were originally active at instantiation.
    private IEnumerator EnableRenderersDelayed(List<Renderer> renderers)
    {
        yield return new WaitForSeconds(spawnVisualDelay);

        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.enabled = true;
            }
        }
    }

    // Clears the spawned enemy reference when it dies.
    public void NotifyEnemyDeath(Enemy deadEnemy)
    {
        if (deadEnemy == null)
        {
            return;
        }

        if (deadEnemy == spawnedEnemy)
        {
            spawnedEnemy = null;
            if (isBossSpawner) UnlockBossDoors();
        }
    }

    // EDIT (boss-doors): door helpers, skip empty slots in the array
    private void LockBossDoors()
    {
        if (bossDoors == null) return;
        foreach (Door door in bossDoors)
        {
            if (door != null) door.BossLock();
        }
    }

    private void UnlockBossDoors()
    {
        if (bossDoors == null) return;
        foreach (Door door in bossDoors)
        {
            if (door != null) door.BossUnlock();
        }
    }

    // Draws the spawn point and, if enabled, the detection radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);

        if (useRadiusDetection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        // EDIT (boss-doors): links to the boss doors (red)
        if (isBossSpawner && bossDoors != null)
        {
            Gizmos.color = Color.red;
            foreach (Door door in bossDoors)
            {
                if (door != null) Gizmos.DrawLine(transform.position, door.transform.position);
            }
        }
    }
}