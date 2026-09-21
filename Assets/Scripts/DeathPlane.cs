using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathPlane : MonoBehaviour
{
    public event Action DeathPlaneHit;

    private void OnTriggerEnter(Collider other)
    {
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        DeathPlaneHit?.Invoke();
    }
}
