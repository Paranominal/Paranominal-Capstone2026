using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Two-pass detection each frame.
// Pass 1 (proximity): OverlapSphere finds nearby WorldLabel components and feeds their
//   names to the ScreenSpacePromptPool as floating labels.
// Pass 2 (aim): reads the shared Raycaster hit, drives HUD prompts for actionable
//   interactions and routes interact input.
// EDIT (raycaster-consolidation): aim detection uses Raycaster.Instance instead of its own ray.
// EDIT (interaction-rework): proximity and aim are independent; both can show simultaneously.
// EDIT (screen-space-prompts): floating prompts use a pooled screen-space system.
// EDIT (label-split): floating labels now come from WorldLabel, not IInteractable.
public class InteractionFocusController : MonoBehaviour
{
    [Header("Proximity Detection (Floating Labels)")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask obstructionMask;
    [Tooltip("Maximum distance at which floating labels begin to fade in.")]
    [SerializeField] private float proximityRadius = 4f;
    [Tooltip("Distance at which floating labels reach full opacity.")]
    [SerializeField] private float labelFullOpacityDistance = 1.5f;

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

        // -- Pass 1: Proximity (floating name labels from WorldLabel components) --
        DriveWorldLabels(camPos);

        // -- Pass 2: Aim via Raycaster (HUD prompts + interact input) --
        DriveAimPrompt();
    }

    // Summary: OverlapSphere for nearby WorldLabel components. Shows a floating name
    // label for each one, keyed by instance ID.
    // EDIT (label-split): no longer queries IInteractable; uses WorldLabel directly.
    private void DriveWorldLabels(Vector3 camPos)
    {
        Collider[] hits = Physics.OverlapSphere(camPos, proximityRadius, interactableMask);
        foreach (Collider col in hits)
        {
            WorldLabel label = col.GetComponent<WorldLabel>();
            if (label == null)
                label = col.GetComponentInChildren<WorldLabel>();
            if (label == null) continue;
            if (string.IsNullOrEmpty(label.displayName)) continue;

            // ClosestPoint fallback for non-convex MeshColliders.
            Vector3 point;
            if (col is MeshCollider mc && !mc.convex)
                point = col.bounds.ClosestPoint(camPos);
            else
                point = col.ClosestPoint(camPos);

            float dist = Vector3.Distance(camPos, point);
            float proximity = Mathf.Clamp01(Mathf.InverseLerp(proximityRadius, labelFullOpacityDistance, dist));
            if (proximity <= 0f) continue;
            if (Physics.Linecast(camPos, point, obstructionMask)) continue;

            promptPool?.Show(
                label.GetInstanceID(),
                label.transform.position,
                label.displayName,
                proximity);
        }

        promptPool?.Flush();
    }

    // Summary: Reads the shared Raycaster hit. Shows HUD prompt for actionable
    // interactions and routes interact input.
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
        if (prompt.HasPrompt)
        {
            hudPresenter?.SetTarget(prompt, 1f);
        }
        else
        {
            hudPresenter?.Clear();
        }

        // Route interact input regardless of prompt.
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