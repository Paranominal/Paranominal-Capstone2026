using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Enemy Pool")]
    [SerializeField] private List<GameObject> enemyPool = new List<GameObject>();

    [Header("Spawn Visuals")]
    [Tooltip("Seconds to wait before showing the spawned enemy, giving the animator time to set the spawn pose.")]
    [SerializeField] private float spawnVisualDelay = 0.05f;

    // Resizes this spawn point's enemy pool to match the spawner's maximum number of waves.
    public void ResizeEnemyPool(int maxWaves)
    {
        if (maxWaves < 0)
        {
            maxWaves = 0;
        }

        while (enemyPool.Count < maxWaves)
        {
            enemyPool.Add(null);
        }

        while (enemyPool.Count > maxWaves)
        {
            enemyPool.RemoveAt(enemyPool.Count - 1);
        }
    }

    // Spawns the enemy assigned to the requested wave and returns its EnemyBehaviourBase if one was created.
    public EnemyBehaviourBase SpawnEnemy(int currentWave, IEnemySpawner ownerSpawner)
    {
        if (currentWave <= 0)
        {
            return null;
        }

        int waveIndex = currentWave - 1;

        if (waveIndex >= enemyPool.Count)
        {
            return null;
        }

        GameObject enemyPrefab = enemyPool[waveIndex];

        if (enemyPrefab == null)
        {
            return null;
        }

        GameObject spawnedObject = Instantiate(enemyPrefab, transform.position, transform.rotation);

        // Michael edit (spawn-visual-fix): snapshot which renderers are active, hide them, then restore after a short delay so the animator can set the spawn pose.
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

        EnemyBehaviourBase enemyBehaviour = spawnedObject.GetComponent<EnemyBehaviourBase>();

        if (enemyBehaviour != null)
        {
            enemyBehaviour.SetOwnerSpawner(ownerSpawner);
            return enemyBehaviour;
        }

        Debug.LogWarning($"{spawnedObject.name} is missing an EnemyBehaviourBase component.");
        return null;
    }

    // Waits for the configured delay, then re-enables only the renderers that were originally active at instantiation.
    private IEnumerator EnableRenderersDelayed(List<Renderer> renderers)
    {
        yield return new WaitForSeconds(spawnVisualDelay);
        foreach (var r in renderers)
            if (r != null) r.enabled = true;
    }

    // Draws a simple gizmo for the spawn point in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}