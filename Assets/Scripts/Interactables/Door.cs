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
    InputAction collectAction; // this could be rebound to a different action if you prefer

    public bool unlocked; // at the moment the door can theoretically be "locked" open but thats neither here nor there
    public DoorState state;
    public float speed = 5f;
    public float slamSpeed = 20f;
    public float arrivalThreshold = 0.5f; // Threshold to final position for re-enabling the door's collider after opening/closing.
    public float openAngle = -100f;
    public float ajarAngle = -20f;
    private Quaternion closedRotation; // Set when door starts - local transform value so the door isn't moving relative to the doorway parent object.
    private float actualSpeed;
    private Quaternion targetRotation;
    private float currentSpeed;
    private bool isMoving;
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

    // Gathers references and stores the local closed rotation as the baseline for all angle calculations.
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");

        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
        currentSpeed = speed;

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
        actualSpeed = speed;
    }

    // Handles interaction input and lerps the door toward its target rotation.
    void Update()
    {
        if (Physics.Raycast(raycaster.Ray, out RaycastHit hit, 1000f, interactable))
        {
            if (collectAction.WasReleasedThisFrame() && GetComponentInChildren<Collider>() == hit.collider)
            {
                Interact();
            }
        }

        if (isMoving)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, currentSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.localRotation, targetRotation) < arrivalThreshold)
            {
                transform.localRotation = targetRotation;
                currentSpeed = speed;
                isMoving = false;

                if (doorCollider != null)
                {
                    doorCollider.enabled = true;
                }
            }
            else if (doorCollider != null && doorCollider.enabled)
            {
                doorCollider.enabled = false;
            }
        }

        if (transform.rotation == targetRotation && actualSpeed != speed)   // used to reset speed whenever the door comes to a stop
        {
            actualSpeed = speed;
        }

        if (Vector3.Distance(player.transform.position, transform.position) > ajarDistance && state == DoorState.Open)
        {
            targetRotation = startAngle * Quaternion.AngleAxis(ajarAngle, transform.up);
            state = DoorState.Ajar;
        }
    }

    // Sets the door's state and derives the target local rotation from the closed baseline.
    protected void SetState(DoorState newState)
    {
        if (newState == state)
        {
            return;
        }

        state = newState;

        switch (newState)
        {
            case DoorState.Open:
                targetRotation = closedRotation * Quaternion.AngleAxis(openAngle, Vector3.up);
                break;
            case DoorState.Ajar:
                targetRotation = closedRotation * Quaternion.AngleAxis(ajarAngle, Vector3.up);
                break;
            case DoorState.Closed:
                targetRotation = closedRotation;
                break;
        }

        isMoving = true;
    }

    // Toggles the door open or closes it, depending on current state.
    public void Interact()
    {
        if (!unlocked)
        {
            AudioManager.PlaySound(lockedSound, audioSource);
            return;
        }

        if (state == DoorState.Open)
        {
            Close();
        }
        else
        {
            SetState(DoorState.Open);
            AudioManager.PlaySound(openSound, audioSource);
        }
    }

    // Closes the door and plays the close sound.
    public void Close()
    {
        SetState(DoorState.Closed);
        AudioManager.PlaySound(closeSound, audioSource);
    }

    // Locks the door.
    public void Lock()
    {
        unlocked = false;
    }

    // Unlocks the door.
    public void Unlock()
    {
        unlocked = true;
    }

    // Locks the door if already closed, or slams it shut if open or ajar.
    public void LockOrSlam()
    {
        if (state == DoorState.Closed)
        {
            Lock();
        }
        else
        {
            Slam();
        }
    }

    // Slams the door shut at increased speed, locks it, and plays the slam sound - calls SetState directly to avoid triggering the close sound via Close().
    public void Slam()
    {
        currentSpeed = slamSpeed;
        SetState(DoorState.Closed);
        Lock();
        AudioManager.PlaySound(slamSound, audioSource);
    }
}