using System.Collections.Generic;
using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    [SerializeField] private GameObject textDisplay;
    public ALTGrimoireEntry grimoireEntry;
    public GameObject pickupDialogue;
    [SerializeField] private Outline outline;
    [Header("Enemy Spawns")]
    [Tooltip("Trigger the assigned standalone spawn points when this item is picked up.")]
    [SerializeField] private bool triggerEnemySpawns;
    [ShowIf("triggerEnemySpawns")]
    [Tooltip("Drag standalone spawn points from the scene into this list.")]
    [SerializeField] private List<EnemySpawnPoint_Standalone> spawnPoints = new List<EnemySpawnPoint_Standalone>();

    private bool pickupSpawnsTriggered;

    // Called by Inventory only after an actual pickup has been accepted.
    public void OnPickedUp()
    {
        if (!triggerEnemySpawns || pickupSpawnsTriggered) return;
        pickupSpawnsTriggered = true;

        if (spawnPoints == null) return;
        foreach (EnemySpawnPoint_Standalone spawnPoint in spawnPoints)
        {
            if (spawnPoint != null) spawnPoint.SpawnEnemy();
        }
    }

    void Start()
    {
        //Debug.Log($"{dialogue.itemName} | {dialogue} | {dialogue.gameObject}");
        Deactivate();
    }
    public void Activate()
    {
        if (textDisplay != null) textDisplay.SetActive(true);
        if (outline != null) outline.enabled = true;
    }
    public void Deactivate()
    {
        if (textDisplay != null) textDisplay.SetActive(false);
        if (outline != null) outline.enabled = false;
    }
}
