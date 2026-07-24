using UnityEngine;
using UnityEngine.InputSystem;

// NOTE (interaction prompt system): this is the legacy raycast interaction path, kept for the
// cauldron (Container + ContainerCheck). Doors now run through InteractionFocusController instead.
public class InteractionObject : MonoBehaviour
{
    public string keyName;
    public LayerMask Interactable;
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
        if (Physics.Raycast(raycaster.Ray, out RaycastHit hit, 1000f, Interactable))
        {
            if (grimoire.entries.Count != 0)
            {
                if (grimoire.GetCurrentEntry().entryName == keyName && grimoire.GetCurrentEntry().collected && collectAction.WasReleasedThisFrame() && target.gameObject.GetComponentInChildren<Collider>() == hit.collider)
                {
                    // can run Interact() on any class that implements the IInteractable interface
                    // EDIT (interaction prompt system): Interact now takes a context; pass one built from this grimoire ref.
                    target.Interact(new InteractionContext { grimoire = grimoire });

                    if (consumesItem)
                    {
                        grimoire.CollectEntry(grimoire.GetCurrentEntry(), false);
                    }
                }
            }

        }
    }
}
