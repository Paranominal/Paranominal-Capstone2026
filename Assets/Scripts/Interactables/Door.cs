using UnityEngine;
using UnityEngine.InputSystem;

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
    public float speed = 10;
    private Quaternion targetRotation;
    public float openAngle = -90;
    public float ajarAngle = -20;
    public float closedAngle = 0;
    public Collider doorCollider;
    private Quaternion startAngle;
    public bool isArenaLocked;
    public float ajarDistance = 3;
    private PlayerMover player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");
        targetRotation = transform.rotation;
        startAngle = transform.rotation;
        if (doorCollider == null)
        {
            doorCollider = GetComponentInChildren<Collider>();
        }
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMover>();    //there is no failsafe here if there's more than one playermover in the scene. don't fuck this up.
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
                    targetRotation = startAngle * Quaternion.AngleAxis(openAngle, transform.up);
                    state = doorState.open;
                }
                else
                {
                    targetRotation = startAngle * Quaternion.AngleAxis(ajarAngle, transform.up);
                    state = doorState.ajar;
                }

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

        if (Vector3.Distance(player.transform.position, transform.position) > ajarDistance && state == doorState.open)
        {
            targetRotation = startAngle * Quaternion.AngleAxis(ajarAngle, transform.up);
            state = doorState.ajar;
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
        targetRotation = startAngle * Quaternion.AngleAxis(closedAngle, transform.up);
        state = doorState.closed;
        if (locked)
        {
            unlocked = false;
        }
    }

    public void Unlock()
    {
        Interact();
    }

    public void StartArena()
    {
        if (!isArenaLocked)
        {
            Close();
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
