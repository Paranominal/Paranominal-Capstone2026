using System.Collections.Generic;
using UnityEngine;

/// Summary:
/// Reusable damage-dealing trigger volume. Sits on a child GameObject of an
/// attacker (e.g. a "HitBox" sphere on the Eye-bat). Attacks activate the
/// hitbox for a window, supplying the damage payload; the hitbox handles
/// trigger detection and routes hits to anything implementing IDamageable.
///
/// Activation patterns this supports:
///   - One-shot strike (Statue AoE: activate, deactivate after strikeDuration)
///   - Time-bounded (Eye-bat dive: activate during dive, deactivate at end)
///   - Always-on contact (Ghost: activate on spawn, never deactivate)
///   - Lingering DoT (configure allowMultipleHitsPerTarget + retriggerInterval)
/// Reusable damage-dealing trigger volume. Sits on a child GameObject of an
/// attacker (e.g. a "HitBox" sphere on the Eye-bat). Attacks activate the
/// hitbox for a window, supplying the damage payload; the hitbox handles
/// trigger detection and routes hits to anything implementing IDamageable.
///
/// Activation patterns this supports:
///   - One-shot strike (Statue AoE: activate, deactivate after strikeDuration)
///   - Time-bounded (Eye-bat dive: activate during dive, deactivate at end)
///   - Always-on contact (Ghost: activate on spawn, never deactivate)
///   - Lingering DoT (configure allowMultipleHitsPerTarget + retriggerInterval)
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Header("Hit Targets")]
    [Tooltip("Layers that this hitbox can damage. Other layers are ignored entirely.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Hit Behaviour")]
    [Tooltip("If false, each target can only be hit once per activation. " +
             "If true, the same target can be hit repeatedly (for DoT zones).")]
    [SerializeField] private bool allowMultipleHitsPerTarget = false;

    [Tooltip("When 'Allow Multiple Hits Per Target' is true, this is the minimum " +
             "time between hits on the same target. Ignored otherwise.")]
    [SerializeField] private float retriggerInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isActive;
    private DamageInfo currentDamage;
    //tracks per-target hit timestamps so we can support both single-hit
    //and DoT-style retriggering with one data structure
    private readonly Dictionary<IDamageable, float> hitHistory = new Dictionary<IDamageable, float>();
    private Collider triggerCollider;

    public bool IsActive => isActive;

    /// <summary>
    /// Fires whenever the hitbox makes contact with a collider on hitLayers, regardless of
    /// whether damage was actually dealt. Use this when an enemy needs to react to *any*
    /// contact - e.g. the Ghost destroying itself on touch even before PlayerStatus exists.
    /// </summary>
    public event System.Action<Collider> OnContact;

    /// <summary>
    /// Fires only when damage is successfully dealt to an IDamageable. Use this for hit
    /// reactions that depend on the damage actually landing - flashes, sound effects, etc.
    /// </summary>
    public event System.Action<IDamageable, DamageInfo> OnHit;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (!triggerCollider.isTrigger && debugMode)
        {
            Debug.LogWarning($"[Hitbox] Collider on {gameObject.name} is not set to trigger. " +
                             "Setting isTrigger = true at runtime.", this);
            triggerCollider.isTrigger = true;
        }

        //start inactive - attacks must explicitly activate
        triggerCollider.enabled = false;
    }

    /// <summary>
    /// Begin dealing damage. Targets entering or already inside the trigger
    /// will be hit (subject to the per-target hit rules).
    /// </summary>
    public void Activate(DamageInfo damage)
    {
        currentDamage = damage;
        hitHistory.Clear();
        isActive = true;
        triggerCollider.enabled = true;

        if (debugMode)
            Debug.Log($"[Hitbox] Activated on {gameObject.name} (damage: {damage.amount}).", this);
    }

    /// <summary>
    /// Stop dealing damage. The collider is disabled and hit history is cleared.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        triggerCollider.enabled = false;
        hitHistory.Clear();

        if (debugMode)
            Debug.Log($"[Hitbox] Deactivated on {gameObject.name}.", this);
    }

    //fires once when a collider enters the trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        TryDamage(other);
    }

    //fires every physics step while a collider is inside.
    //we only care about this for DoT-style hitboxes that retrigger over time;
    //single-hit hitboxes return early because the target is already in hitHistory.
    private void OnTriggerStay(Collider other)
    {
        if (!isActive) return;
        if (!allowMultipleHitsPerTarget) return;
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        //layer filter
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        //fires for any layer-matching contact, even if no IDamageable is found.
        //the Ghost uses this to die on touch regardless of player's damage system.
        OnContact?.Invoke(other);

        //find an IDamageable on the hit collider or any parent.
        //GetComponentInParent walks up the hierarchy, which is what we want -
        //the player's collider is often on a child of the root that holds PlayerStatus.
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            //placeholder behaviour while PlayerStatus doesn't exist yet:
            //log so we know the hitbox connected, then bail.
            if (debugMode)
                Debug.Log($"[Hitbox] Hit '{other.name}' but no IDamageable found. " +
                          $"(Would deal {currentDamage.amount} damage.)", other);
            return;
        }

        //per-target hit-rate gating
        if (hitHistory.TryGetValue(damageable, out float lastHitTime))
        {
            if (!allowMultipleHitsPerTarget) return;
            if (Time.time - lastHitTime < retriggerInterval) return;
        }

        //fill in hit-point data based on where the target actually is now
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDirection = (other.transform.position - transform.position);
        hitDirection.y = 0f;
        if (hitDirection.sqrMagnitude > 0.0001f) hitDirection.Normalize();

        DamageInfo info = new DamageInfo(
            currentDamage.amount,
            hitPoint,
            hitDirection,
            currentDamage.source
        );

        damageable.TakeDamage(info);
        hitHistory[damageable] = Time.time;

        OnHit?.Invoke(damageable, info);

        if (debugMode)
            Debug.Log($"[Hitbox] Damaged '{other.name}' for {info.amount}.", other);
    }
}
