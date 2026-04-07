using UnityEngine;
using UnityEngine.InputSystem;

public class Door : MonoBehaviour, IInteractable
{
    public bool clockwise;
    public LayerMask interactable;
    InputAction collectAction;  // this could be rebound to a different action if you prefer
    public bool unlocked;   // at the moment the door can theoretically be "locked" open but thats neither here nor there
    private bool open;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactable))
        {
            if (collectAction.WasReleasedThisFrame() && unlocked && GetComponentInChildren<Collider>() == hit.collider)
            {
                if (clockwise && !open || !clockwise && open)
                {
                    transform.Rotate(0, 90, 0);
                    open = !open;
                }
                else
                {
                    transform.Rotate(0, -90, 0);
                    open = !open;
                }
            }
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
}
