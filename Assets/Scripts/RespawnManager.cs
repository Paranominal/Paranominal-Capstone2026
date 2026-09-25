using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private List<DeathPlane> deathPlane;
    [SerializeField] private Transform miriam;
    [SerializeField] private RoomEntryDetector[] roomEntryDetectors;
    private RespawnPoint currentRespawnPoint;

    void Reset()
    {
        deathPlane.Add(GetComponentInChildren<DeathPlane>());
        roomEntryDetectors = FindObjectsByType<RoomEntryDetector>(FindObjectsSortMode.None);
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
        miriam.GetComponent<CharacterController>().enabled = false;
        miriam.position = currentRespawnPoint.transform.position;
        miriam.rotation = currentRespawnPoint.transform.rotation;
        miriam.GetComponent<CharacterController>().enabled = true;
        Debug.Log($"[{this}] Miriam Respawned at ( {currentRespawnPoint.transform.position} )!");
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
}
