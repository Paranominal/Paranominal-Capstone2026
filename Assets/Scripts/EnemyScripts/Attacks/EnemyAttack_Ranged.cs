// Summary:
// Ranged attack with a telegraphed windup. Tracks the target during windup, fires a Projectile
// prefab from a launch point, then recovers. The projectile is self-managing after launch.

using System.Collections;
using UnityEngine;

public class EnemyAttack_Ranged : EnemyAttack_Base
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Projectile Properties")]
    [SerializeField] private int damage = 8;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private float lifetime = 5f;

    [Header("Targeting")]
    [Tooltip("Local-space offset from the enemy's position where the projectile spawns.")]
    [SerializeField] private Vector3 launchOffset = new Vector3(0f, 1f, 0.5f);
    [Tooltip("Height offset on the target to aim at (e.g. 1.0 for chest height).")]
    [SerializeField] private float targetHeightOffset = 1f;

    [Header("Timing")]
    [Tooltip("How long the enemy telegraphs before firing. 0 = fires immediately.")]
    [SerializeField] private float windupDuration = 0.4f;
    [Tooltip("Brief pause after firing before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.3f;

    [Header("Tracking")]
    [Tooltip("Turn speed while tracking the target during windup. 0 = lock facing at commit.")]
    [SerializeField] private float windupTurnSpeed = 180f;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAttacking;
    private bool isWindingUp;
    private Coroutine attackRoutine;

    public override bool IsAttacking => isAttacking;
    public override bool IsWindingUp => isWindingUp;
    public override float WindupDuration => windupDuration;

    public override void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode) Debug.LogWarning($"[EnemyAttack_Ranged] PerformAttack called while already attacking. Ignored.", this);
            return;
        }
        if (projectilePrefab == null)
        {
            Debug.LogError($"[EnemyAttack_Ranged] No projectile prefab assigned on {gameObject.name}.", this);
            return;
        }
        if (target == null)
        {
            Debug.LogError($"[EnemyAttack_Ranged] PerformAttack called with null target on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(target));
    }

    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        isAttacking = false;
        isWindingUp = false;
        InvokeAttackCancelled();
        StartCooldown(2f);
        if (debugMode) Debug.Log($"[EnemyAttack_Ranged] Attack cancelled on {gameObject.name}.", this);
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;

        // windup: track the target while telegraphing
        if (windupDuration > 0f)
        {
            isWindingUp = true;
            InvokeWindupStart();
            if (debugMode) Debug.Log($"[EnemyAttack_Ranged] Windup started on {gameObject.name}.", this);

            float elapsed = 0f;
            while (elapsed < windupDuration)
            {
                if (target != null && windupTurnSpeed > 0f)
                {
                    Vector3 lookDir = target.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, windupTurnSpeed * Time.deltaTime);
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            isWindingUp = false;
        }

        // fire
        FireProjectile(target);
        InvokeStrikeStart();
        if (debugMode) Debug.Log($"[EnemyAttack_Ranged] Fired projectile from {gameObject.name}.", this);

        // recovery
        yield return new WaitForSeconds(recoveryDuration);

        InvokeStrikeEnd();
        isAttacking = false;
        attackRoutine = null;
        StartCooldown();
        if (debugMode) Debug.Log($"[EnemyAttack_Ranged] Attack complete on {gameObject.name}.", this);
    }

    private void FireProjectile(Transform target)
    {
        Vector3 spawnPos = transform.position + transform.TransformDirection(launchOffset);
        Vector3 aimPoint = target != null ? target.position + Vector3.up * targetHeightOffset : transform.position + transform.forward;
        Vector3 direction = (aimPoint - spawnPos).normalized;

        GameObject instance = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction, Vector3.up));

        Projectile proj = instance.GetComponent<Projectile>();
        if (proj == null)
        {
            Debug.LogError($"[EnemyAttack_Ranged] Prefab '{projectilePrefab.name}' has no Projectile component.", this);
            Destroy(instance);
            return;
        }

        proj.Initialize(gameObject, direction, damage, initialVelocity, maxVelocity, acceleration, lifetime);
    }
}