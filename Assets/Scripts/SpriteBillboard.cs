using System;
using Unity.Mathematics;
using Unity.VisualScripting;
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
    [SerializeField] Vector3 forwardVector = new Vector3(0,0,1);

    private void Update()
    {
        if (easing) DoSlow();
        else DoImmediate();
    }

    Vector3 targetRotation;

    private void DoSlow()
    {
        targetRotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position).eulerAngles;
        if (!rotateX) targetRotation = new Vector3(0, targetRotation.y, targetRotation.z);
        Vector3.Slerp(transform.rotation.eulerAngles, targetRotation, Time.deltaTime * easeSpeed);
    }

    public void DoImmediate()
    {
        targetRotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position).eulerAngles;
        if (!rotateX) targetRotation = new Vector3(0, targetRotation.y, targetRotation.z);
        transform.rotation = Quaternion.Euler(targetRotation);
    }
}
