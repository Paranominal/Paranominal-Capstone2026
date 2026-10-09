using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Dependancies")]
    [SerializeField] private DialogueManager dialogueManager;
    [Header("Options")]
    [SerializeField] float tutDelay = 0.5f;
    [Header("Initial Entries")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private List<GameObject> initialEntries;
    [Header("Weapon Tutorial")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private GameObject reloadTut;
    [Header("Shoot Demonds Tutorial")]
    [SerializeField] private GameObject shootDemonTut;
    [SerializeField] private TutorialTrigger shootDemonsTrigger;
    [Header("Immunity Tutorial")]
    [SerializeField] private EnemySpawnPoint enemySpawner;
    [SerializeField] private GameObject immuneTut;

    void Awake()
    {
        if (!weaponEvents) Debug.LogWarning($"[{this}] No Weapon Events set! This might be a mistake");

        if (reloadTut) weaponEvents.ReloadStarted += TriggerReloadedTutorial;
        else Debug.LogWarning($"[{this}] No 'Reload' Tutorial Set! this might be a mistake");
        if (shootDemonTut)
        {
            // weaponEvents.ShotResolved += ShootEnemy;
            shootDemonsTrigger.TutorialTriggered += TriggerShootEnemyTutorial;
        } 
        else Debug.LogWarning($"[{this}] No 'Shoot Demon' Tutorial Set! this might be a mistake");
        if (immuneTut) enemySpawner.EnemySpawned += ImmuneEnemySpawned;
        else Debug.LogWarning($"[{this}] No 'Immunity' Tutorial Set! this might be a mistake");
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
            // Debug.LogWarning("AHHH");
        }
    }

    void TriggerReloadedTutorial()
    {
        weaponEvents.ReloadStarted -= TriggerReloadedTutorial; // unsub
        DoTutorial(reloadTut); //trigger tut
    }

    void ShootEnemy(ShotResult result)
    {
        if (result.Outcome != ShotOutcome.EnemyHit) return; // if anything but EnemyHit
        TriggerShootEnemyTutorial();
    }

    void TriggerShootEnemyTutorial()
    {
        weaponEvents.ShotResolved -= ShootEnemy; // unsub
        shootDemonsTrigger.TutorialTriggered -= TriggerShootEnemyTutorial; // unsub

        DoTutorialImmediate(shootDemonTut); // trigger tut
    }



    void ImmuneEnemySpawned(Enemy immuneEnemy)
    {
        immuneEnemy.ImmuneHit += TriggerImmuneTutorial;
    }

    void TriggerImmuneTutorial(Enemy immuneEnemy)
    {
        immuneEnemy.ImmuneHit -= TriggerImmuneTutorial;
        DoTutorial(immuneTut); // trigger tut
    }

    void DoTutorial(GameObject tut)
    {
        StartCoroutine(TutorialCoroutine(tut));
    }

    void DoTutorialImmediate(GameObject tut)
    {
        dialogueManager.StartDialogue(tut);
    }
    
    
    IEnumerator TutorialCoroutine(GameObject tut)
    {
        yield return new WaitForSeconds(tutDelay);
        dialogueManager.StartDialogue(tut);
    }
}
