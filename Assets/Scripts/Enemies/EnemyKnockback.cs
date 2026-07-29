using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    // EDIT (weapon system): knockbackForce is now supplied per-hit via DamageInfo.
    // This field is kept as a fallback for the parameterless overload.
    [SerializeField] private float fallbackKnockbackForce = 15f;
    [SerializeField] private float movementLockDuration = 0.12f;

    // EDIT (weapon system): resistance multiplier. 0 = full knockback, 1 = immune.
    [Header("Resistance")]
    [Tooltip("0 = takes full knockback, 1 = completely immune. Set per prefab variant.")]
    [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private EnemyBehaviourBase[] enemyBehaviours;
    private Coroutine restoreCoroutine;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyBehaviours = GetComponents<EnemyBehaviourBase>();
    }

    // EDIT (weapon system): parameterless overload kept for backward compatibility.
    // Uses fallback force and the enemy's own -forward as direction.
    public void ApplyKnockback()
    {
        Vector3 knockDir = -transform.forward;
        knockDir.y = 0f;
        if (knockDir.sqrMagnitude < 0.0001f) knockDir = Vector3.back;
        knockDir.Normalize();

        ApplyKnockback(knockDir, fallbackKnockbackForce);
    }

    // EDIT (weapon system): new overload accepting direction and force from DamageInfo.
    // Force is scaled by (1 - knockbackResistance).
    public void ApplyKnockback(Vector3 direction, float force)
    {
        float effectiveForce = force * (1f - knockbackResistance);
        if (effectiveForce <= 0f) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.back;
        direction.Normalize();

        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
        }

        SetBehavioursEnabled(false);
        restoreCoroutine = StartCoroutine(KnockbackRoutine(direction, effectiveForce));
    }

    private IEnumerator KnockbackRoutine(Vector3 knockDir, float force)
    {
        float elapsedTime = 0f;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();

            while (elapsedTime < movementLockDuration)
            {
                navAgent.Move(knockDir * (force * Time.deltaTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            navAgent.isStopped = false;
        }
        else if (rb != null && !rb.isKinematic)
        {
            if (navAgent != null) navAgent.enabled = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(knockDir * force, ForceMode.VelocityChange);

            yield return new WaitForSeconds(movementLockDuration);

            rb.linearVelocity = Vector3.zero;
            if (navAgent != null) navAgent.enabled = true;
        }

        SetBehavioursEnabled(true);
        restoreCoroutine = null;
    }

    private void SetBehavioursEnabled(bool isEnabled)
    {
        if (enemyBehaviours == null || enemyBehaviours.Length == 0) return;

        for (int i = 0; i < enemyBehaviours.Length; i++)
        {
            if (enemyBehaviours[i] != null)
                enemyBehaviours[i].enabled = isEnabled;
        }
    }
}
