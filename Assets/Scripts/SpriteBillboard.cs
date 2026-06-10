using System;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class SpriteBillboard : MonoBehaviour
{
    [Header("Billboard Rotations")] 
    [SerializeField] private bool rotateX = false;
    void Update()
    {
        if (!rotateX) transform.rotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
        else transform.rotation = Quaternion.Euler(Camera.main.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, 0f);
    }
}
