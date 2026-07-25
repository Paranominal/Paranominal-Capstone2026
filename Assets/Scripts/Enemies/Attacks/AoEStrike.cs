using System.Collections;
using UnityEngine;

/// Summary:
/// A self-contained AoE strike. Sits on a prefab that represents one flavor
/// of attack (lightning, spikes, fire, explosion, etc) - the prefab carries
/// the visual, the Hitbox child collider, and this component to drive the
/// strike's lifetime.
///
/// AoEAttack spawns one of these at the snapshotted position after the
/// telegraph completes. The strike then runs its own activation sequence
/// and despawns itself - the spawner does not need to manage it further.
///
/// Each flavor configures its own intrinsic damage, hitbox active window,
/// and total lifetime on the prefab. Different flavors can support different
/// patterns:
///   - Instant: short hitbox window, short lifetime (lightning, explosion)
///   - Lingering: long hitbox window, may allow multi-hit on Hitbox (fire)
///   - Delayed: optional spawn delay before the hitbox activates (spikes)

public class AoEStrike : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Hitbox component on a child GameObject of this prefab. Active during " +
             "the strike's hitActiveDuration window.")]
    [SerializeField] private Hitbox hitbox;

    [Header("Timing")]
    [Tooltip("Delay before the hitbox activates after the strike spawns. Useful for " +
             "flavors where the visual telegraphs an instant before contact (e.g. spikes erupting).")]
    [SerializeField] private float hitActivationDelay = 0f;

    [Tooltip("How long the hitbox stays active. Short for instant strikes (~0.15s), " +
             "long for lingering effects like fire pools.")]
    [SerializeField] private float hitActiveDuration = 0.15f;

    [Tooltip("Total lifetime of this strike object before it self-destructs. Should be " +
             ">= hitActivationDelay + hitActiveDuration. Extra time lets the visual finish playing.")]
    [SerializeField] private float totalLifetime = 1f;

    [Header("Damage")]
    [Tooltip("Damage dealt by this strike. Each flavor's prefab carries its own value.")]
    [SerializeField] private int damage = 10;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private GameObject sourceOverride;

    private void Start()
    {
        StartCoroutine(StrikeSequence());
    }

    /// Summary:
    /// Optional: AoEAttack can call this immediately after instantiating to attribute
    /// the damage to the spawning enemy rather than the strike object itself.
    
    public void SetSource(GameObject source)
    {
        sourceOverride = source;
    }

    private IEnumerator StrikeSequence()
    {
        if (debugMode)
            Debug.Log($"[AoEStrike] Spawned at {transform.position} (damage {damage}).", this);

        //optional pre-strike delay (e.g. spike eruption windup baked into the strike itself)
        if (hitActivationDelay > 0f)
        {
            yield return new WaitForSeconds(hitActivationDelay);
        }

        //activate the hitbox
        if (hitbox != null)
        {
            GameObject source = sourceOverride != null ? sourceOverride : gameObject;
            DamageInfo info = new DamageInfo(damage, transform.position, Vector3.up, source);
            hitbox.Activate(info);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[AoEStrike] No Hitbox assigned on {gameObject.name} - " +
                             "strike will deal no damage.", this);
        }

        yield return new WaitForSeconds(hitActiveDuration);

        if (hitbox != null) hitbox.Deactivate();

        //wait out any remaining lifetime so the visual can finish
        float consumed = hitActivationDelay + hitActiveDuration;
        float remaining = totalLifetime - consumed;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        Destroy(gameObject);
    }
}
