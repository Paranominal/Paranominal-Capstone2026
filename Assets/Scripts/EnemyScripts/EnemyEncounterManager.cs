using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEncounterManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int maxWaves = 5;
    [SerializeField] private float timeBetweenWaves = 3f;

    [Header("Spawn Points")]
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    [Header("Encounter State")]
    [SerializeField] private bool isPlayerInRoom = false;

    private int currentWave;
    private bool hasEncounterStarted;
    private Coroutine waveLoopCoroutine;

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
            if (!hasEncounterStarted)
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
            PauseSpawnedEnemies();
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
            yield return StartCoroutine(RunSingleWaveRoutine());

            if (currentWave < maxWaves)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

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

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("EnemyEncounterManager could not find any child EnemySpawnPoint components.");
            return false;
        }

        return true;
    }
}