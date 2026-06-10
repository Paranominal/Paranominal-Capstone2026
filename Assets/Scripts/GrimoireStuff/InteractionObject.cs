using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionObject : MonoBehaviour
{
    public string keyName;
    public LayerMask interactable;
    private ALTGrimoire grimoire;
    InputAction collectAction;  // this could be rebound to a different action if you prefer
    public IInteractable target;
    public bool consumesItem;
    private Raycaster raycaster;

    void Start()
    {
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
        if (raycaster == null)
        {
            raycaster = FindAnyObjectByType<Raycaster>();
        }
        collectAction = InputSystem.actions.FindAction("Collect");
        target = GetComponentInChildren<IInteractable>();   // theres some glaring issues with this (namely you can't currently have more than one interaction type on one object) but it should work
    }

    void Update()
    {
        if (Physics.Raycast(raycaster.Ray, out RaycastHit hit, 1000f, interactable))
        {
            if (grimoire.entries.Count != 0)
            {
                if (grimoire.GetCurrentEntry().entryName == keyName && grimoire.GetCurrentEntry().collected && collectAction.WasReleasedThisFrame() && target.gameObject.GetComponentInChildren<Collider>() == hit.collider)
                {
                    // can run Interact() on any class that implements the IInteractable interface
                    target.Interact();
                    
                    if (consumesItem)
                    {
                        grimoire.CollectEntry(grimoire.GetCurrentEntry(), false);
                    }
                }
            }

        }
    }
}
