using UnityEngine;

// Summary: Key-lock interactable placed on a child object of a door (keyhole, padlock, etc.).
// Mirrors the Ward pattern: holds a reference to the target Door and unlocks it when
// the player interacts with the correct key. Non-interactive without the key.
[RequireComponent(typeof(AudioSource))]
public class Lock : MonoBehaviour, IInteractable
{
    [Header("Lock")]
    public ItemDefinition requiredKey;
    public bool consumesKey = true;
    public Door targetDoor;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO unlockSound;

    private bool isUnlocked;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Interact(InteractionContext context)
    {
        if (isUnlocked) return;
        if (context == null || !context.HasKey(requiredKey)) return;

        isUnlocked = true;

        if (consumesKey)
            context.ConsumeKey(requiredKey);

        AudioManager.PlaySound(unlockSound, audioSource);

        if (targetDoor != null)
            targetDoor.Unlock();
        
        Destroy(gameObject);
    }

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        if (isUnlocked) return InteractionPrompt.None;

        // Only show a HUD prompt when the player has the key.
        if (context != null && context.HasKey(requiredKey))
        {
            string keyName = requiredKey != null ? requiredKey.displayName : "Key";
            return new InteractionPrompt
            {
                surface = PromptSurface.Hud,
                label = $"Unlock ({keyName})",
                actionName = "Collect",
            };
        }

        // No key: return nothing. The Door's WorldSpace "Locked" prompt handles discovery.
        return InteractionPrompt.None;
    }
}
