using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private List<DeathPlane> deathPlane;
    [SerializeField] private Transform miriam;
    [SerializeField] private List<RoomEntryDetector> roomEntryDetectors;
    [SerializeField] private InputActionReference respawnInput;
    private RespawnPoint currentRespawnPoint;
    private bool isRespawning;

    void Reset()
    {
        deathPlane = FindObjectsByType<DeathPlane>(FindObjectsSortMode.None).ToList();
        roomEntryDetectors = FindObjectsByType<RoomEntryDetector>(FindObjectsSortMode.None).ToList();

        miriam = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    { 
        PressRespawn();
    }

    void Start()
    {
        foreach (DeathPlane plane in deathPlane)
        {
            plane.DeathPlaneHit += RespawnMiriam;
        }
        

        foreach (RoomEntryDetector roomEntryDetector in roomEntryDetectors)
        {
            roomEntryDetector.PlayerEntry += SetRespawnPoint;
        }
    }

    void RespawnMiriam()
    {
        if (isRespawning) return;   // the death plane can fire repeatedly while she's falling
        StartCoroutine(RespawnRoutine());
    }

    void SetRespawnPoint(RoomEntryDetector roomEntryDetector)
    {
        RespawnPoint respawnPoint = roomEntryDetector.GetComponentInChildren<RespawnPoint>();
        if (respawnPoint != null) currentRespawnPoint = respawnPoint;
        else
        {
            Debug.LogWarning($"[{this}] Roombounds ({roomEntryDetector}) is missing a RespawnPoint! this might be a mistake.");
        }
    }

    void PressRespawn()
    {
        if (respawnInput.action.WasReleasedThisFrame()) RespawnMiriam();
    }

    // i updated the respawn to use a coroutine so that we can use the transition manager to fade in and out when respawning, since it uses an ienumerator
    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        if (TransitionManager.Instance != null)
            yield return TransitionManager.Instance.TransitionIn();

        var controller = miriam.GetComponent<CharacterController>();
        controller.enabled = false;
        miriam.position = currentRespawnPoint.transform.position;
        miriam.rotation = currentRespawnPoint.transform.rotation;
        controller.enabled = true;
        Debug.Log($"[{this}] Miriam Respawned at ( {currentRespawnPoint.transform.position} )!");

        if (TransitionManager.Instance != null)
            yield return TransitionManager.Instance.TransitionOut();

        isRespawning = false;
    }
}
