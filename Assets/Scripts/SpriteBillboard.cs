using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UIElements.Experimental;

public class SpriteBillboard : MonoBehaviour
{
    [Header("Billboard Rotations")] 
    [SerializeField] private bool rotateX = false;
    [Header("Preferences")] 
    [SerializeField] bool easing = false;
    [ShowIf("easing", true), SerializeField] private float easeSpeed = 0.1f;
    void Update()
    {
        if (easing) DoSlow();
        else DoImmediate();
    }
    Vector3 targetRotation;
    void DoSlow()
    {
        targetRotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position).eulerAngles;
        if (!rotateX) targetRotation = new Vector3(0, targetRotation.y, targetRotation.z);
        Vector3.Slerp(transform.rotation.eulerAngles, targetRotation, Time.deltaTime * easeSpeed);
    }

    void DoImmediate()
    {
        targetRotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position).eulerAngles;
        if (!rotateX) targetRotation = new Vector3(0, targetRotation.y, targetRotation.z);
        transform.rotation = Quaternion.Euler(targetRotation);
    }
}
