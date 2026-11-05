using System.Collections.Generic;
using UnityEngine;

public class CollectionTrigger : MonoBehaviour
{
    private enum TriggerState { Activate, Deactivate }
    [SerializeField] private TriggerState onPickup = TriggerState.Activate;
    // [SerializeField] private bool activateOnPickup;
    [SerializeField] private bool animate;
    [SerializeField, ShowIf("animate", true), Range(0.1f, 10)] private float speed = 1;
    // multiple objects can activate things.
    [Tooltip("List of objects that can trigger the script.")]
    [SerializeField] private List<CollectibleObject> collectibles;
    private Animator animator;

    void Awake()
    {
        if (animate)
        {
            animator = GetComponent<Animator>();
            if (animator == null) Debug.LogWarning($"[{this}] Animate is set to ({animate}), but no Animator is on the Object!");
        }

        foreach (CollectibleObject item in collectibles)
        {
            // sub
            item.ObjectCollected += DoTrigger;
        }

        if (onPickup == TriggerState.Activate)
        {
            gameObject.SetActive(false);
        }
    }

    void DoTrigger()
    {
        if (onPickup == TriggerState.Activate)
        {
            gameObject.SetActive(true);
            if (animate) DoAnimation();
        }
        else
        {
            gameObject.SetActive(false);
            if (animate) DoAnimation();
        } 
        
        // unsub
        foreach (CollectibleObject item in collectibles)
        {
            item.ObjectCollected -= DoTrigger;
        }
    }

    void DoAnimation()
    {
        if (!animator) return;

        animator.speed = speed;

        if (onPickup == TriggerState.Activate) animator.SetTrigger("activate");
        else animator.SetTrigger("deactivate");
    }
}
