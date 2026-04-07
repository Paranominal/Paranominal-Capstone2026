using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ContainerCheck : MonoBehaviour
{
    private Container container;
    public List<ALTGrimoireEntry> solution;
    public List<ALTGrimoireEntry> failures;
    InputAction collectAction;
    public LayerMask interactable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
