using UnityEngine;
using UnityEngine.InputSystem;

public class ScannableLock : MonoBehaviour
{
    public ALTScannableObject lockObject;
    private ALTGrimoire grimoire;
    public IInteractable target;
    public bool destroysItem;
    private bool isDestroyed = false;

    void Start()
    {
        if (grimoire == null)
        {
            grimoire = FindAnyObjectByType<ALTGrimoire>();
        }
        target = GetComponentInChildren<IInteractable>();   // theres some glaring issues with this (namely you can't currently have more than one interaction type on one object) but it should work
    }

    void Update()
    {
        if (!isDestroyed)
        {
            if (grimoire.CompareEntry(lockObject.entry))
            {
                target.Interact();
                if (destroysItem)
                {
                    Destroy(lockObject.gameObject);
                    isDestroyed = true;
                }
            }
        }
        
    }
}
