using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyArenaSpawner : MonoBehaviour
{
    public enum SpawnTimingMode
    {
        OverWaveDuration,
        SimultaneousAtWaveStart
    }

    public enum SpawnLocationMode
    {
        SpawnPoints,
        RandomWithinRadius
    }

    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject enemyPrefab;
        public int cost = 1;
        public int minimumWave = 1;
        public float spawnWeight = 1f;
    }

    [Header("Enemy Pool")]
    [SerializeField] private List<EnemyEntry> enemyPool = new List<EnemyEntry>();

    [Header("Wave Settings")]
    [SerializeField] private int startingWave = 1;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] private int budgetPerWave = 10;
    [SerializeField] private int maxEnemiesPerWave = 50;

    [Header("Spawn Timing Settings")]
    [SerializeField] private float waveDuration = 10f;
    [SerializeField] private float timeBetweenWaves = 3f;
    [SerializeField] private SpawnTimingMode spawnTimingMode = SpawnTimingMode.OverWaveDuration;

    [Header("Spawn Location Settings")]
    [SerializeField] private SpawnLocationMode spawnLocationMode = SpawnLocationMode.SpawnPoints;
    [SerializeField] private Transform[] spawnLocations;
    [SerializeField] private bool randomiseSpawnPoint = true;
    [SerializeField] private float randomSpawnRadius = 10f;
    [SerializeField] private LayerMask groundLayer;

    private int currentWave;
    private int nextSpawnPointIndex;

    private readonly List<EnemyEntry> enemiesToSpawn = new List<EnemyEntry>();
    private readonly List<EnemyController> spawnedEnemies = new List<EnemyController>();

    // Builds the first wave when the scene begins.
    private void Start()
    {
        if (!ValidateSpawnerSetup())
        {
            enabled = false;
            return;
        }

        currentWave = startingWave - 1;
        StartCoroutine(WaveLoopRoutine());
    }

    // Removes any destroyed enemy references that still remain in the live list.
    private void Update()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

    // Draws the random spawn radius in the Scene view when that mode is enabled.
    private void OnDrawGizmosSelected()
    {
        if (spawnLocationMode == SpawnLocationMode.RandomWithinRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, randomSpawnRadius);
        }
    }

    // Repeats the full wave cycle of generating, spawning, waiting, and advancing.
    private IEnumerator WaveLoopRoutine()
    {
        while (currentWave < maxWaves)
        {
            yield return StartCoroutine(RunSingleWaveRoutine());
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    // Handles one full wave from generation through to completion.
    private IEnumerator RunSingleWaveRoutine()
    {
        currentWave++;

        GenerateWave();

        if (enemiesToSpawn.Count == 0)
        {
            Debug.LogWarning($"Wave {currentWave} could not generate any enemies.");
            yield break;
        }

        if (spawnTimingMode == SpawnTimingMode.SimultaneousAtWaveStart)
        {
            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                SpawnEnemy(enemiesToSpawn[i]);
            }
        }
        else
        {
            float spawnInterval = waveDuration / enemiesToSpawn.Count;

            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                SpawnEnemy(enemiesToSpawn[i]);

                if (i < enemiesToSpawn.Count - 1)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }

        while (spawnedEnemies.Count > 0)
        {
            spawnedEnemies.RemoveAll(enemy => enemy == null);
            yield return null;
        }
    }

    // Generates the enemy list for the current wave based on a wave budget.
    private void GenerateWave()
    {
        enemiesToSpawn.Clear();

        int waveBudget = currentWave * budgetPerWave;
        int generatedEnemyCount = 0;

        while (waveBudget > 0 && generatedEnemyCount < maxEnemiesPerWave)
        {
            List<EnemyEntry> affordableEnemies = GetAffordableEnemies(waveBudget);

            if (affordableEnemies.Count == 0)
            {
                break;
            }

            EnemyEntry selectedEnemy = GetWeightedRandomEnemy(affordableEnemies);

            if (selectedEnemy == null)
            {
                break;
            }

            enemiesToSpawn.Add(selectedEnemy);
            waveBudget -= selectedEnemy.cost;
            generatedEnemyCount++;
        }
    }

    // Returns all enemies that are unlocked for this wave and affordable within the remaining budget.
    private List<EnemyEntry> GetAffordableEnemies(int remainingWaveBudget)
    {
        List<EnemyEntry> affordableEnemies = new List<EnemyEntry>();

        for (int i = 0; i < enemyPool.Count; i++)
        {
            EnemyEntry enemyEntry = enemyPool[i];

            if (enemyEntry.enemyPrefab == null)
            {
                continue;
            }

            if (enemyEntry.cost <= 0)
            {
                continue;
            }

            if (enemyEntry.minimumWave > currentWave)
            {
                continue;
            }

            if (enemyEntry.cost > remainingWaveBudget)
            {
                continue;
            }

            affordableEnemies.Add(enemyEntry);
        }

        return affordableEnemies;
    }

    // Picks a random enemy from the provided list using spawn weight values.
    private EnemyEntry GetWeightedRandomEnemy(List<EnemyEntry> availableEnemies)
    {
        float totalWeight = 0f;

        for (int i = 0; i < availableEnemies.Count; i++)
        {
            totalWeight += Mathf.Max(0f, availableEnemies[i].spawnWeight);
        }

        if (totalWeight <= 0f)
        {
            return availableEnemies[Random.Range(0, availableEnemies.Count)];
        }

        float randomValue = Random.Range(0f, totalWeight);
        float runningWeight = 0f;

        for (int i = 0; i < availableEnemies.Count; i++)
        {
            runningWeight += Mathf.Max(0f, availableEnemies[i].spawnWeight);

            if (randomValue <= runningWeight)
            {
                return availableEnemies[i];
            }
        }

        return availableEnemies[availableEnemies.Count - 1];
    }

    // Spawns one enemy, assigns its owner spawner, and registers it as alive.
    private void SpawnEnemy(EnemyEntry enemyEntry)
    {
        if (enemyEntry == null || enemyEntry.enemyPrefab == null)
        {
            return;
        }

        if (!TryGetSpawnTransform(out Vector3 spawnPosition, out Quaternion spawnRotation))
        {
            Debug.LogWarning("EnemyWaveSpawner could not find a valid spawn location.");
            return;
        }

        GameObject spawnedObject = Instantiate(enemyEntry.enemyPrefab, spawnPosition, spawnRotation);
        EnemyController enemyController = spawnedObject.GetComponent<EnemyController>();

        if (enemyController != null)
        {
            enemyController.SetOwnerSpawner(this);
            spawnedEnemies.Add(enemyController);
        }
        else
        {
            Debug.LogWarning($"{spawnedObject.name} is missing an EnemyController component.");
        }
    }

    // Attempts to get a valid spawn position and rotation using the selected spawn location mode.
    private bool TryGetSpawnTransform(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        if (spawnLocationMode == SpawnLocationMode.RandomWithinRadius)
        {
            return TryGetRandomSpawnTransform(out spawnPosition, out spawnRotation);
        }

        return TryGetSpawnPointTransform(out spawnPosition, out spawnRotation);
    }

    // Attempts to get a spawn position and rotation from the configured spawn points.
    private bool TryGetSpawnPointTransform(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (spawnLocations == null || spawnLocations.Length == 0)
        {
            return false;
        }

        Transform selectedSpawnPoint;

        if (randomiseSpawnPoint)
        {
            selectedSpawnPoint = spawnLocations[Random.Range(0, spawnLocations.Length)];
        }
        else
        {
            selectedSpawnPoint = spawnLocations[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnLocations.Length;
        }

        if (selectedSpawnPoint == null)
        {
            return false;
        }

        spawnPosition = selectedSpawnPoint.position;
        spawnRotation = selectedSpawnPoint.rotation;
        return true;
    }

    // Attempts to get a random spawn position within the configured radius and place it on the ground and NavMesh.
    private bool TryGetRandomSpawnTransform(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * randomSpawnRadius;

            Vector3 rayStartPosition = new Vector3(
                transform.position.x + randomOffset.x,
                transform.position.y + 10f,
                transform.position.z + randomOffset.y
            );

            if (Physics.Raycast(rayStartPosition, Vector3.down, out RaycastHit groundHit, 20f, groundLayer))
            {
                if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                {
                    spawnPosition = navHit.position;
                    spawnRotation = Quaternion.identity;
                    return true;
                }
            }
        }

        return false;
    }

    // Removes a dead enemy from the active enemy list.
    public void NotifyEnemyDeath(EnemyController deadEnemy)
    {
        if (deadEnemy == null)
        {
            return;
        }

        spawnedEnemies.Remove(deadEnemy);
    }

    // Checks whether the spawner has all required data before starting.
    private bool ValidateSpawnerSetup()
    {
        if (enemyPool == null || enemyPool.Count == 0)
        {
            Debug.LogWarning("EnemyWaveSpawner is missing enemy pool entries.");
            return false;
        }

        if (spawnTimingMode == SpawnTimingMode.OverWaveDuration && waveDuration <= 0f)
        {
            Debug.LogWarning("EnemyWaveSpawner requires a wave duration greater than 0 when using timed spawning.");
            return false;
        }

        if (spawnLocationMode == SpawnLocationMode.SpawnPoints)
        {
            if (spawnLocations == null || spawnLocations.Length == 0)
            {
                Debug.LogWarning("EnemyWaveSpawner is missing spawn locations.");
                return false;
            }
        }

        if (spawnLocationMode == SpawnLocationMode.RandomWithinRadius)
        {
            if (randomSpawnRadius <= 0f)
            {
                Debug.LogWarning("EnemyWaveSpawner requires a random spawn radius greater than 0.");
                return false;
            }

            if (groundLayer == 0)
            {
                Debug.LogWarning("EnemyWaveSpawner is missing a ground layer for random spawning.");
                return false;
            }
        }

        return true;
    }
}