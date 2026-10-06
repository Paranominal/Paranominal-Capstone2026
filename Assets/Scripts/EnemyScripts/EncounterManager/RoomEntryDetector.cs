using System;
using UnityEngine;

public class RoomEntryDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyEncounterManager encounterManager;
    public event Action<RoomEntryDetector> PlayerEntry;

    [Header("Boss Room")]
    [Tooltip("Is this detector for a Boss Room?")]
    [SerializeField] private bool isBossRoom;
    [ShowIf("isBossRoom")]
    [Tooltip("Assign Boss and Sub-Boss Spawner/s here.")] // EDIT (boss-Doors): temporary, will review later and make it not shit blah blah blah leave me alone
    [SerializeField] private EnemySpawnPoint_Standalone[] bossSpawnPoints;

    // Automatically finds the parent encounter manager when the object is first loaded.
    private void Awake()
    {
        if (encounterManager == null)
        {
            encounterManager = GetComponentInParent<EnemyEncounterManager>();
        }
    }

    // Detects the player entering the room trigger and notifies the encounter manager.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerEntry?.Invoke(this);

        if (encounterManager != null)
        {
            encounterManager.SetPlayerInRoom(true);
            Debug.Log($"{other.name} has entered the room!");
        }
        else if (isBossRoom)
        {
            Debug.Log($"{other.name} has entered the Boss Room!");
            foreach (EnemySpawnPoint_Standalone spawner in bossSpawnPoints)
            {
                spawner.SpawnEnemy();
            }
        }
    }

    // Detects the player leaving the room trigger and notifies the encounter manager.
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (encounterManager != null)
        {
            encounterManager.SetPlayerInRoom(false);
            Debug.Log($"{other.name} has exited the room!");
        }
    }
}