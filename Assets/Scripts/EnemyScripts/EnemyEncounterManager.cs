using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEncounterManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int maxWaves = 1;
    [SerializeField] private float timeBetweenWaves = 3f;

    [Header("Spawn Points")]
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    [Header("Encounter State")]
    [SerializeField] private bool isPlayerInRoom = false;
    [SerializeField] private float resetCounter = 5f;

    private int currentWave;
    private bool hasEncounterStarted;
    private bool hasEncounterCompleted;
    private Coroutine waveLoopCoroutine;
    private Coroutine resetCoroutine;

    private readonly List<EncounterEnemyController> spawnedEnemies = new List<EncounterEnemyController>();

    // Gathers child spawn points and keeps their enemy pools synced to the current maximum number of waves.
    private void OnValidate()
    {
        GatherSpawnPoints();
        SyncSpawnPointEnemyPools();
    }

    // Gathers child spawn points and validates the spawner setup when the scene begins.
    private void Start()
    {
        GatherSpawnPoints();
        SyncSpawnPointEnemyPools();

        if (!ValidateSpawnerSetup())
        {
            enabled = false;
            return;
        }

        currentWave = 0;
    }

    // Removes any destroyed enemy references that still remain in the live list.
    private void Update()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
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
                StartEncounter();
            }
            else
            {
                ResumeSpawnedEnemies();
            }
        }
        else
        {
            if (hasEncounterStarted && !hasEncounterCompleted)
            {
                PauseSpawnedEnemies();
                StartResetCounter();
            }
        }
    }

    // Starts the encounter wave loop the first time the player enters the room.
    private void StartEncounter()
    {
        if (hasEncounterStarted)
        {
            return;
        }

        hasEncounterStarted = true;
        waveLoopCoroutine = StartCoroutine(WaveLoopRoutine());
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
        waveLoopCoroutine = null;
    }

    // Handles one full wave from spawning through to completion.
    private IEnumerator RunSingleWaveRoutine()
    {
        currentWave++;

        SpawnWave();

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
    private void SpawnWave()
    {
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[i];

            if (spawnPoint == null)
            {
                continue;
            }

            EncounterEnemyController spawnedEnemy = spawnPoint.SpawnEnemy(currentWave, this);

            if (spawnedEnemy != null)
            {
                spawnedEnemies.Add(spawnedEnemy);

                if (!isPlayerInRoom)
                {
                    spawnedEnemy.SetPaused(true);
                }
            }
        }
    }

    // Sets all currently active spawned enemies to the paused state.
    private void PauseSpawnedEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EncounterEnemyController enemy = spawnedEnemies[i];

            if (enemy == null)
            {
                continue;
            }

            enemy.SetPaused(true);
        }
    }

    // Sets all currently active spawned enemies back to the aggro state.
    private void ResumeSpawnedEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EncounterEnemyController enemy = spawnedEnemies[i];

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
            EncounterEnemyController enemy = spawnedEnemies[i];

            if (enemy == null)
            {
                continue;
            }

            Destroy(enemy.gameObject);
        }

        spawnedEnemies.Clear();

        currentWave = 0;
        hasEncounterStarted = false;
        hasEncounterCompleted = false;
    }

    // Automatically gathers all child spawn points under this spawner in the hierarchy.
    private void GatherSpawnPoints()
    {
        spawnPoints.Clear();
        spawnPoints.AddRange(GetComponentsInChildren<EnemySpawnPoint>(true));
    }

    // Resizes all child spawn point enemy pools to match the current maximum number of waves.
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
    public void NotifyEnemyDeath(EncounterEnemyController deadEnemy)
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

        return true;
    }
}