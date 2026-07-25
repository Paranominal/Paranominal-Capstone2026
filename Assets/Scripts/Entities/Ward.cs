using UnityEngine;

// Summary: A destructible ward (e.g. a glyph on a door). The player needs a specific item
// to destroy it, unlocking the door behind it. Follows the same prompt pattern as Door:
// world-space "Warded" label when the player lacks the item, HUD action prompt when they have it.
[RequireComponent(typeof(Collider))]
public class Ward : MonoBehaviour, IInteractable
{
    [Header("Requirements")]
    [SerializeField] private ItemDefinition requiredItem;
    [SerializeField] private bool consumesItem = true;

    [Header("Door")]
    [SerializeField] private Door targetDoor;

    [Header("Prompt")]
    [SerializeField] private Transform promptAnchor;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO destroySound;

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        if (context != null && context.HasKey(requiredItem))
        {
            string itemName = requiredItem != null ? requiredItem.displayName : "Item";
            return new InteractionPrompt
            {
                surface = PromptSurface.Hud,
                label = $"Destroy Ward ({itemName})",
                actionName = "Collect"
            };
        }

        return new InteractionPrompt
        {
            surface = PromptSurface.WorldSpace,
            label = "Warded",
            anchor = promptAnchor
        };
    }

    public void Interact(InteractionContext context)
    {
        if (context == null) return;

        if (context.HasKey(requiredItem))
        {
            if (consumesItem)
                context.ConsumeKey(requiredItem);

            if (destroySound != null && audioSource != null)
                AudioManager.PlaySound(destroySound, audioSource);

            if (targetDoor != null)
                targetDoor.Unlock();

            Debug.Log("Ward destroyed: " + gameObject.name);
            Destroy(gameObject);
        }
    }
}
