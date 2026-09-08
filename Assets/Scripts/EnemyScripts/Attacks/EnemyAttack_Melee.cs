// Summary:
// Telegraphed melee attack. Handles windup (tracking), strike (DamageField), and recovery.
// The player's dodge window is the windup phase: sidestepping while the enemy tracks at a (typically slow) turn speed.

using System.Collections;
using UnityEngine;

public class EnemyAttack_Melee : EnemyAttack_Base
{
    [Header("Damage Field")]
    [Tooltip("DamageField prefab instantiated in front of the enemy during the strike.")]
    [SerializeField] private DamageField damageFieldPrefab;
    [SerializeField] private float damageFieldHeight = 0.5f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Timing")]
    [Tooltip("How long the owner winds up before striking. " +
             "Longer = more readable, easier to dodge.")]
    [SerializeField] private float windupDuration = 0.6f;

    [Tooltip("How long the damage field persists during the strike.")]
    [SerializeField] private float strikeDuration = 0.15f;

    [Tooltip("Brief pause after the strike before the attack is considered finished.")]
    [SerializeField] private float recoveryDuration = 0.4f;

    [Header("Tracking")]
    [Tooltip("Turn speed while tracking the target during windup. " +
             "Lower = more dodgeable. 0 = lock facing at commit.")]
    [SerializeField] private float windupTurnSpeed = 90f;

    [Header("Damage")]
    [SerializeField] private int damage = 10;

    [Header("Attack Indicator")]
    [Tooltip("Optional sprite shown during windup and strike. Hidden on spawn.")]
    [SerializeField] private SpriteRenderer attackIndicator;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private bool isAttacking;
    private bool isWindingUp;
    private Coroutine attackRoutine;
    private DamageField activeDamageField;

    public override bool IsAttacking => isAttacking;
    public override bool IsWindingUp => isWindingUp;
    public override float WindupDuration => windupDuration;
    public float StrikeDuration => strikeDuration;

    protected override void Awake()
    {
        base.Awake();
        SetAttackIndicator(false);
    }

    public override void PerformAttack(Transform target)
    {
        if (isAttacking)
        {
            if (debugMode) Debug.LogWarning($"[EnemyAttack_Melee] PerformAttack called while already attacking. Ignored.", this);
            return;
        }
        if (damageFieldPrefab == null)
        {
            Debug.LogError($"[EnemyAttack_Melee] No DamageField prefab assigned on {gameObject.name}.", this);
            return;
        }

        attackRoutine = StartCoroutine(AttackSequence(target));
    }

    public override void CancelAttack()
    {
        if (!isAttacking) return;
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        CleanupDamageField();
        isAttacking = false;
        isWindingUp = false;
        SetAttackIndicator(false);
        InvokeAttackCancelled();
        StartCooldown(2f);
        if (debugMode) Debug.Log($"[EnemyAttack_Melee] Attack cancelled on {gameObject.name}.", this);
    }

    private void SetAttackIndicator(bool show)
    {
        if (attackIndicator != null) attackIndicator.enabled = show;
    }

    private void CleanupDamageField()
    {
        if (activeDamageField != null)
        {
            Destroy(activeDamageField.gameObject);
            activeDamageField = null;
        }
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;
        isWindingUp = true;
        SetAttackIndicator(true);
        InvokeWindupStart();
        if (debugMode) Debug.Log($"[EnemyAttack_Melee] Windup started on {gameObject.name}.", this);

        // windup: track the target while the player reads the tell
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

        // strike: instantiate damage field in front of the enemy
        if (debugMode) Debug.Log($"[EnemyAttack_Melee] Strike on {gameObject.name}.", this);
        Vector3 spawnPos = transform.position + (transform.forward * AttackRange / 2f) + (Vector3.up * damageFieldHeight * 0.5001f);
        activeDamageField = Instantiate(damageFieldPrefab, spawnPos, transform.rotation).GetComponent<DamageField>();
        activeDamageField.DoDamageField(damage, strikeDuration, AttackRange / 2f, damageFieldHeight, targetLayers, this);
        InvokeStrikeStart();

        // wait for the damage field to finish (hit or timed out)
        yield return new WaitUntil(() => activeDamageField == null || activeDamageField.attackComplete);

        CleanupDamageField();
        SetAttackIndicator(false);
        InvokeStrikeEnd();

        // recovery
        yield return new WaitForSeconds(recoveryDuration);

        isAttacking = false;
        attackRoutine = null;
        StartCooldown();
        if (debugMode) Debug.Log($"[EnemyAttack_Melee] Attack complete on {gameObject.name}.", this);
    }
}