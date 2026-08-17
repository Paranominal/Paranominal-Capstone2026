using UnityEngine;
using UnityEngine.AI;
using System;

public class ToMiniBoss : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float scaleMultiplier = 1.5f;

    [Header("Stagger & Cycle Settings")]
    [SerializeField] private int cyclesRequired = 3;

    [Header("Minion Spawn Settings")]
    [SerializeField] private bool enableSpawning = false; //toggle or not
    [SerializeField] private int[] spawnMinionCycles = new int[] { 1 }; //this is an array, 0 would mean after the first cycle
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionsToSpawn = 3;
    [SerializeField] private float spawnRadius = 3f;

    private int currentCycle = 0;

    private WeakPointManager wpManager;

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

        wpManager = GetComponentInChildren<WeakPointManager>(true);

        HideWeakpoints();
    }

    private void HideWeakpoints()
    {
        if (wpManager != null)
        {
            WeakPoint[] points = wpManager.GetComponentsInChildren<WeakPoint>(true);
            foreach (WeakPoint p in points)
            {
                p.Hide();
            }
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
            if (enableSpawning && spawnMinionCycles != null && Array.IndexOf(spawnMinionCycles, currentCycle) != -1)
            {
                SpawnMinions();
            }

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
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
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