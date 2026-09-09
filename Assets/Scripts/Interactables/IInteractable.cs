using UnityEngine;

// Michael feature (interaction-rework): Interact now takes an InteractionContext, and interactables resolve a context-aware prompt via ResolvePrompt.
public interface IInteractable
{
    void Interact(InteractionContext context);
    InteractionPrompt ResolvePrompt(InteractionContext context);
    GameObject gameObject { get; }
}
