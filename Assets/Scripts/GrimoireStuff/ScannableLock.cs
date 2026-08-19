using UnityEngine;

// Michael feature (interaction-rework): updated target.Interact() to pass InteractionContext.
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
            grimoire = FindAnyObjectByType<ALTGrimoire>();

        target = GetComponentInChildren<IInteractable>();
    }

    void Update()
    {
        if (!isDestroyed)
        {
            if (grimoire.CompareEntry(lockObject.entry))
            {
                target.Interact(new InteractionContext());
                if (destroysItem)
                {
                    Destroy(lockObject.gameObject);
                    isDestroyed = true;
                }
            }
        }
    }
}
