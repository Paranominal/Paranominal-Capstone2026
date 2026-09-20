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

    [Header("One-Way")]
    [SerializeField] private bool isOneWay = false;
    [ShowIf("isOneWay")]
    [Tooltip("When enabled, the accessible side is flipped to the door's back face.")]
    [SerializeField] private bool flipAccessibleSide = false;
    [ShowIf("isOneWay")]
    [Tooltip("When enabled, the door becomes two-way once all locks are unlocked.")]
    [SerializeField] private bool twoWayWhenUnlocked = true;
    [ShowIf("isOneWay")]
    [Tooltip("When enabled, the door becomes two-way after being opened from the accessible side.")]
    [SerializeField] private bool twoWayOnceOpened = false;

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
    private bool hasBeenOpened;

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
            // one-way locked doors close fully so the lock remains meaningful
            if (isOneWay && HasLockedLocks())
                Close();
            else
                Ajar();
        }
    }

    public void Interact(InteractionContext context)
    {
        bool locked = HasLockedLocks();
        bool effectivelyOneWay = IsEffectivelyOneWay();
        bool onAccessibleSide = IsPlayerOnAccessibleSide();

        if (locked)
        {
            // one-way + locked: accessible side bypasses locks entirely
            if (effectivelyOneWay && onAccessibleSide)
            {
                if (twoWayOnceOpened) hasBeenOpened = true;
                Toggle();
                return;
            }

            // standard lock logic (both sides for non-one-way, inaccessible side for one-way)
            DoorLock firstRemainingLock = null;
            bool unlockedSomething = false;

            if (doorLocks != null)
            {
                foreach (DoorLock doorLock in doorLocks)
                {
                    if (doorLock == null || !doorLock.IsLocked)
                        continue;

                    if (doorLock.CanUnlock(context))
                    {
                        doorLock.TryUnlock(context, false);
                        unlockedSomething = true;
                    }
                    else if (firstRemainingLock == null)
                    {
                        firstRemainingLock = doorLock;
                    }
                }
            }

            if (HasLockedLocks())
            {
                firstRemainingLock?.PlayLockedFeedback();
                return;
            }

            // just unlocked the last lock: don't auto-open, player interacts again to open
            if (unlockedSomething)
                return;
        }
        else if (effectivelyOneWay && !onAccessibleSide)
        {
            // no locks, one-way, wrong side: blocked
            return;
        }

        if (twoWayOnceOpened && isOneWay && onAccessibleSide)
            hasBeenOpened = true;

        Toggle();
    }

    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        bool locked = HasLockedLocks();
        bool effectivelyOneWay = IsEffectivelyOneWay();
        bool onAccessibleSide = IsPlayerOnAccessibleSide();

        if (locked)
        {
            if (effectivelyOneWay && onAccessibleSide)
            {
                return new InteractionPrompt
                {
                    label = state == DoorState.Open ? "Close" : "Open",
                    actionName = "Collect"
                };
            }

            return new InteractionPrompt
            {
                label = CanUnlockAnyLock(context) ? "Unlock" : "Locked",
                actionName = "Collect"
            };
        }

        if (effectivelyOneWay && !onAccessibleSide)
        {
            return new InteractionPrompt
            {
                label = "Won't open from this side",
                actionName = ""
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
            Close();
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

    // One-Way
    private bool IsEffectivelyOneWay()
    {
        if (!isOneWay) return false;
        if (twoWayOnceOpened && hasBeenOpened) return false;
        // only disable one-way when locks exist but have all been unlocked
        if (twoWayWhenUnlocked && HasAnyLocks() && !HasLockedLocks()) return false;
        return true;
    }

    private bool HasAnyLocks()
    {
        if (doorLocks == null || doorLocks.Length == 0) return false;
        foreach (DoorLock doorLock in doorLocks)
        {
            if (doorLock != null) return true;
        }
        return false;
    }

    private bool IsPlayerOnAccessibleSide()
    {
        if (player == null) return true;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return true;

        bool inFront = Vector3.Dot(transform.forward, toPlayer.normalized) > 0f;
        return flipAccessibleSide ? !inFront : inFront;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!isOneWay) return;

        Vector3 direction = flipAccessibleSide ? -transform.forward : transform.forward;
        Vector3 start = transform.position + Vector3.up * 1f;
        Vector3 end = start + direction * 1.5f;

        // arrow shaft
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);

        // arrowhead
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        Gizmos.DrawLine(end, end + (-direction + right) * 0.3f);
        Gizmos.DrawLine(end, end + (-direction - right) * 0.3f);
    }
    #endif
}