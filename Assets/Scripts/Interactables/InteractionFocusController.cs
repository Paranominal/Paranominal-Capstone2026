using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Two-pass detection each frame.
// Pass 1 (proximity): OverlapSphere finds nearby WorldLabel components and feeds them to the WorldLabelPool. Handles both FloatingLabel and InteractionPrompt modes.
// Pass 2 (aim): reads the shared Raycaster hit, routes interact input.
public class InteractionFocusController : MonoBehaviour
{
    [Header("Proximity Detection (Floating Labels)")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask obstructionMask;
    [Tooltip("Maximum distance at which floating labels begin to fade in.")]
    [SerializeField] private float proximityRadius = 4f;
    [Tooltip("Distance at which floating labels reach full opacity.")]
    [SerializeField] private float labelFullOpacityDistance = 1.5f;

    [Header("Aim Detection (Interact Input)")]
    [SerializeField] private float interactionRange = 10f;

    [Header("Presenters")]
    [SerializeField] private WorldLabelPool labelPool;

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

        if (labelPool == null)
            labelPool = FindAnyObjectByType<WorldLabelPool>();

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
        labelPool?.SetVisible(!grimoireOpen);
    }

    void Update()
    {
        if (suspended) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (context.inventory == null) context.inventory = FindAnyObjectByType<Inventory>();

        Vector3 camPos = cam.transform.position;

        // Pass 1: Proximity (world-space labels for both FloatingLabel and InteractionPrompt)
        DriveWorldLabels(camPos);

        // Pass 2: Aim via Raycaster (interact input routing only, no HUD display)
        DriveAimInteraction();
    }

    // OverlapSphere for nearby WorldLabel components. Resolves content based on mode: FloatingLabel uses displayName, InteractionPrompt uses IInteractable.
    private void DriveWorldLabels(Vector3 camPos)
    {
        Collider[] hits = Physics.OverlapSphere(camPos, proximityRadius, interactableMask);
        foreach (Collider col in hits)
        {
            WorldLabel label = col.GetComponent<WorldLabel>();
            if (label == null)
                label = col.GetComponentInChildren<WorldLabel>();
            if (label == null) continue;

            // Resolve display text based on mode.
            string text;
            if (label.mode == WorldLabelMode.InteractionPrompt)
            {
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null)
                    interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                InteractionPrompt prompt = interactable.ResolvePrompt(context);
                if (!prompt.HasPrompt) continue;

                text = FormatPrompt(prompt);
            }
            else
            {
                if (string.IsNullOrEmpty(label.displayName)) continue;
                text = label.displayName;
            }

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

            // InteractionPrompt: fixed facing from parent transform, flipped if player is on the back side so the prompt is always readable.
            // FloatingLabel: rotation is ignored by the pool (it billboards instead).
            Quaternion rotation = Quaternion.identity;
            if (label.mode == WorldLabelMode.InteractionPrompt)
            {
                Vector3 toCamera = camPos - label.transform.position;
                float dot = Vector3.Dot(label.transform.parent.forward, toCamera);
                rotation = dot > 0f
                    ? label.transform.parent.rotation * Quaternion.Euler(0f, 180f, 0f)
                    : label.transform.parent.rotation;
            }

            labelPool?.Show(
                label.GetInstanceID(),
                label.transform.position,
                rotation,
                text,
                proximity,
                label.mode);
        }

        labelPool?.Flush();
    }

    // Reads the shared Raycaster hit. Routes interact input to the aimed IInteractable. No longer drives HUD display.
    private void DriveAimInteraction()
    {
        Raycaster raycaster = Raycaster.Instance;
        if (raycaster == null || !raycaster.HasHit) return;
        if (raycaster.Hit.distance > interactionRange) return;
        if (raycaster.Hit.collider == null) return;

        IInteractable aimed = raycaster.Hit.collider.GetComponent<IInteractable>();
        if (aimed == null) return;

        if (interactAction != null && interactAction.WasReleasedThisFrame())
        {
            aimed.Interact(context);
        }
    }

    // Formats an InteractionPrompt into a display string with keybind.
    private string FormatPrompt(InteractionPrompt prompt)
    {
        InputAction action = InputSystem.actions.FindAction(prompt.actionName);
        if (action != null)
        {
            string binding = action.GetBindingDisplayString(0);
            return $"[{binding}] {prompt.label}";
        }
        return prompt.label;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}