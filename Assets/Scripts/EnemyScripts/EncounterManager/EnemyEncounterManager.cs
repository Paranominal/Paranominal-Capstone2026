using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyEncounterManager : MonoBehaviour, IEnemySpawner
{
    public enum EncounterMode
    {
        Standard,
        Arena
    }

    public enum SpawnTimingMode
    {
        SimultaneousAtWaveStart,
        OverWaveDuration
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

    [Header("Encounter Mode")]
    [SerializeField] private EncounterMode encounterMode = EncounterMode.Standard;

    [Header("Wave Settings")]
    [SerializeField] private int maxWaves = 1;
    [SerializeField] private float timeBetweenWaves = 3f;

    [Header("Standard Mode - Spawn Points")]
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    [Header("Arena Mode - Enemy Pool")]
    [SerializeField] private List<EnemyEntry> enemyPool = new List<EnemyEntry>();

    [Header("Arena Mode - Wave Budget")]
    [SerializeField] private int startingWave = 1;
    [SerializeField] private int budgetPerWave = 10;
    [SerializeField] private int maxEnemiesPerWave = 50;

    [Header("Arena Mode - Spawn Timing")]
    [SerializeField] private SpawnTimingMode spawnTimingMode = SpawnTimingMode.SimultaneousAtWaveStart;
    [SerializeField] private float waveDuration = 10f;

    [Header("Arena Mode - Spawn Locations")]
    [SerializeField] private SpawnLocationMode spawnLocationMode = SpawnLocationMode.SpawnPoints;
    [SerializeField] private bool randomiseSpawnPoint = true;
    [SerializeField] private float randomSpawnRadius = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Encounter State")]
    [HideInInspector] public bool isPlayerInRoom = false;
    [SerializeField] private float resetCounter = 5f;

    [Header("Door Gating")]
    [SerializeField] private bool useDoorGating = false;
    [SerializeField] private List<Door> doors = new List<Door>();

    private bool isPlayerPastDoor = false;
    private PlayerStatus playerStatus;
    private int currentWave;
    private int nextSpawnPointIndex;
    private bool hasEncounterStarted;
    private bool hasEncounterCompleted;
    private Coroutine waveLoopCoroutine;
    private Coroutine resetCoroutine;

    private readonly List<EnemyEntry> enemiesToSpawn = new List<EnemyEntry>();
    private readonly List<EnemyBehaviourBase> spawnedEnemies = new List<EnemyBehaviourBase>();

    // Gathers child spawn points and keeps their enemy pools synced to the current maximum number of waves.
    private void OnValidate()
    {
        GatherSpawnPoints();

        if (encounterMode == EncounterMode.Standard)
        {
            SyncSpawnPointEnemyPools();
        }
    }

    // Gathers child spawn points and validates the spawner setup when the scene begins.
    private void Start()
    {
        GatherSpawnPoints();

        if (encounterMode == EncounterMode.Standard)
        {
            SyncSpawnPointEnemyPools();
        }

        if (!ValidateSpawnerSetup())
        {
            enabled = false;
            return;
        }

        currentWave = encounterMode == EncounterMode.Arena ? startingWave - 1 : 0;

        playerStatus = FindFirstObjectByType<PlayerStatus>();

        if (playerStatus == null)
            Debug.LogWarning("[EnemyEncounterManager] Could not find PlayerStatus in scene.");
    }

    // Removes any destroyed enemy references that still remain in the live list.
    private void Update()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

    // Draws the random spawn radius in the Scene view when arena mode + random radius is enabled.
    private void OnDrawGizmosSelected()
    {
        if (encounterMode == EncounterMode.Arena && spawnLocationMode == SpawnLocationMode.RandomWithinRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, randomSpawnRadius);
        }
    }

    // Sets whether the player is currently inside the encounter room.
    public void SetPlayerInRoom(bool playerInRoom)
    {
        isPlayerInRoom = playerInRoom;

        if (isPlayerInRoom)
        {
            CancelResetCounter();

            if (!hasEncounterStarted && !hasEncounterCompleted)
            {
                TryStartEncounter();
            }
            else
            {
                ResumeSpawnedEnemies();
            }
        }
        else
        {
            isPlayerPastDoor = false;

            if (hasEncounterStarted && !hasEncounterCompleted)
            {
                PauseSpawnedEnemies();
                StartResetCounter();
            }
        }
    }

    // Called by a DoorRadiusDetector when the player exits any door's radius.
    // If door gating is enabled, this satisfies the distance condition for starting the encounter.
    public void NotifyDoorRadiusExited()
    {
        if (!useDoorGating || hasEncounterStarted || hasEncounterCompleted)
        {
            return;
        }

        isPlayerPastDoor = true;
        TryStartEncounter();
    }

    // Starts the encounter if all entry conditions are satisfied.
    // Without door gating, only the room flag is required.
    // With door gating, both the room flag and the door radius flag must be set.
    private void TryStartEncounter()
    {
        if (hasEncounterStarted || hasEncounterCompleted)
        {
            return;
        }

        if (!isPlayerInRoom)
        {
            return;
        }

        if (useDoorGating && !isPlayerPastDoor)
        {
            return;
        }

        StartEncounter();
    }

    // Starts the encounter wave loop and slams/locks all registered doors.
    private void StartEncounter()
    {
        if (hasEncounterStarted)
        {
            return;
        }

        hasEncounterStarted = true;
        playerStatus?.SetInEncounter(true);

        if (useDoorGating)
        {
            LockDoors();
        }

        waveLoopCoroutine = StartCoroutine(WaveLoopRoutine());
    }

    // Slams or locks each registered door depending on its current state.
    private void LockDoors()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            Door door = doors[i];

            if (door == null)
            {
                continue;
            }

            door.StartArena();
        }
    }

    // Unlocks all registered doors once the encounter is complete and clears their encounter lock flag.
    private void UnlockDoors()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            Door door = doors[i];

            if (door == null)
            {
                continue;
            }

            door.EndArena();
        }
    }

    // Repeats the full wave cycle of spawning, waiting, and advancing until the maximum number of waves is reached.
    private IEnumerator WaveLoopRoutine()
    {
        while (currentWave < maxWaves)
        {
            yield return StartCoroutine(WaitUntilPlayerIsInRoomRoutine());
            yield return StartCoroutine(RunSingleWaveRoutine());

            if (currentWave < maxWaves)
            {
                yield return StartCoroutine(WaitForNextWaveRoutine());
            }
        }

        hasEncounterCompleted = true;
        playerStatus?.SetInEncounter(false);
        waveLoopCoroutine = null;

        if (useDoorGating)
        {
            UnlockDoors();
        }
    }

    // Handles one full wave from spawning through to completion.
    private IEnumerator RunSingleWaveRoutine()
    {
        currentWave++;

        if (encounterMode == EncounterMode.Standard)
        {
            yield return StartCoroutine(RunStandardWaveRoutine());
        }
        else
        {
            yield return StartCoroutine(RunArenaWaveRoutine());
        }
    }

    // Spawns enemies from child spawn points and waits for all of them to die.
    private IEnumerator RunStandardWaveRoutine()
    {
        SpawnStandardWave();

        while (spawnedEnemies.Count > 0)
        {
            spawnedEnemies.RemoveAll(enemy => enemy == null);
            yield return null;
        }
    }

    // Generates and spawns a budget-based wave, then waits for all enemies to die.
    private IEnumerator RunArenaWaveRoutine()
    {
        GenerateArenaWave();

        if (enemiesToSpawn.Count == 0)
        {
            Debug.LogWarning($"Wave {currentWave} could not generate any enemies.");
            yield break;
        }

        if (spawnTimingMode == SpawnTimingMode.SimultaneousAtWaveStart)
        {
            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                SpawnArenaEnemy(enemiesToSpawn[i]);
            }
        }
        else
        {
            float spawnInterval = waveDuration / enemiesToSpawn.Count;

            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                SpawnArenaEnemy(enemiesToSpawn[i]);

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

    // Waits for the player to be in the room before allowing the encounter to continue.
    private IEnumerator WaitUntilPlayerIsInRoomRoutine()
    {
        while (!isPlayerInRoom)
        {
            yield return null;
        }
    }

    // Waits between waves, but only counts time while the player is in the room.
    private IEnumerator WaitForNextWaveRoutine()
    {
        float timer = 0f;

        while (timer < timeBetweenWaves)
        {
            if (isPlayerInRoom)
            {
                timer += Time.deltaTime;
            }

            yield return null;
        }
    }

    // Spawns the current wave across all child spawn points.
    private void SpawnStandardWave()
    {
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[i];

            if (spawnPoint == null)
            {
                continue;
            }

            EnemyBehaviourBase spawnedEnemy = spawnPoint.SpawnEnemy(currentWave, this);

            if (spawnedEnemy != null)
            {
                spawnedEnemy.SetOwnerSpawner(this);
                spawnedEnemies.Add(spawnedEnemy);

                if (!isPlayerInRoom)
                {
                    spawnedEnemy.SetPaused(true);
                }
            }
        }
    }

    // Generates the enemy list for the current arena wave based on a wave budget.
    private void GenerateArenaWave()
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

    // Spawns one arena enemy, assigns its owner spawner, and registers it as alive.
    private void SpawnArenaEnemy(EnemyEntry enemyEntry)
    {
        if (enemyEntry == null || enemyEntry.enemyPrefab == null)
        {
            return;
        }

        if (!TryGetSpawnTransform(out Vector3 spawnPosition, out Quaternion spawnRotation))
        {
            Debug.LogWarning("EnemyEncounterManager could not find a valid spawn location for arena enemy.");
            return;
        }

        GameObject spawnedObject = Instantiate(enemyEntry.enemyPrefab, spawnPosition, spawnRotation);
        EnemyBehaviourBase enemyBehaviour = spawnedObject.GetComponent<EnemyBehaviourBase>();

        if (enemyBehaviour != null)
        {
            enemyBehaviour.SetOwnerSpawner(this);
            spawnedEnemies.Add(enemyBehaviour);

            if (!isPlayerInRoom)
            {
                enemyBehaviour.SetPaused(true);
            }
        }
        else
        {
            Debug.LogWarning($"{spawnedObject.name} is missing an EnemyBehaviourBase component.");
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

    // Attempts to get a spawn position and rotation from the configured child spawn points.
    private bool TryGetSpawnPointTransform(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return false;
        }

        EnemySpawnPoint selectedSpawnPoint;

        if (randomiseSpawnPoint)
        {
            selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        }
        else
        {
            selectedSpawnPoint = spawnPoints[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Count;
        }

        if (selectedSpawnPoint == null)
        {
            return false;
        }

        spawnPosition = selectedSpawnPoint.transform.position;
        spawnRotation = selectedSpawnPoint.transform.rotation;
        return true;
    }

    // Attempts to get a random spawn position within the configured radius, placed on the ground and NavMesh.
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

    // Sets all currently active spawned enemies to the paused state.
    private void PauseSpawnedEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyBehaviourBase enemy = spawnedEnemies[i];

            if (enemy == null)
            {
                continue;
            }

            enemy.SetPaused(true);
        }
    }

    // Sets all currently active spawned enemies back to the unpaused state.
    private void ResumeSpawnedEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyBehaviourBase enemy = spawnedEnemies[i];

            if (enemy == null)
            {
                continue;
            }

            enemy.SetPaused(false);
        }
    }

    // Starts the encounter reset countdown if one is not already running.
    private void StartResetCounter()
    {
        if (resetCoroutine != null)
        {
            return;
        }

        resetCoroutine = StartCoroutine(ResetCounterRoutine());
    }

    // Cancels the encounter reset countdown if the player returns to the room.
    private void CancelResetCounter()
    {
        if (resetCoroutine == null)
        {
            return;
        }

        StopCoroutine(resetCoroutine);
        resetCoroutine = null;
    }

    // Counts down while the player is outside the room, then resets the encounter if the timer expires.
    private IEnumerator ResetCounterRoutine()
    {
        float timer = resetCounter;

        while (timer > 0f)
        {
            if (isPlayerInRoom)
            {
                resetCoroutine = null;
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        ResetEncounter();
        resetCoroutine = null;
    }

    // Fully resets the encounter back to its initial state.
    private void ResetEncounter()
    {
        if (waveLoopCoroutine != null)
        {
            StopCoroutine(waveLoopCoroutine);
            waveLoopCoroutine = null;
        }

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyBehaviourBase enemy = spawnedEnemies[i];

            if (enemy == null)
            {
                continue;
            }

            Destroy(enemy.gameObject);
        }

        spawnedEnemies.Clear();
        enemiesToSpawn.Clear();

        isPlayerPastDoor = false;
        currentWave = encounterMode == EncounterMode.Arena ? startingWave - 1 : 0;
        hasEncounterStarted = false;
        playerStatus?.SetInEncounter(false);
        hasEncounterCompleted = false;
    }

    // Automatically gathers all child spawn points under this manager in the hierarchy.
    private void GatherSpawnPoints()
    {
        spawnPoints.Clear();
        spawnPoints.AddRange(GetComponentsInChildren<EnemySpawnPoint>(true));
    }

    // Resizes all child spawn point enemy pools to match the current maximum number of waves - only really relevant in standard mode; arena mode uses spawn points as location markers only.
    private void SyncSpawnPointEnemyPools()
    {
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[i];

            if (spawnPoint == null)
            {
                continue;
            }

            spawnPoint.ResizeEnemyPool(maxWaves);
        }
    }

    // Removes a dead enemy from the active enemy list.
    public void NotifyEnemyDeath(EnemyBehaviourBase deadEnemy)
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
        if (maxWaves <= 0)
        {
            Debug.LogWarning("EnemyEncounterManager requires a maximum number of waves greater than 0.");
            return false;
        }

        if (resetCounter <= 0f)
        {
            Debug.LogWarning("EnemyEncounterManager requires a reset counter greater than 0.");
            return false;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("EnemyEncounterManager could not find any child EnemySpawnPoint components.");
            return false;
        }

        if (useDoorGating && (doors == null || doors.Count == 0))
        {
            Debug.LogWarning("EnemyEncounterManager has door gating enabled but no doors are assigned.");
        }

        if (encounterMode == EncounterMode.Standard)
        {
            return true;
        }

        // Arena-specific validation.
        if (enemyPool == null || enemyPool.Count == 0)
        {
            Debug.LogWarning("EnemyEncounterManager (Arena) is missing enemy pool entries.");
            return false;
        }

        if (spawnTimingMode == SpawnTimingMode.OverWaveDuration && waveDuration <= 0f)
        {
            Debug.LogWarning("EnemyEncounterManager (Arena) requires a wave duration greater than 0 when using timed spawning.");
            return false;
        }

        if (spawnLocationMode == SpawnLocationMode.RandomWithinRadius)
        {
            if (randomSpawnRadius <= 0f)
            {
                Debug.LogWarning("EnemyEncounterManager (Arena) requires a random spawn radius greater than 0.");
                return false;
            }

            if (groundLayer == 0)
            {
                Debug.LogWarning("EnemyEncounterManager (Arena) is missing a ground layer for random spawning.");
                return false;
            }
        }

        return true;
    }

    // Exposes fields for the custom editor.
    public EncounterMode CurrentEncounterMode => encounterMode;
    public bool UseDoorGating => useDoorGating;
}