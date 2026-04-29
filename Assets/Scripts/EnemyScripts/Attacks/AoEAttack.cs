using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable telegraphed area-of-effect attack. Handles the sequence:
///   1. Telegraph - spawn DangerZone visual at the target position
///   2. Strike    - activate the hitbox briefly via OverlapSphere
///   3. Recovery  - brief pause before control returns to the behaviour
///
/// The owning behaviour is responsible for snapshotting the target position
/// and calling PerformAttack(snapshotPos). Once committed, the zone does
/// not move - this is what gives the player a fair window to dodge.
/// </summary>
public class AoEAttack : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Prefab containing a DangerZone component and the telegraph visual/shader.")]
    [SerializeField] private DangerZone dangerZonePrefab;

    [Header("Timing")]
    [Tooltip("How long the danger-zone is visible before the strike resolves. " +
             "Main 'feel' knob - longer = easier to dodge.")]
    [SerializeField] private float telegraphDuration = 1.2f;

    [Tooltip("How long the hitbox is active during the strike. Should be short.")]
    [SerializeField] private float strikeDuration = 0.15f;

    [Tooltip("Brief pause after the strike before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Hit Detection")]
    [Tooltip("Radius of the AoE in world units.")]
    [SerializeField] private float aoeRadius = 2f;

    [Tooltip("Layers that can be hit by the strike (typically just the player layer).")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Tooltip("Vertical offset added to the snapshot position when checking hits. " +
             "Useful if the snapshot is taken at the player's feet but their collider is centered higher.")]
    [SerializeField] private float hitCheckYOffset = 0.5f;

    [Header("Damage")]
    [Tooltip("Damage value passed to the (future) damage system. Currently logged only.")]
    [SerializeField] private int damage = 10;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isAttacking;
    private DangerZone activeZone;
    private Coroutine attackRoutine;

    public bool IsAttacking => isAttacking;

    /// <summary>
    /// Begin the telegraph -> strike -> recovery sequence at the given world position.
    /// The position should be snapshotted by the caller before this is invoked.
    /// </summary>
    public void PerformAttack(Vector3 targetPosition)
    {
        if (isAttacking)
        {
            if (debugMode)
                Debug.LogWarning($"[AoEAttack] PerformAttack called while already attacking. Ignored.", this);
            return;
        }

        if (dangerZonePrefab == null)
        {
            Debug.LogError($"[AoEAttack] No DangerZone prefab assigned on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(targetPosition));
    }

    /// <summary>
    /// Cancel an in-progress attack. The danger-zone is removed and no strike is performed.
    /// Useful for stagger interruptions.
    /// </summary>
    public void CancelAttack()
    {
        if (!isAttacking) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (activeZone != null)
        {
            activeZone.Cancel();
            activeZone = null;
        }

        isAttacking = false;

        if (debugMode)
            Debug.Log($"[AoEAttack] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Vector3 targetPosition)
    {
        isAttacking = true;

        //commit: spawn the danger zone at the snapshotted position
        activeZone = Instantiate(dangerZonePrefab, targetPosition, Quaternion.identity);
        activeZone.Show(targetPosition, aoeRadius, telegraphDuration);

        if (debugMode)
            Debug.Log($"[AoEAttack] Telegraph started at {targetPosition} (radius {aoeRadius}, " +
                      $"duration {telegraphDuration}s)", this);

        //telegraph window - player has this long to escape
        yield return new WaitForSeconds(telegraphDuration);

        //strike: resolve hits at the snapshotted position
        ResolveStrike(targetPosition);

        //zone destroys itself when its lifetime ends, but null our reference now
        activeZone = null;

        //brief active hitbox window (currently a single check, but extending here
        //would let the strike linger - e.g. for a lava pool or lingering damage)
        yield return new WaitForSeconds(strikeDuration);

        //recovery
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;

        if (debugMode)
            Debug.Log($"[AoEAttack] Attack complete on {gameObject.name}.", this);
    }

    private void ResolveStrike(Vector3 targetPosition)
    {
        Vector3 checkCenter = targetPosition + Vector3.up * hitCheckYOffset;
        Collider[] hits = Physics.OverlapSphere(checkCenter, aoeRadius, hitLayers, QueryTriggerInteraction.Collide);

        if (hits.Length == 0)
        {
            if (debugMode)
                Debug.Log($"[AoEAttack] Strike resolved at {targetPosition} - no hits.", this);
            return;
        }

        foreach (Collider hit in hits)
        {
            //TODO: replace with real damage call once the damage system is implemented.
            //e.g. if (hit.TryGetComponent<IDamageable>(out var d)) d.TakeDamage(damage);
            Debug.Log($"[AoEAttack] Hit '{hit.name}' for {damage} damage.", hit);
        }
    }

    //visualize the AoE in the editor when the component is selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * hitCheckYOffset, aoeRadius);
    }
}
