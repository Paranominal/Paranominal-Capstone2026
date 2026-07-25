using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Finds the interactable the player is looking at (proximity + look direction + line-of-sight),
// drives the HUD and world-space prompt presenters, and routes the interact button to that target.
public class InteractionFocusController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float outerRadius = 4f;
    [SerializeField] private float innerRadius = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float lookThreshold = 0.6f;

    [Header("Presenters")]
    [SerializeField] private HudPromptPresenter hudPresenter;
    [SerializeField] private WorldSpacePromptPresenter worldPresenter;

    [Header("Input")]
    [SerializeField] private string interactActionName = "Collect";

    private InputAction interactAction;
    private InteractionContext context;
    private Camera cam;
    private bool suspended;

    void Start()
    {
        cam = Camera.main;
        interactAction = InputSystem.actions.FindAction(interactActionName);

        // EDIT (auto-resolve): fallback for cross-prefab references.
        if (hudPresenter == null)
            hudPresenter = FindAnyObjectByType<HudPromptPresenter>();
        if (worldPresenter == null)
            worldPresenter = FindAnyObjectByType<WorldSpacePromptPresenter>();

        Inventory inventory = FindAnyObjectByType<Inventory>();

        context = new InteractionContext
        {
            player = transform,
            camera = cam,
            grimoire = ALTGrimoire.instance,
            inventory = inventory,
        };

        if (ALTGrimoire.instance != null)
            ALTGrimoire.instance.OnGrimoireToggled += OnGrimoireToggled;
    }

    void OnDestroy()
    {
        if (ALTGrimoire.instance != null)
            ALTGrimoire.instance.OnGrimoireToggled -= OnGrimoireToggled;
    }

    private void OnGrimoireToggled(bool grimoireOpen)
    {
        suspended = grimoireOpen;
        hudPresenter?.SetVisible(!grimoireOpen);
        worldPresenter?.SetVisible(!grimoireOpen);
    }

    void Update()
    {
        if (suspended) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        context.camera = cam;
        if (context.grimoire == null) context.grimoire = ALTGrimoire.instance;
        if (context.inventory == null) context.inventory = FindAnyObjectByType<Inventory>();

        IInteractable best = null;
        float bestStrength = 0f;

        Vector3 camPos = cam.transform.position;
        Vector3 camFwd = cam.transform.forward;

        Collider[] hits = Physics.OverlapSphere(camPos, outerRadius, interactableMask);
        foreach (Collider col in hits)
        {
            IInteractable it = col.GetComponentInParent<IInteractable>();
            if (it == null) continue;

            // ClosestPoint doesn't support non-convex MeshColliders; fall back to bounds.
            Vector3 point;
            if (col is MeshCollider mc && !mc.convex)
                point = col.bounds.ClosestPoint(camPos);
            else
                point = col.ClosestPoint(camPos);

            float dist = Vector3.Distance(camPos, point);
            float proximity = Mathf.InverseLerp(outerRadius, innerRadius, dist);

            Vector3 dir = (point - camPos).normalized;
            float look = Mathf.InverseLerp(lookThreshold, 1f, Vector3.Dot(camFwd, dir));

            float strength = proximity * look;
            if (strength <= bestStrength) continue;

            if (Physics.Linecast(camPos, point, obstructionMask)) continue;

            best = it;
            bestStrength = strength;
        }

        DrivePresenters(best, bestStrength);

        if (best != null && bestStrength > 0f && interactAction != null && interactAction.WasReleasedThisFrame())
        {
            best.Interact(context);
        }
    }

    private void DrivePresenters(IInteractable focused, float strength)
    {
        if (focused == null || strength <= 0f)
        {
            hudPresenter?.Clear();
            worldPresenter?.Clear();
            return;
        }

        InteractionPrompt prompt = focused.ResolvePrompt(context);
        switch (prompt.surface)
        {
            case PromptSurface.Hud:
                hudPresenter?.SetTarget(prompt, strength);
                worldPresenter?.Clear();
                break;
            case PromptSurface.WorldSpace:
                worldPresenter?.SetTarget(prompt, strength);
                hudPresenter?.Clear();
                break;
            default:
                hudPresenter?.Clear();
                worldPresenter?.Clear();
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, outerRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}
