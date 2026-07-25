using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCollect : MonoBehaviour
{
    [SerializeField] private PlayerLook look;
    [SerializeField] private Inventory inventory;
    [SerializeField] private InputActionReference collectAction;
    // [SerializeField] private LayerMask interactablesLayers;
    // [SerializeField] private LayerMask collectiblesLayer;
    private CollectibleObject currentTarget;
    public bool debugMode; 
    void Update()
    {
        Hover();
        if (collectAction.action.WasPressedThisFrame()) Collect(currentTarget);
        if (debugMode) DoDebugLog();
    }
    void Hover()
    {
        if (look.LookingAt() == null)
        {
            if (currentTarget != null) currentTarget.Deactivate();
            currentTarget = null;
            return;
        }

        CollectibleObject target = look.LookingAt().GetComponent<CollectibleObject>();
        if (target == null | target == currentTarget) return;
        else
        {
            if (currentTarget != null) currentTarget.Deactivate();
            target.Activate();
            currentTarget = target;
        }
    }
    void Collect(CollectibleObject collectible)
    {
        if (collectible == null) return;
        if (collectible.dialogue != null) inventory.Add(collectible.gameObject, true, collectible.dialogue);
        else inventory.Add(collectible.gameObject, true);
    }

    void DoDebugLog()
    {
        Debug.Log($"Looking at: [{look.LookingAt()}]");
        Debug.Log($"Currently hovering: [{currentTarget}]");
    }
}
