using UnityEngine;

// Handles only the physical behaviour and player interaction of a door.
[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour, IInteractable
{
    public enum DoorState
    {
        Open,
        Ajar,
        Closed,
    }

    [Header("Door State")]
    [SerializeField] private DoorState state = DoorState.Closed;
    [SerializeField] private DoorLock[] doorLocks;

    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float ajarAngle = -20f;
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float ajarDistance = 3f;
    [SerializeField] private Collider doorCollider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;

    private Quaternion startRotation;
    private Quaternion targetRotation;
    private PlayerMover player;

    private void Start()
    {
        startRotation = transform.rotation;
        targetRotation = transform.rotation;

        if (doorCollider == null)
            doorCollider = GetComponentInChildren<Collider>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        player = FindAnyObjectByType<PlayerMover>();
    }

    private void Update()
    {
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            speed * Time.deltaTime);

        bool isMoving = Quaternion.Angle(transform.rotation, targetRotation) > 0.1f;

        if (doorCollider != null)
            doorCollider.enabled = !isMoving;

        if (player != null && state == DoorState.Open &&
            Vector3.Distance(player.transform.position, transform.position) > ajarDistance)
        {
            Ajar();
        }
    }

    public void Interact(InteractionContext context)
    {
        DoorLock firstRemainingLock = null;

        if (doorLocks != null)
        {
            foreach (DoorLock doorLock in doorLocks)
            {
                if (doorLock == null || !doorLock.IsLocked)
                    continue;

                if (doorLock.CanUnlock(context))
                    doorLock.TryUnlock(context, false);
                else if (firstRemainingLock == null)
                    firstRemainingLock = doorLock;
            }
        }

        if (HasLockedLocks())
        {
            firstRemainingLock?.PlayLockedFeedback();
            return;
        }

        Toggle();
    }

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        if (HasLockedLocks())
        {
            return new InteractionPrompt
            {
                label = CanUnlockAnyLock(context) ? "Unlock" : "Locked",
                actionName = "Collect"
            };
        }

        return new InteractionPrompt
        {
            label = state == DoorState.Open ? "Close" : "Open",
            actionName = "Collect"
        };
    }

    public void Toggle()
    {
        if (state == DoorState.Open)
            Ajar();
        else
            Open();
    }

    public void Open()
    {
        SetTargetRotation(openAngle, DoorState.Open);
        AudioManager.PlaySound(openSound, audioSource);
    }

    public void ForceOpen()
    {
        Open();
    }

    public void Ajar()
    {
        SetTargetRotation(ajarAngle, DoorState.Ajar);
        AudioManager.PlaySound(closeSound, audioSource);
    }

    public void Close()
    {
        SetTargetRotation(closedAngle, DoorState.Closed);
        AudioManager.PlaySound(closeSound, audioSource);
    }

    private void SetTargetRotation(float angle, DoorState newState)
    {
        targetRotation = startRotation * Quaternion.AngleAxis(angle, Vector3.up);
        state = newState;
    }

    private bool HasLockedLocks()
    {
        if (doorLocks == null)
            return false;

        foreach (DoorLock doorLock in doorLocks)
        {
            if (doorLock != null && doorLock.IsLocked)
                return true;
        }

        return false;
    }

    private bool CanUnlockAnyLock(InteractionContext context)
    {
        if (doorLocks == null)
            return false;

        foreach (DoorLock doorLock in doorLocks)
        {
            if (doorLock != null && doorLock.IsLocked && doorLock.CanUnlock(context))
                return true;
        }

        return false;
    }
}
