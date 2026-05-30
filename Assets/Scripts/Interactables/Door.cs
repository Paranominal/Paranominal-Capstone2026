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
    InputAction collectAction;  // this could be rebound to a different action if you prefer
    public bool unlocked;   // at the moment the door can theoretically be "locked" open but thats neither here nor there
    public doorState state;
    public float speed = 5;
    private Quaternion targetRotation;
    public float openAngle = -90;
    public float ajarAngle = -20;
    public float closedAngle = 0;
    public Collider doorCollider;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private SoundDataSO lockedSound;
    [SerializeField] private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");
        targetRotation = transform.rotation;
        if (doorCollider == null)
        {
            doorCollider = GetComponentInChildren<Collider>();
        }
    }

    // Update is called once per frame
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

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, speed * Time.deltaTime);
        if (transform.rotation != targetRotation && doorCollider.enabled)
        {
            doorCollider.enabled = false;
        }
        else if (transform.rotation == targetRotation && !doorCollider.enabled)
        {
            doorCollider.enabled = true;
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
        Close(true);    //this can be set to false depending on what you want the default behaviour to be
    }

    public void Close(bool locked)  //locked bool determines if door locks on close
    {
        targetRotation = Quaternion.AngleAxis(closedAngle, transform.up);
        state = doorState.closed;
        if (locked)
        {
            unlocked = false;
        }
    }
}
