using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour, IInteractable
{
    public enum DoorState
    {
        Open,
        Ajar,
        Closed,
    }

    public LayerMask interactable;  // NOTE: no longer used by Door itself; interaction now runs through InteractionFocusController. Left in case it's referenced elsewhere.

    public bool unlocked;   // at the moment the door can theoretically be "locked" open but thats neither here nor there
    public bool isEncounterLocked;  // This is just for specifying that the door was locked by an encounter/arena - basically only needed if specific visuals will be implemented
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

    // EDIT (interaction prompt system): key + prompt configuration.
    [Header("Interaction Prompt")]
    public string requiredKeyName;          // Grimoire entry name that unlocks this door
    public bool consumesKey = true;         // whether using the key spends it from the Grimoire
    public bool hideUntilAttempted;         // hide the world "Locked" label until the player first tries the door
    public Transform promptAnchor;          // where the world-space "Locked" label floats (place over the lock)
    [HideInInspector] public bool revealed; // set once the player has attempted a hidden-lock door

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO slamSound;
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private SoundDataSO lockedSound;

    void Start()
    {
        // EDIT (interaction prompt system): removed collectAction / raycaster lookups; the focus
        // controller now owns interaction input and raycasting.
        targetRotation = transform.rotation;
        startAngle = transform.rotation;
        actualSpeed = speed;

        if (doorCollider == null)
        {
            doorCollider = GetComponentInChildren<Collider>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMover>();    //there is no failsafe here if there's more than one playermover in the scene. don't fuck this up.
        }
    }

    // Lerps the door toward its target rotation and handles auto-ajar.
    // EDIT (interaction prompt system): removed the raycast/input block; input is handled by InteractionFocusController.
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

        if (transform.rotation == targetRotation && actualSpeed != speed)   // used to reset speed whenever the door comes to a stop
        {
            actualSpeed = speed;
        }

        if (Vector3.Distance(player.transform.position, transform.position) > ajarDistance && state == DoorState.Open)
        {
            Ajar();
        }
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

    public void ForceOpen() //this will unlock the door in the process. does not play unlocking sound
    {
        unlocked = true;
        Open();
    }

    public void Ajar()
    {
        targetRotation = startAngle * Quaternion.AngleAxis(ajarAngle, transform.up);
        state = DoorState.Ajar;
        AudioManager.PlaySound(closeSound, audioSource);    //a seperate ajar and closed sound would be ideal
    }

    // Summary: Context-aware interaction. Encounter-locked doors stay shut; a held key unlocks a
    // key-locked door; an unlocked door toggles open/closed. Should only be called by the interaction system.
    // EDIT (interaction prompt system): was Interact(); now takes a context and folds in the old TryDoor + key-use logic.
    public void Interact(InteractionContext context)
    {
        Debug.Log($"required=[{requiredKeyName}] hasKey={context?.HasKey(requiredKeyName)}");
        if (context?.grimoire != null)
            foreach (ALTGrimoireEntry e in context.grimoire.entries)
                Debug.Log($"  entry=[{e.entryName}] collected={e.collected}");

        // Encounter/arena lock is a hard gate: keys do nothing while it's active.
        if (isArenaLocked)
        {
            AudioManager.PlaySound(lockedSound, audioSource);
            return;
        }

        if (!unlocked)
        {
            if (context != null && context.HasKey(requiredKeyName))
            {
                unlocked = true;
                if (consumesKey)
                {
                    context.ConsumeKey(requiredKeyName);
                }
                AudioManager.PlaySound(lockedSound, audioSource);   // TODO: a dedicated unlock sound would be nicer than reusing lockedSound
            }
            else
            {
                AudioManager.PlaySound(lockedSound, audioSource);
                if (hideUntilAttempted)
                {
                    revealed = true;   // from now on the world "Locked" label resolves
                }
            }
            return;
        }

        // Unlocked: toggle open/closed.
        if (state == DoorState.Open)
        {
            Ajar();
        }
        else
        {
            Open();
        }
    }

    // Summary: Chooses which prompt to show for the current door + player state.
    // EDIT (interaction prompt system): added for the prompt system.
    public InteractionPrompt ResolvePrompt(InteractionContext context)
    {
        // Encounter/arena lock always reads as "Locked" and ignores keys.
        if (isArenaLocked)
        {
            return new InteractionPrompt { surface = PromptSurface.WorldSpace, label = "Locked", anchor = promptAnchor };
        }

        if (unlocked)
        {
            return new InteractionPrompt { surface = PromptSurface.Hud, label = "Open", actionName = "Collect" };
        }

        if (context != null && context.HasKey(requiredKeyName))
        {
            return new InteractionPrompt { surface = PromptSurface.Hud, label = "Use Key", actionName = "Collect" };
        }

        // Locked, no key: optionally hidden until the player tries the door.
        if (hideUntilAttempted && !revealed)
        {
            return InteractionPrompt.None;
        }

        return new InteractionPrompt { surface = PromptSurface.WorldSpace, label = "Locked", anchor = promptAnchor };
    }

    public void TryDoor()   //attempts to toggle the door's state
    {
        // NOTE (interaction prompt system): no longer the interaction entry point (Interact handles that).
        // Left intact in case other scripts call it directly.
        if (unlocked)
        {
            if (state == DoorState.Open)
            {
                Ajar();
            }
            else
            {
                Open();
            }
        }
        else
        {
            AudioManager.PlaySound(lockedSound, audioSource);
        }
    }

    public void Slam()
    {
        Close(true, true);    //this can be set to false depending on what you want the default behaviour to be
    }

    public void Close()
    {
        Close(false, false);    //by default this closes calm and chill and nothing bad happens
    }

    public void Close(bool locked, bool fast)  //locked bool determines if door locks on close, fast bool determines if the speed is multiplied or not
    {
        targetRotation = startAngle * Quaternion.AngleAxis(closedAngle, transform.up);
        state = DoorState.Closed;
        if (locked)
        {
            unlocked = false;
        }
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

    // Locks the door.
    public void Lock()
    {
        unlocked = false;
    }

    // Unlocks the door.
    // EDIT (interaction prompt system): no longer routes through Interact (which now needs a context); unlocks directly.
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
