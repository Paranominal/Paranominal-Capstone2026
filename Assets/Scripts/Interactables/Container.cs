using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
public class Container : MonoBehaviour, IInteractable
{
    private ALTGrimoire grimoire;
    public List<ALTGrimoireEntry> contents;
    public Outline outline;
    public LayerMask interactable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (outline != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactable) && GetComponentInChildren<Collider>() == hit.collider)
            {
                outline.enabled = true;
            }
            else
            {
                outline.enabled = false;
            }
        }
    }

    public void Interact()
    {
        contents.Add(grimoire.GetCurrentEntry());
    }

    public void Empty()
    {
        contents.Clear();
    }
}
