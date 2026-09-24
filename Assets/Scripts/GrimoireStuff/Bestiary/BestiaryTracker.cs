using System.Collections.Generic;
using UnityEngine;

// Summary: Links an enemy to its Bestiary entry. Adds the entry the first time the player sees this enemy type,
// and records a kill when it dies. "Seen" means inside the player camera's view, within range, with a clear line of sight.
[RequireComponent(typeof(Enemy))]
public class BestiaryTracker : MonoBehaviour
{
    [SerializeField] private EnemyDefinition definition;

    [Header("Sighting")]
    [SerializeField] private float sightRange = 25f;
    [Tooltip("Layers that can block line of sight. This enemy's own colliders are ignored automatically.")]
    [SerializeField] private LayerMask obstructionLayers = ~0;
    [Tooltip("Seconds between sighting checks.")]
    [SerializeField] private float checkInterval = 0.2f;

    [Header("External Systems")]
    [SerializeField] private Bestiary bestiary;
    [Tooltip("Defaults to the MainCamera-tagged camera (PlayerCam).")]
    [SerializeField] private Camera playerCamera;

    private Enemy enemy;
    private Renderer[] visualRenderers;
    private bool discovered;
    private float checkTimer;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        // Particles are skipped so VFX don't stretch the bounds used for the sight point.
        List<Renderer> renderers = new List<Renderer>();
        foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!(childRenderer is ParticleSystemRenderer))
                renderers.Add(childRenderer);
        }
        visualRenderers = renderers.ToArray();
    }

    private void Start()
    {
        if (bestiary == null)
            bestiary = FindAnyObjectByType<Bestiary>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (definition == null)
            Debug.LogWarning($"[{this}] No EnemyDefinition assigned, this enemy won't appear in the Bestiary.");
    }

    private void OnEnable()
    {
        if (enemy != null) enemy.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (discovered || definition == null || bestiary == null) return;

        // Another enemy of this type already discovered it, no need to keep checking.
        if (bestiary.HasDiscovered(definition))
        {
            discovered = true;
            return;
        }

        // Scaled time, so no checks run while paused.
        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        // Skip mid-spawn and mid-death so the entry (and later its snapshot) isn't taken during a dissolve.
        if (enemy.IsDying || enemy.CurrentState == Enemy.BehaviourState.Spawning) return;

        if (CanPlayerSee())
        {
            bestiary.Add(definition);
            discovered = true;
        }
    }

    private bool CanPlayerSee()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return false;
        }

        Vector3 target = GetVisualCentre();
        Vector3 cameraPosition = playerCamera.transform.position;

        if ((target - cameraPosition).sqrMagnitude > sightRange * sightRange) return false;

        // z <= 0 means behind the camera.
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(target);
        if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        // Hitting this enemy's own colliders counts as a clear view.
        if (Physics.Linecast(cameraPosition, target, out RaycastHit hit, obstructionLayers, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(transform);

        return true;
    }

    // Summary: Centre of the enemy's visible meshes. The transform pivot is usually at the feet, which low cover would block.
    private Vector3 GetVisualCentre()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        foreach (Renderer visualRenderer in visualRenderers)
        {
            if (visualRenderer == null || !visualRenderer.enabled) continue;

            if (!hasBounds)
            {
                bounds = visualRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(visualRenderer.bounds);
            }
        }

        return bounds.center;
    }

    private void HandleDied(Enemy deadEnemy)
    {
        if (bestiary != null && definition != null)
            bestiary.RecordKill(definition);
    }
}
