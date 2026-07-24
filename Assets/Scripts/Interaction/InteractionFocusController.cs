using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Finds the interactable the player is looking at (proximity + look direction + line-of-sight),
// drives the HUD and world-space prompt presenters, and routes the interact button to that target.
// Assumes a single focused interactable at a time.
public class InteractionFocusController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask obstructionMask;      // geometry that blocks line-of-sight (walls etc.)
    [SerializeField] private float outerRadius = 4f;         // prompt starts fading in at this distance
    [SerializeField] private float innerRadius = 2f;         // prompt fully faded in at this distance (and closer)
    [Range(0f, 1f)]
    [SerializeField] private float lookThreshold = 0.6f;     // how directly you must look (1 = dead-on, lower = more forgiving)

    [Header("Presenters")]
    [SerializeField] private HudPromptPresenter hudPresenter;
    [SerializeField] private WorldSpacePromptPresenter worldPresenter;

    [Header("Input")]
    [SerializeField] private string interactActionName = "Collect";

    private InputAction interactAction;
    private InteractionContext context;
    private Camera cam;
    private bool suspended;   // true while the grimoire is open

    void Start()
    {
        cam = Camera.main;
        interactAction = InputSystem.actions.FindAction(interactActionName);

        context = new InteractionContext
        {
            player = transform,
            camera = cam,
            grimoire = ALTGrimoire.instance,
        };

        // Prompts are meaningless while the grimoire is up, so mute both surfaces with it.
        if (ALTGrimoire.instance != null)
            ALTGrimoire.instance.OnGrimoireToggled += OnGrimoireToggled;
    }

    void OnDestroy()
    {
        if (ALTGrimoire.instance != null)
            ALTGrimoire.instance.OnGrimoireToggled -= OnGrimoireToggled;
    }

    // Summary: Mutes both prompt surfaces while the grimoire is open.
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

        if (Keyboard.current.f1Key.wasPressedThisFrame && context.grimoire != null)
        {
            foreach (ALTGrimoireEntry e in context.grimoire.entries)
                Debug.Log($"entryName=[{e.entryName}] collected={e.collected}");
        }

        IInteractable best = null;
        float bestStrength = 0f;

        Vector3 camPos = cam.transform.position;
        Vector3 camFwd = cam.transform.forward;

        Collider[] hits = Physics.OverlapSphere(camPos, outerRadius, interactableMask);
        foreach (Collider col in hits)
        {
            IInteractable it = col.GetComponentInParent<IInteractable>();
            if (it == null) continue;

            Vector3 point = col.ClosestPoint(camPos);

            float dist = Vector3.Distance(camPos, point);
            float proximity = Mathf.InverseLerp(outerRadius, innerRadius, dist);   // 0 at outer, 1 at inner

            Vector3 dir = (point - camPos).normalized;
            float look = Mathf.InverseLerp(lookThreshold, 1f, Vector3.Dot(camFwd, dir));

            float strength = proximity * look;
            if (strength <= bestStrength) continue;   // can't beat current best, skip the LOS cost

            // Line-of-sight: blocked if any obstruction sits between camera and the point.
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

    // Summary: Sends the focused prompt to whichever surface it wants and clears the other.
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
