using UnityEngine;
using System.Collections.Generic;
public class Container : MonoBehaviour, IInteractable
{
    private ALTGrimoire grimoire;
    public List<ALTGrimoireEntry> contents;

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

    }

    // EDIT (interaction prompt system): signature now takes a context. Behaviour unchanged; the
    // cauldron still runs on the legacy raycast path (InteractionObject + ContainerCheck), so this
    // Interact is called from there, not from the focus controller.
    public void Interact(InteractionContext context)
    {
        contents.Add(grimoire.GetCurrentEntry());
    }

    // EDIT (interaction prompt system): required by IInteractable. The cauldron doesn't use the new
    // prompt surfaces, so it returns no prompt.
    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        return InteractionPrompt.None;
    }

    public void Empty()
    {
        contents.Clear();
    }
}
