// Summary:
// Fire-and-forget ranged attack. Instantiates a Projectile prefab from a launch point aimed at the target. 
// The projectile is self-managing after launch. A brief recovery window keeps the enemy planted after firing.

using System.Collections;
using UnityEngine;

public class EnemyAttack_Ranged : EnemyAttack_Base
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Where the projectile spawns. Falls back to this transform if unassigned.")]
    [SerializeField] private Transform launchPoint;

    [Header("Projectile Properties")]
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private float lifetime = 5f;

    [Header("Timing")]
    [Tooltip("Brief pause after firing before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAttacking;
    private Coroutine attackRoutine;

    public override bool IsAttacking => isAttacking;

    public override void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode) Debug.LogWarning($"[RangedAttack] PerformAttack called while already attacking. Ignored.", this);
            return;
        }
        if (projectilePrefab == null)
        {
            Debug.LogError($"[RangedAttack] No projectile prefab assigned on {gameObject.name}.", this);
            return;
        }
        if (target == null)
        {
            Debug.LogError($"[RangedAttack] PerformAttack called with null target on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(target));
    }

    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        isAttacking = false;
        InvokeAttackCancelled();
        StartCooldown(2f);
        if (debugMode) Debug.Log($"[RangedAttack] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;

        FireProjectile(target);
        InvokeStrikeStart();

        if (debugMode) Debug.Log($"[RangedAttack] Fired projectile from {gameObject.name} at {target.name}.", this);

        yield return new WaitForSeconds(recoveryDuration);

        InvokeStrikeEnd();
        isAttacking = false;
        attackRoutine = null;
        StartCooldown();
    }

    private void FireProjectile(Transform target)
    {
        Vector3 spawnPos = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 direction = (target.position - spawnPos).normalized;

        GameObject instance = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction, Vector3.up)
        );

        Projectile proj = instance.GetComponent<Projectile>();
        if (proj == null)
        {
            Debug.LogError($"[RangedAttack] Prefab '{projectilePrefab.name}' has no Projectile component.", this);
            Destroy(instance);
            return;
        }

        proj.Initialize(gameObject, direction, baseDamage, initialVelocity, maxVelocity, acceleration, lifetime);
    }
}
