using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Door with open/close/ajar/slam states and arena-lock support.
// EDIT (lock-extraction): key-lock logic moved to the Lock component.
// EDIT (label-split): locked doors no longer return a prompt. Visual indicators
// on the door itself communicate locked state. Only unlocked doors show a HUD prompt.
[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour, IInteractable
{
    public enum DoorState
    {
        Open,
        Ajar,
        Closed,
    }

    public bool unlocked;
    public bool isEncounterLocked;
    public DoorState state;
    public float speed = 10;
    public float slamSpeed = 20;
    public float openAngle = -90;
    public float ajarAngle = -20;
    public float closedAngle = 0;
    private Quaternion targetRotation;
    private float actualSpeed;
    public Collider doorCollider;
    private Quaternion startAngle;
    public bool isArenaLocked;
    public float ajarDistance = 3;
    private PlayerMover player;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO slamSound;
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private SoundDataSO lockedSound;
    [SerializeField] private SoundDataSO unlockSound;

    void Start()
    {
        targetRotation = transform.rotation;
        startAngle = transform.rotation;
        actualSpeed = speed;

        if (doorCollider == null)
            doorCollider = GetComponentInChildren<Collider>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player == null)
            player = FindAnyObjectByType<PlayerMover>();
    }

    void Update()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, actualSpeed * Time.deltaTime);

        if (transform.rotation != targetRotation && doorCollider.enabled)
        {
            doorCollider.enabled = false;
        }
        else if (transform.rotation == targetRotation && !doorCollider.enabled)
        {
            doorCollider.enabled = true;
        }

        if (transform.rotation == targetRotation && actualSpeed != speed)
        {
            actualSpeed = speed;
        }

        if (Vector3.Distance(player.transform.position, transform.position) > ajarDistance && state == DoorState.Open)
        {
            Ajar();
        }
    }

    public void Interact(InteractionContext context)
    {
        if (isArenaLocked)
        {
            AudioManager.PlaySound(lockedSound, audioSource);
            return;
        }

        if (!unlocked)
        {
            AudioManager.PlaySound(lockedSound, audioSource);
            return;
        }

        if (state == DoorState.Open)
            Ajar();
        else
            Open();
    }

    // EDIT (label-split): locked/arena-locked doors return no prompt.
    // Only unlocked doors show the HUD "Open" action.
    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        if (isArenaLocked || !unlocked)
            return InteractionPrompt.None;

        return new InteractionPrompt
        {
            label = "Open",
            actionName = "Collect",
        };
    }

    public void Open()
    {
        if (unlocked)
        {
            targetRotation = startAngle * Quaternion.AngleAxis(openAngle, transform.up);
            state = DoorState.Open;
            AudioManager.PlaySound(openSound, audioSource);
        }
    }

    public void ForceOpen()
    {
        unlocked = true;
        Open();
    }

    public void Ajar()
    {
        targetRotation = startAngle * Quaternion.AngleAxis(ajarAngle, transform.up);
        state = DoorState.Ajar;
        AudioManager.PlaySound(closeSound, audioSource);
    }

    public void TryDoor()
    {
        if (unlocked)
        {
            if (state == DoorState.Open)
                Ajar();
            else
                Open();
        }
        else
        {
            AudioManager.PlaySound(lockedSound, audioSource);
        }
    }

    public void Slam()
    {
        Close(true, true);
    }

    public void Close()
    {
        Close(false, false);
    }

    public void Close(bool locked, bool fast)
    {
        targetRotation = startAngle * Quaternion.AngleAxis(closedAngle, transform.up);
        state = DoorState.Closed;
        if (locked) unlocked = false;
        if (fast)
        {
            actualSpeed = slamSpeed;
            AudioManager.PlaySound(slamSound, audioSource);
        }
        else
        {
            AudioManager.PlaySound(closeSound);
        }
    }

    public void Lock()
    {
        unlocked = false;
    }

    public void Unlock()
    {
        unlocked = true;
    }

    public void StartArena()
    {
        if (!isArenaLocked)
        {
            Slam();
            isArenaLocked = true;
            Debug.Log("Door locked in Arena mode.");
        }
        else
        {
            Debug.LogWarning("Door already in Arena mode. Did you mean EndArena()?");
        }
    }

    public void EndArena()
    {
        if (isArenaLocked)
        {
            Unlock();
            isArenaLocked = false;
            Debug.Log("Arena mode ended.");
        }
        else
        {
            Debug.LogWarning("Door is not in Arena mode. Did you mean StartArena()?");
        }
    }
}