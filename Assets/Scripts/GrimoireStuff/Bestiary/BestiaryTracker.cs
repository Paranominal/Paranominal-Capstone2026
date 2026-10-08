using System.Collections.Generic;
using UnityEngine;

// Summary: Links an enemy to its Bestiary entry. Adds the entry the first time the player sees this enemy type, and records a kill when it dies. 
// "Seen" means inside the player camera's view, within range, with a clear line of sight.
// Takes a snapshot on sighting, and keeps checking if the record exists but has no image yet.
[RequireComponent(typeof(Enemy))]
public class BestiaryTracker : MonoBehaviour
{
    [SerializeField] private EnemyDefinition definition;

    [Header("Sighting")]
    [SerializeField] private float sightRange = 20f;
    [Tooltip("Seconds between sighting checks.")]
    [SerializeField] private float checkInterval = 0.2f;

    [Header("External Systems")]
    [SerializeField] private Bestiary bestiary;
    // EDIT (bestiary-snapshots): snapshot source.
    [SerializeField] private PhotoSnapshots snapshots;
    [Tooltip("Defaults to the MainCamera-tagged camera (PlayerCam).")]
    [SerializeField] private Camera playerCamera;

    private Enemy enemy;
    private Renderer[] visualRenderers;
    private bool discovered;
    private float checkTimer;
    // short wait between sighting and snapshot. Geometry that's just been disabled
    // (e.g. its collider goes before its visuals) can still show up in the photo otherwise.
    private const float SnapshotDelay = 0.25f;
    private bool snapshotPending;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        // Particles are skipped so VFX don't stretch the bounds used for the sight point and snapshot framing.
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
        // EDIT (bestiary-snapshots): cross-prefab fallback.
        if (snapshots == null)
            snapshots = FindAnyObjectByType<PhotoSnapshots>();
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

        // stop once the record exists with an image. A record without one (killed before being seen)
        // keeps this checking so the image gets filled in on the next sighting.
        if (HasCompleteRecord())
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

        // bounds calculated once and shared by the sight check and the snapshot.
        Bounds visualBounds = GetVisualBounds();
        if (CanPlayerSee(visualBounds.center))
        {
            // first sighting only starts the delay. The next check re-confirms the enemy is still in view before taking the snapshot.
            if (!snapshotPending)
            {
                snapshotPending = true;
                checkTimer = SnapshotDelay;
                return;
            }

            Texture snapshot = snapshots != null ? snapshots.TakeEnemySnapshot(visualBounds) : null;
            bestiary.Add(definition, snapshot);
            discovered = true;
        }
        else
        {
            // lost sight during the delay, start over on the next sighting.
            snapshotPending = false;
        }
    }

    // True when this type is in the Bestiary and nothing more is needed from a sighting.
    private bool HasCompleteRecord()
    {
        Bestiary.BestiaryRecord record = bestiary.GetRecord(definition);
        if (record == null) return false;

        // Without a snapshot source there's no image to wait for.
        return record.snapshot != null || snapshots == null;
    }

    // takes the target point instead of calculating it.
    private bool CanPlayerSee(Vector3 target)
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return false;
        }

        Vector3 cameraPosition = playerCamera.transform.position;

        if ((target - cameraPosition).sqrMagnitude > sightRange * sightRange) return false;

        // z <= 0 means behind the camera.
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(target);
        if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        LayerMask obstructionLayers = snapshots != null ? snapshots.SightObstructionLayers : (LayerMask)~0;

        // Hitting this enemy's own colliders counts as a clear view.
        if (Physics.SphereCast(cameraPosition, 0.2f, target - cameraPosition, out RaycastHit hit, sightRange, obstructionLayers, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(transform);

        return true;
    }

    // Bounds of the enemy's visible renderers. Used for the sight point (the pivot is usually at the feet, which low cover would block) and for framing the snapshot.
    private Bounds GetVisualBounds()
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

        return bounds;
    }

    private void HandleDied(Enemy deadEnemy)
    {
        if (bestiary != null && definition != null)
            bestiary.RecordKill(definition);
    }
}