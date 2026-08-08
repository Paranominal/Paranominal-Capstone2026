using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Two-pass interaction detection.
// Pass 1 (proximity): OverlapSphere finds nearby interactables, drives floating prompts
//   for informational labels (no actionName) on objects that have a PromptAnchor.
// Pass 2 (aim): reads the shared Raycaster hit, drives HUD prompts for actionable
//   labels (has actionName) and routes interact input.
// EDIT (raycaster-consolidation): aim detection uses Raycaster.Instance instead of its own ray.
// EDIT (interaction-rework): proximity and aim are now independent; both can show simultaneously.
// EDIT (screen-space-prompts): floating prompts use a pooled screen-space system.
// EDIT (prompt-simplification): presentation inferred from prompt fields + PromptAnchor
//   instead of an explicit PromptSurface enum.
public class InteractionFocusController : MonoBehaviour
{
    [Header("Proximity Detection (Floating Prompts)")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float proximityRadius = 4f;

    [Header("Aim Detection (HUD Prompts)")]
    [SerializeField] private float interactionRange = 10f;

    [Header("Presenters")]
    [SerializeField] private HudPromptPresenter hudPresenter;
    [SerializeField] private ScreenSpacePromptPool promptPool;

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

        if (hudPresenter == null)
            hudPresenter = FindAnyObjectByType<HudPromptPresenter>();
        if (promptPool == null)
            promptPool = FindAnyObjectByType<ScreenSpacePromptPool>();

        Inventory inventory = FindAnyObjectByType<Inventory>();

        context = new InteractionContext
        {
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
        promptPool?.SetVisible(!grimoireOpen);
    }

    void Update()
    {
        if (suspended) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (context.inventory == null) context.inventory = FindAnyObjectByType<Inventory>();

        Vector3 camPos = cam.transform.position;

        // -- Pass 1: Proximity (floating prompts for informational labels) --
        DriveWorldPrompt(camPos);

        // -- Pass 2: Aim via Raycaster (HUD prompts + interact input) --
        DriveAimPrompt();
    }

    // Summary: OverlapSphere for nearby interactables. For each one that resolves an
    // informational prompt (no actionName) and has a PromptAnchor, shows a floating label.
    private void DriveWorldPrompt(Vector3 camPos)
    {
        Collider[] hits = Physics.OverlapSphere(camPos, proximityRadius, interactableMask);
        foreach (Collider col in hits)
        {
            IInteractable it = col.GetComponent<IInteractable>();
            if (it == null)
                it = col.GetComponentInParent<IInteractable>();
            if (it == null) continue;

            InteractionPrompt prompt = it.ResolvePrompt(context);
            if (!prompt.HasPrompt) continue;
            // Actionable prompts are handled by aim, not proximity.
            if (!string.IsNullOrEmpty(prompt.actionName)) continue;

            PromptAnchor anchor = it.gameObject.GetComponentInChildren<PromptAnchor>();
            if (anchor == null) continue;

            // ClosestPoint fallback for non-convex MeshColliders.
            Vector3 point;
            if (col is MeshCollider mc && !mc.convex)
                point = col.bounds.ClosestPoint(camPos);
            else
                point = col.ClosestPoint(camPos);

            float dist = Vector3.Distance(camPos, point);
            float proximity = Mathf.InverseLerp(proximityRadius, 0f, dist);
            if (proximity <= 0f) continue;
            if (Physics.Linecast(camPos, point, obstructionMask)) continue;

            promptPool?.Show(
                it.gameObject.GetInstanceID(),
                anchor.transform.position,
                prompt.label,
                proximity);
        }

        promptPool?.Flush();
    }

    // Summary: Reads the shared Raycaster hit. Shows HUD prompt for actionable labels
    // and routes interact input.
    private void DriveAimPrompt()
    {
        Raycaster raycaster = Raycaster.Instance;
        if (raycaster == null || !raycaster.HasHit)
        {
            hudPresenter?.Clear();
            return;
        }

        if (raycaster.Hit.distance > interactionRange)
        {
            hudPresenter?.Clear();
            return;
        }

        IInteractable aimed = raycaster.Hit.collider.GetComponent<IInteractable>();
        if (aimed == null)
            aimed = raycaster.Hit.collider.GetComponentInParent<IInteractable>();

        if (aimed == null)
        {
            hudPresenter?.Clear();
            return;
        }

        InteractionPrompt prompt = aimed.ResolvePrompt(context);
        if (prompt.HasPrompt && !string.IsNullOrEmpty(prompt.actionName))
        {
            hudPresenter?.SetTarget(prompt, 1f);
        }
        else
        {
            hudPresenter?.Clear();
        }

        // Route interact input regardless of prompt type.
        if (interactAction != null && interactAction.WasReleasedThisFrame())
        {
            aimed.Interact(context);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}
