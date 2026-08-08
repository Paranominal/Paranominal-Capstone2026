using UnityEngine;

// Summary: A destructible ward (e.g. a glyph on a door). The player needs a specific item
// to destroy it, unlocking the door behind it.
// EDIT (label-split): no longer shows a floating "Warded" label. Visual indicators on
// the ward itself communicate its presence. Only shows a HUD prompt when the player
// has the required item.
[RequireComponent(typeof(Collider))]
public class Ward : MonoBehaviour, IInteractable
{
    [Header("Requirements")]
    [SerializeField] private ItemDefinition requiredItem;
    [SerializeField] private bool consumesItem = true;

    [Header("Door")]
    [SerializeField] private Door targetDoor;

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
                label = $"Destroy Ward ({itemName})",
                actionName = "Collect",
            };
        }

        return InteractionPrompt.None;
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

            Debug.Log("Glyph destroyed: " + gameObject.name);
            Destroy(gameObject);
        }
    }
}