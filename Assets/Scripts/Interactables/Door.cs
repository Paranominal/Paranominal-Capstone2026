using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour, IInteractable
{
    public enum doorState
    {
        open,
        ajar,
        closed,
    }

    public LayerMask interactable;
    InputAction collectAction;
    public bool unlocked;
    public doorState state;
    public float speed = 5;
    public float slamSpeed = 20f;
    private Quaternion targetRotation;
    private float currentSpeed;
    public float openAngle = -90;
    public float ajarAngle = -20;
    public float closedAngle = 0;
    public Collider doorCollider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundDataSO slamSound;
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private SoundDataSO lockedSound;

    // Caches references and sets the initial target rotation.
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");
        targetRotation = transform.rotation;
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
            if (collectAction.WasReleasedThisFrame() && unlocked && GetComponentInChildren<Collider>() == hit.collider)
            {
                if (state != doorState.open)
                {
                    targetRotation = Quaternion.AngleAxis(openAngle, transform.up);
                    state = doorState.open;
                    AudioManager.PlaySound(openSound, audioSource);
                }
                else
                {
                    targetRotation = Quaternion.AngleAxis(ajarAngle, transform.up);
                    state = doorState.ajar;
                    AudioManager.PlaySound(closeSound, audioSource);
                }
            }
            else if (collectAction.WasReleasedThisFrame() && !unlocked && GetComponentInChildren<Collider>() == hit.collider)
            {
                AudioManager.PlaySound(lockedSound, audioSource);
            }
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, currentSpeed * Time.deltaTime);

        if (transform.rotation != targetRotation && doorCollider.enabled)
        {
            doorCollider.enabled = false;
        }
        else if (transform.rotation == targetRotation && !doorCollider.enabled)
        {
            doorCollider.enabled = true;
            currentSpeed = speed;
        }
    }

    public void Interact()
    {
        if (!unlocked)
        {
            unlocked = true;
            Debug.Log("The sound of a door unlocking. Woah so immersive.");
        }
    }

    public void Close()
    {
        Close(true);
    }

    public void Close(bool locked)
    {
        targetRotation = Quaternion.AngleAxis(closedAngle, transform.up);
        state = doorState.closed;

        if (locked)
        {
            unlocked = false;
        }
    }

    // Locks the door if it is already closed, or slams it shut if it is open or ajar.
    public void LockOrSlam()
    {
        if (state == doorState.closed)
        {
            unlocked = false;
        }
        else
        {
            Slam();
        }
    }

    // Slams the door shut at an increased speed and locks it.
    // Plays the slam sound spatially if one is assigned.
    public void Slam()
    {
        currentSpeed = slamSpeed;
        Close(true);

        if (slamSound != null && audioSource != null)
        {
            AudioManager.PlaySound(slamSound, audioSource);
        }
    }
}
