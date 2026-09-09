using UnityEngine;
using System.Collections.Generic;

// Michael feature (interaction-rework): updated IInteractable signature.
public class Container : MonoBehaviour, IInteractable
{
    private ALTGrimoire grimoire;
    public List<ALTGrimoireEntry> contents;

    void Start()
    {
        if (grimoire == null)
            grimoire = FindAnyObjectByType<ALTGrimoire>();
    }

    public void Interact(InteractionContext context)
    {
        contents.Add(grimoire.GetCurrentEntry());
    }

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        return new InteractionPrompt
        {
            label = "Store",
            actionName = "Collect"
        };
    }

    public void Empty()
    {
        contents.Clear();
    }
}
