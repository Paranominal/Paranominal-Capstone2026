using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class SpriteBillboard : MonoBehaviour
{
    [Header("Billboard Rotations")] 
    [SerializeField] private bool rotateX = false;
    [Header("Preferences")] 
    [SerializeField] private bool slowBillboard = false;
    [SerializeField] private float slowBillboardTime = 0.1f;

    Vector3 currentSmoothVelocity; // you look lonely
    Quaternion targetRotation;

    private void Update()
    {
        if (slowBillboard) DoSlow();
        else DoImmediate();
    }

    private void DoSlow()
    {
        if (!rotateX) targetRotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
        else targetRotation = Quaternion.Euler(Camera.main.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, 0f);
        Vector3 smoothedRotation = Vector3.Lerp(transform.rotation.eulerAngles, targetRotation.eulerAngles, slowBillboardTime);
        Quaternion newRotation = Quaternion.Euler(smoothedRotation);
        transform.rotation = newRotation;
    }

    public void DoImmediate()
    {
        if (!rotateX) transform.rotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
        else transform.rotation = Quaternion.Euler(Camera.main.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, 0f);
    }
}
