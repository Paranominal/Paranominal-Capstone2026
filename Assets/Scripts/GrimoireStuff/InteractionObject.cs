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

    void Start()
    {
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
        collectAction = InputSystem.actions.FindAction("Collect");
        target = GetComponentInChildren<IInteractable>();   // theres some glaring issues with this (namely you can't currently have more than one interaction type on one object) but it should work
    }

    void Update()
    {
        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactable))
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
