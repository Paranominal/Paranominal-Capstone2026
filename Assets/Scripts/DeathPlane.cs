using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathPlane : MonoBehaviour
{
    public event Action DeathPlaneHit;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log($"DeathPlane Hit!");
        DeathPlaneHit?.Invoke();
    }
}
