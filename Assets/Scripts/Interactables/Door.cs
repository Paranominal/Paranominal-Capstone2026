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
    InputAction collectAction;

    public bool unlocked;
    public DoorState state;
    public float speed = 5f;
    public float slamSpeed = 20f;
    public float arrivalThreshold = 0.5f;
    public float openAngle = -90f;
    public float ajarAngle = -20f;
    public Collider doorCollider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO slamSound;
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private SoundDataSO lockedSound;

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private float currentSpeed;
    private bool isMoving;

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
    }

    // Handles interaction input and lerps the door toward its target rotation.
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactable))
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