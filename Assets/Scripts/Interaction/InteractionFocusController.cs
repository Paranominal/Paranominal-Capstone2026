using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Two-pass interaction detection.
// Pass 1 (proximity): OverlapSphere finds nearby interactables, drives WorldSpace prompts.
// Pass 2 (aim): reads the shared Raycaster hit, drives HUD prompts and routes interact input.
// EDIT (raycaster-consolidation): aim detection uses Raycaster.Instance instead of its own ray.
// EDIT (interaction-rework): proximity and aim are now independent; both presenters can show simultaneously.
public class InteractionFocusController : MonoBehaviour
{
    [Header("Proximity Detection (WorldSpace Prompts)")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float proximityRadius = 4f;

    [Header("Aim Detection (HUD Prompts)")]
    [SerializeField] private float interactionRange = 10f;

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

        if (hudPresenter == null)
            hudPresenter = FindAnyObjectByType<HudPromptPresenter>();
        if (worldPresenter == null)
            worldPresenter = FindAnyObjectByType<WorldSpacePromptPresenter>();

        Inventory inventory = FindAnyObjectByType<Inventory>();

        context = new InteractionContext
        {
            player = transform,
            camera = cam,
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
        if (context.inventory == null) context.inventory = FindAnyObjectByType<Inventory>();

        Vector3 camPos = cam.transform.position;

        // -- Pass 1: Proximity (WorldSpace prompts) --
        DriveWorldPrompt(camPos);

        // -- Pass 2: Aim via Raycaster (HUD prompts + interact input) --
        DriveAimPrompt();
    }

    // Summary: OverlapSphere for nearby interactables. Shows the nearest WorldSpace prompt.
    private void DriveWorldPrompt(Vector3 camPos)
    {
        IInteractable bestWorld = null;
        float bestProximity = 0f;
        InteractionPrompt bestPrompt = InteractionPrompt.None;

        Collider[] hits = Physics.OverlapSphere(camPos, proximityRadius, interactableMask);
        foreach (Collider col in hits)
        {
            IInteractable it = col.GetComponent<IInteractable>();
            if (it == null)
                it = col.GetComponentInParent<IInteractable>();
            if (it == null) continue;

            InteractionPrompt prompt = it.ResolvePrompt(context);
            if (prompt.surface != PromptSurface.WorldSpace) continue;

            // ClosestPoint fallback for non-convex MeshColliders.
            Vector3 point;
            if (col is MeshCollider mc && !mc.convex)
                point = col.bounds.ClosestPoint(camPos);
            else
                point = col.ClosestPoint(camPos);

            float dist = Vector3.Distance(camPos, point);
            float proximity = Mathf.InverseLerp(proximityRadius, 0f, dist);

            if (proximity <= bestProximity) continue;
            if (Physics.Linecast(camPos, point, obstructionMask)) continue;

            bestWorld = it;
            bestProximity = proximity;
            bestPrompt = prompt;
        }

        if (bestWorld != null && bestProximity > 0f)
        {
            worldPresenter?.SetTarget(bestPrompt, bestProximity);
        }
        else
        {
            worldPresenter?.Clear();
        }
    }

    // Summary: Reads the shared Raycaster hit. Shows HUD prompt for the aimed interactable
    // and routes interact input to it.
    private void DriveAimPrompt()
    {
        Raycaster raycaster = Raycaster.Instance;
        if (raycaster == null || !raycaster.HasHit)
        {
            hudPresenter?.Clear();
            return;
        }

        // Range check against our own interaction distance.
        if (raycaster.Hit.distance > interactionRange)
        {
            hudPresenter?.Clear();
            return;
        }

        // EDIT (interaction-rework): check the hit collider's own object first,
        // then walk up. Prevents parent-shadowing (e.g. Lock on a Door child object).
        IInteractable aimed = raycaster.Hit.collider.GetComponent<IInteractable>();
        if (aimed == null)
            aimed = raycaster.Hit.collider.GetComponentInParent<IInteractable>();

        if (aimed == null)
        {
            hudPresenter?.Clear();
            return;
        }

        InteractionPrompt prompt = aimed.ResolvePrompt(context);
        if (prompt.surface == PromptSurface.Hud)
        {
            hudPresenter?.SetTarget(prompt, 1f);
        }
        else
        {
            hudPresenter?.Clear();
        }

        // Route interact input regardless of prompt surface.
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
