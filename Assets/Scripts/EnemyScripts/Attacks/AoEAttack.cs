using System.Collections;
using UnityEngine;

/// <summary>
/// Choreographs a telegraphed area attack by spawning two prefabs in sequence:
///   1. DangerZone   - visual telegraph, shown for telegraphDuration
///   2. AoEStrike    - the actual attack with its own visual, hitbox, and damage
///
/// AoEAttack itself has no opinion about visuals, hitbox shape, or damage values -
/// those live on the spawned prefabs. To create a new attack flavor (lightning,
/// spikes, fire, explosion, etc), make a new AoEStrike prefab and assign it.
///
/// The fairness contract: the strike spawns at the snapshotted target position,
/// not at a re-tracked one. The player's window to dodge is the telegraph duration.
/// </summary>
public class AoEAttack : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Visual telegraph spawned at the target position during the windup.")]
    [SerializeField] private DangerZone dangerZonePrefab;

    [Tooltip("The actual attack spawned at the same position after the telegraph completes. " +
             "Swap this prefab to change the attack flavor (lightning, spikes, fire, etc).")]
    [SerializeField] private AoEStrike strikePrefab;

    [Header("Timing")]
    [Tooltip("How long the danger zone is shown before the strike spawns. Main 'feel' knob.")]
    [SerializeField] private float telegraphDuration = 1.2f;

    [Tooltip("Brief pause after the strike spawns before the attack is considered finished. " +
             "The strike itself manages its own lifetime independently.")]
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Telegraph")]
    [Tooltip("Visual radius passed to the DangerZone for sizing. Should roughly match the " +
             "strike prefab's hitbox so the telegraph reads honestly.")]
    [SerializeField] private float telegraphRadius = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isAttacking;
    private DangerZone activeZone;
    private Coroutine attackRoutine;

    public bool IsAttacking => isAttacking;

    /// <summary>
    /// Begin the telegraph -> strike sequence at the given world position.
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

        if (strikePrefab == null)
        {
            Debug.LogError($"[AoEAttack] No AoEStrike prefab assigned on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(targetPosition));
    }

    /// <summary>
    /// Cancel an in-progress attack before the strike spawns. The danger zone is removed.
    /// Strikes that have already spawned manage their own lifetime and are not cancelled.
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

        //telegraph
        if (dangerZonePrefab != null)
        {
            activeZone = Instantiate(dangerZonePrefab, targetPosition, Quaternion.identity);
            activeZone.Show(targetPosition, telegraphRadius, telegraphDuration);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[AoEAttack] No DangerZone prefab assigned - attack will have no telegraph.", this);
        }

        if (debugMode)
            Debug.Log($"[AoEAttack] Telegraph started at {targetPosition} " +
                      $"(duration {telegraphDuration}s).", this);

        yield return new WaitForSeconds(telegraphDuration);

        //strike: spawn the AoEStrike prefab; from here it's self-managing
        AoEStrike strike = Instantiate(strikePrefab, targetPosition, Quaternion.identity);
        strike.SetSource(gameObject);
        activeZone = null;

        if (debugMode)
            Debug.Log($"[AoEAttack] Strike spawned at {targetPosition}.", this);

        //recovery - lets the behaviour's cooldown start at a sensible moment.
        //the strike's own lifetime continues independently in the scene.
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;

        if (debugMode)
            Debug.Log($"[AoEAttack] Attack complete on {gameObject.name}.", this);
    }
}