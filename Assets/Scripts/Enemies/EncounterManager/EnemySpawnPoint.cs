using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Enemy Pool")]
    [SerializeField] private List<GameObject> enemyPool = new List<GameObject>();

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

    // Draws a simple gizmo for the spawn point in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
