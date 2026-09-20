using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Inventory inventory;
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private GameObject reloadTut;
    [SerializeField] private GameObject shootDemonTut;
    [SerializeField] private List<GameObject> initialEntries;

    void Awake()
    {
        if (!weaponEvents) Debug.LogWarning($"[{this}] No Weapon Events set! This might be a mistake");

        if (reloadTut) weaponEvents.ReloadStarted += TriggerReloadedTutorial;
        else Debug.LogWarning($"[{this}] No 'Reload' Tutorial Set! this might be a mistake");
        if (shootDemonTut) weaponEvents.ShotResolved += TriggerShootEnemyTutorial;
        else Debug.LogWarning($"[{this}] No 'Shoot Demon' Tutorial Set! this might be a mistake");
    }
    void Start()
    {
        AddInitialEntries();
    }

    void AddInitialEntries()
    {
        foreach (GameObject entry in initialEntries)
        {
            inventory.Add(Instantiate(entry), true);
            Debug.LogWarning("AHHH");
        }
    }

    void TriggerReloadedTutorial()
    {
        weaponEvents.ReloadStarted -= TriggerReloadedTutorial; // unsub
        dialogueManager.StartDialogue(reloadTut); //trigger tut
    }
    
    void TriggerShootEnemyTutorial(ShotResult result)
    {
        if (result.Outcome != ShotOutcome.EnemyHit) return; // if anything but EnemyHit
        weaponEvents.ShotResolved -= TriggerShootEnemyTutorial; // unsub
        dialogueManager.StartDialogue(shootDemonTut); // trigger tut
    }
}
