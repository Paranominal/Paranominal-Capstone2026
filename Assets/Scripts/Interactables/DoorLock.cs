using UnityEngine;

// Owns a door's access state, key requirement, and lock feedback.
public class DoorLock : MonoBehaviour
{
    [Header("Lock Settings")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private GameObject requiredItem;
    [SerializeField] private bool consumeItemOnUnlock;

    [Header("Visuals")]
    [Tooltip("Drag the lock into here to have it disappear when unlocked.")]
    [SerializeField] private GameObject[] lockVisuals;
    
    [Header("Magic Lock Visuals")]
    [Tooltip("Enable here and disable DestroyOnComplete in the DissolveEffect Component.")]
    [SerializeField] private bool destroyOnUnlock;
    [Tooltip("Add the DissolveEffect component here.")]
    [SerializeField] private DissolveEffect unlockDissolveEffect;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO lockedSound;
    [SerializeField] private SoundDataSO unlockSound;

    private bool isLocked;

    public bool IsLocked => isLocked;

    private void Awake()
    {
        isLocked = startsLocked;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetVisualsActive(isLocked);
        SetCollidersEnabled(isLocked);
    }

    public bool CanUnlock(InteractionContext context)
    {
        if (!isLocked)
            return true;

        return requiredItem != null &&
               context != null &&
               context.inventory != null &&
               context.inventory.HasItem(requiredItem);
    }

    public bool TryUnlock(InteractionContext context)
    {
        return TryUnlock(context, true);
    }

    public bool TryUnlock(InteractionContext context, bool playLockedFeedback)
    {
        if (!isLocked)
            return true;

        if (!CanUnlock(context))
        {
            if (playLockedFeedback)
                PlayLockedFeedback();

            return false;
        }

        if (consumeItemOnUnlock)
            context.inventory.ConsumeItem(requiredItem);

        Unlock();
        return true;
    }

    public void Unlock()
    {
        if (!isLocked)
            return;

        isLocked = false;
        SetCollidersEnabled(false);
        AudioManager.PlaySound(unlockSound, audioSource);

        if (unlockDissolveEffect != null)
        {
            unlockDissolveEffect.OnDissolveComplete += OnDissolveComplete;
            unlockDissolveEffect.Play();
        }
        else
        {
            CompleteUnlock();
        }
    }

    public void Lock()
    {
        isLocked = true;
        SetVisualsActive(true);
        SetCollidersEnabled(true);
    }

    public void PlayLockedFeedback()
    {
        AudioManager.PlaySound(lockedSound, audioSource);
    }

    private void SetVisualsActive(bool active)
    {
        if (lockVisuals == null)
            return;

        foreach (GameObject lockVisual in lockVisuals)
        {
            if (lockVisual != null)
                lockVisual.SetActive(active);
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider lockCollider in colliders)
            lockCollider.enabled = enabled;
    }

    private void OnDissolveComplete()
    {
        if (unlockDissolveEffect != null)
            unlockDissolveEffect.OnDissolveComplete -= OnDissolveComplete;

        CompleteUnlock();
    }

    private void CompleteUnlock()
    {
        SetVisualsActive(false);

        if (destroyOnUnlock)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (unlockDissolveEffect != null)
            unlockDissolveEffect.OnDissolveComplete -= OnDissolveComplete;
    }
}
