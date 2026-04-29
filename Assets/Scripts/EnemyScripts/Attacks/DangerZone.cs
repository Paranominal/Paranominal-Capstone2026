using UnityEngine;

/// <summary>
/// Visual telegraph for an incoming area attack. Positions and scales the
/// existing danger-zone GameObject (with its shader) and despawns itself
/// once the telegraph window has elapsed.
///
/// This script does not deal damage; it is purely the visual indicator.
/// Damage is handled by AoEAttack, which spawns and owns this object.
/// </summary>
public class DangerZone : MonoBehaviour
{
    [Header("Scaling")]
    [Tooltip("If true, transform.localScale.x and .z will be set to (radius * 2). " +
             "Disable this if your danger-zone prefab handles its own sizing in the shader.")]
    [SerializeField] private bool scaleToRadius = true;

    [Tooltip("Multiplier applied on top of the radius-based scaling. Tweak if your " +
             "prefab's mesh is not a unit-sized quad/disc.")]
    [SerializeField] private float scaleMultiplier = 1f;

    private float lifetime;
    private float elapsed;
    private bool initialized;

    /// <summary>
    /// Place the zone in the world and start its countdown.
    /// Called by AoEAttack right after instantiation.
    /// </summary>
    public void Show(Vector3 position, float radius, float duration)
    {
        transform.position = position;

        if (scaleToRadius)
        {
            float diameter = radius * 2f * scaleMultiplier;
            //preserve y-scale so vertically-extruded meshes still render correctly
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(diameter, currentScale.y, diameter);
        }

        lifetime = Mathf.Max(0.01f, duration);
        elapsed = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Allow the owning attack to clean up the zone early
    /// (e.g. if the attack is interrupted by a stagger).
    /// </summary>
    public void Cancel()
    {
        Destroy(gameObject);
    }
}
