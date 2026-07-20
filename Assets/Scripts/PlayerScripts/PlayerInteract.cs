using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCollect : MonoBehaviour
{
    [SerializeField] private PlayerLook look;
    [SerializeField] private InputActionReference collectAction;
    [SerializeField] private LayerMask interactablesLayers;
    [SerializeField] private LayerMask collectiblesLayer;
    private CollectibleObject currentTarget;
    public bool debugMode; 
    void Update()
    {
        Hover();
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

    void DoDebugLog()
    {
        Debug.Log($"Looking at: [{look.LookingAt()}]");
        Debug.Log($"Currently hovering: [{currentTarget}]");
    }
}
