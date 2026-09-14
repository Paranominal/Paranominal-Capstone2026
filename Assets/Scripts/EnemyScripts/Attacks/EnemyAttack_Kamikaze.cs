// Summary:
// Kamikaze attack. When triggered, the enemy rushes directly at the target and deals damage on contact, then dies. 
// Bypasses the movement script entirely (movement is paused by the behaviour during attacks) and controls NavMeshAgent or Rigidbody directly for the rush.

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAttack_Kamikaze : EnemyAttack_Base
{
    [Header("Rush")]
    [SerializeField] private float rushSpeed = 12f;

    [Header("Contact")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float contactRadius = 1f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    [Tooltip("Height offset on the target to aim at (e.g. 1.0 for chest height).")]
    [SerializeField] private float targetHeightOffset = 1f;

    private bool isAttacking;
    private Coroutine attackRoutine;

    // cached
    private NavMeshAgent navAgent;
    private Rigidbody rb;

    public override bool IsAttacking => isAttacking;

    protected override void Awake()
    {
        base.Awake();
        navAgent = GetComponentInParent<NavMeshAgent>();
        rb = GetComponentInParent<Rigidbody>();
    }

    public override void PerformAttack(Transform target)
    {
        if (isAttacking) return;
        if (target == null)
        {
            Debug.LogError($"[EnemyAttack_Kamikaze] PerformAttack called with null target on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(RushSequence(target));
    }

    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        RushStop();
        isAttacking = false;
        InvokeAttackCancelled();
        if (debugMode) Debug.Log($"[EnemyAttack_Kamikaze] Rush cancelled on {gameObject.name}.", this);
    }

    private void RushStop()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
            navAgent.isStopped = false;
        if (rb != null && navAgent == null)
            rb.linearVelocity = Vector3.zero;
    }

    private IEnumerator RushSequence(Transform target)
    {
        isAttacking = true;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        InvokeStrikeStart();
        if (debugMode) Debug.Log($"[EnemyAttack_Kamikaze] Rushing at {target.name}.", this);

        while (target != null)
        {
            Vector3 aimPoint = target.position + Vector3.up * targetHeightOffset;
            Vector3 dir = (aimPoint - transform.position).normalized;

            // rush toward target
            if (navAgent != null && navAgent.isOnNavMesh)
                navAgent.Move(dir * rushSpeed * Time.deltaTime);
            else if (rb != null)
                rb.linearVelocity = dir * rushSpeed;
            else
                transform.position += dir * rushSpeed * Time.deltaTime;

            // contact check
            Collider[] hits = Physics.OverlapSphere(transform.position, contactRadius, targetLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo(damage, transform.position, dir, gameObject);
                    damageable.TakeDamage(info);

                    if (debugMode) Debug.Log($"[EnemyAttack_Kamikaze] Contact! Dealing {damage} damage.", this);

                    Enemy enemy = GetComponentInParent<Enemy>();
                    if (enemy != null) enemy.Die();
                    yield break;
                }
            }

            yield return null;
        }

        // target lost
        isAttacking = false;
        attackRoutine = null;
        RushStop();
    }
}
