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
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactable))
        {
            if (grimoire.entries.Count != 0)
            {
                if (grimoire.GetCurrentEntry().entryName == keyName && grimoire.GetCurrentEntry().collected && collectAction.WasReleasedThisFrame())
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
