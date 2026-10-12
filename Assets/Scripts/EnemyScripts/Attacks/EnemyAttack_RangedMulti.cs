// Summary:
// Ranged attack that fires every Projectile prefab in its list as a burst, either in list order or shuffled each attack.
// Each prefab uses the stats set on its own Projectile component. Windup before the burst, a set delay between shots,
// recovery after the last shot. The enemy keeps tracking the target throughout, and each shot is aimed when it fires.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack_RangedMulti : EnemyAttack_Base
{
    public enum SelectionMode { Random, Sequence }

    [Header("Projectiles")]
    [Tooltip("Projectile prefabs fired each attack. Each uses the stats set on its own Projectile component.")]
    [SerializeField] private List<Projectile> projectilePrefabs = new List<Projectile>();
    [Tooltip("Sequence: fires the list top to bottom. Random: fires every entry once, in a shuffled order each attack.")]
    [SerializeField] private SelectionMode selectionMode = SelectionMode.Sequence;

    [Header("Targeting")]
    [Tooltip("Where the projectiles spawn. Falls back to the enemy's position if unassigned.")]
    [SerializeField] private Transform launchPoint;
    [Tooltip("Height offset on the target to aim at (e.g. 1.0 for chest height).")]
    [SerializeField] private float targetHeightOffset = 1f;

    [Header("Timing")]
    [Tooltip("How long the enemy telegraphs before the burst. 0 = fires immediately.")]
    [SerializeField] private float windupDuration = 0.4f;
    [Tooltip("Delay between each projectile in the burst.")]
    [SerializeField] private float timeBetweenShots = 0.2f;
    [Tooltip("Brief pause after the last shot before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.3f;

    [Header("Tracking")]
    [Tooltip("Turn speed while tracking the target during windup and the burst. 0 = lock facing at commit.")]
    [SerializeField] private float trackingTurnSpeed = 180f;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAttacking;
    private bool isWindingUp;
    private Coroutine attackRoutine;
    private readonly List<Projectile> burstOrder = new List<Projectile>();

    public override bool IsAttacking => isAttacking;
    public override bool IsWindingUp => isWindingUp;
    public override float WindupDuration => windupDuration;

    public override void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode) Debug.LogWarning($"[EnemyAttack_RangedMulti] PerformAttack called while already attacking. Ignored.", this);
            return;
        }
        if (projectilePrefabs == null || projectilePrefabs.Count == 0)
        {
            Debug.LogError($"[EnemyAttack_RangedMulti] No projectile prefabs assigned on {gameObject.name}.", this);
            return;
        }
        if (target == null)
        {
            Debug.LogError($"[EnemyAttack_RangedMulti] PerformAttack called with null target on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(target));
    }

    // stops the whole attack, including any shots left in the burst
    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        isAttacking = false;
        isWindingUp = false;
        InvokeAttackCancelled();
        StartCooldown(2f);
        if (debugMode) Debug.Log($"[EnemyAttack_RangedMulti] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;

        // windup: track the target while telegraphing
        if (windupDuration > 0f)
        {
            isWindingUp = true;
            InvokeWindupStart();
            if (debugMode) Debug.Log($"[EnemyAttack_RangedMulti] Windup started on {gameObject.name}.", this);

            yield return TrackFor(target, windupDuration);

            isWindingUp = false;
        }

        // burst: fire every entry, tracking between shots
        BuildBurstOrder();
        for (int i = 0; i < burstOrder.Count; i++)
        {
            FireProjectile(burstOrder[i], target);

            // strike start on the first shot only, hides the windup indicator
            if (i == 0) InvokeStrikeStart();

            if (i < burstOrder.Count - 1 && timeBetweenShots > 0f)
                yield return TrackFor(target, timeBetweenShots);
        }

        // recovery
        yield return new WaitForSeconds(recoveryDuration);

        InvokeStrikeEnd();
        isAttacking = false;
        attackRoutine = null;
        StartCooldown();
        if (debugMode) Debug.Log($"[EnemyAttack_RangedMulti] Attack complete on {gameObject.name}.", this);
    }

    // turns towards the target for the given duration
    private IEnumerator TrackFor(Transform target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target != null && trackingTurnSpeed > 0f)
            {
                Vector3 lookDir = target.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, trackingTurnSpeed * Time.deltaTime);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // copies the list, shuffled in Random mode (Fisher-Yates)
    private void BuildBurstOrder()
    {
        burstOrder.Clear();
        burstOrder.AddRange(projectilePrefabs);

        if (selectionMode != SelectionMode.Random) return;

        for (int i = burstOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (burstOrder[i], burstOrder[j]) = (burstOrder[j], burstOrder[i]);
        }
    }

    private void FireProjectile(Projectile prefab, Transform target)
    {
        if (prefab == null)
        {
            Debug.LogError($"[EnemyAttack_RangedMulti] Empty entry in projectile list on {gameObject.name}. Shot skipped.", this);
            return;
        }

        Vector3 spawnPos = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 aimPoint = target != null ? target.position + Vector3.up * targetHeightOffset : transform.position + transform.forward;
        Vector3 direction = (aimPoint - spawnPos).normalized;

        Projectile instance = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction, Vector3.up));
        instance.Initialize(gameObject, direction);

        if (debugMode) Debug.Log($"[EnemyAttack_RangedMulti] Fired '{prefab.name}' from {gameObject.name}.", this);
    }
}