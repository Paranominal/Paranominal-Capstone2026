using UnityEngine;
using UnityEngine.InputSystem;

// NOTE (interaction prompt system): this is a world event, not a player interaction, so it no
// longer routes through IInteractable. Scanning the lock object unlocks the door directly.
public class ScannableLock : MonoBehaviour
{
    public ALTScannableObject lockObject;
    private ALTGrimoire grimoire;

    // EDIT (interaction prompt system): was a generic IInteractable target calling Interact().
    // Now a direct Door reference calling Unlock(), since Interact() means "the player pressed the
    // button" and is a toggle. Worth generalising to an IUnlockable interface later.
    public Door targetDoor;

    public bool destroysItem;
    private bool hasFired = false;   // EDIT: guards the unlock so it only runs once, regardless of destroysItem

    void Start()
    {
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
        if (targetDoor == null)
        {
            targetDoor = GetComponentInChildren<Door>();
        }
    }

    void Update()
    {
        if (hasFired || targetDoor == null || lockObject == null)
        {
            return;
        }

        if (grimoire.CompareEntry(lockObject.entry))
        {
            targetDoor.Unlock();
            hasFired = true;

            if (destroysItem)
            {
                Destroy(lockObject.gameObject);
            }
        }
    }
}