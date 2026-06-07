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

    public LayerMask interactable;
    InputAction collectAction;  // this could be rebound to a different action if you prefer

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
    private Raycaster raycaster;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO slamSound;
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private SoundDataSO lockedSound;

    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");

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
        if (raycaster == null)
        {
            raycaster = FindAnyObjectByType<Raycaster>();
        }
    }

    // Handles interaction input and lerps the door toward its target rotation.
    void Update()
    {
        if (Physics.Raycast(raycaster.Ray, out RaycastHit hit, 1000f, interactable))
        {
            if (collectAction.WasReleasedThisFrame() && GetComponentInChildren<Collider>() == hit.collider)
            {
                TryDoor();
            }
        }

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

    public void Interact()  //This is an IInteract Interface function. It should only ever be called by Interaction objects unless you know what you are doing.
    {
        if (!unlocked)
        {
            AudioManager.PlaySound(lockedSound, audioSource);
            return;
        }
    }

    public void TryDoor()   //attempts to toggle the door's state
    {
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
    public void Unlock()
    {
        Interact();
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