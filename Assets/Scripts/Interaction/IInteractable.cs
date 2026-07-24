using UnityEngine;

// EDIT (interaction prompt system): Interact now takes an InteractionContext, and interactables
// resolve a context-aware prompt via ResolvePrompt.
public interface IInteractable
{
    void Interact(InteractionContext context);
    InteractionPrompt ResolvePrompt(InteractionContext context);
    GameObject gameObject { get; }
}
