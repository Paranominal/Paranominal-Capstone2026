using UnityEngine;
using UnityEngine.InputSystem;

// Michael feature (interaction-rework): updated target.Interact() to pass InteractionContext.
public class InteractionObject : MonoBehaviour
{
    public string keyName;
    public LayerMask interactable;
    private ALTGrimoire grimoire;
    InputAction collectAction;
    public IInteractable target;
    public bool consumesItem;
    private Raycaster raycaster;

    void Start()
    {
        if (grimoire == null)
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        if (raycaster == null)
            raycaster = FindAnyObjectByType<Raycaster>();

        collectAction = InputSystem.actions.FindAction("Collect");
        target = GetComponentInChildren<IInteractable>();
    }

    void Update()
    {
        if (Physics.Raycast(raycaster.Ray, out RaycastHit hit, 1000f, interactable))
        {
            if (grimoire.entries.Count != 0)
            {
                if (grimoire.GetCurrentEntry().entryName == keyName && grimoire.GetCurrentEntry().collected && collectAction.WasReleasedThisFrame() && target.gameObject.GetComponentInChildren<Collider>() == hit.collider)
                {
                    target.Interact(new InteractionContext());

                    if (consumesItem)
                    {
                        grimoire.CollectEntry(grimoire.GetCurrentEntry(), false);
                    }
                }
            }
        }
    }
}
