using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float movementLockDuration = 0.12f;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private EnemyBehaviourBase[] enemyBehaviours;
    private Coroutine restoreCoroutine;

    private void Awake()
    {
        //cache references before play starts
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyBehaviours = GetComponents<EnemyBehaviourBase>();
    }

    public void ApplyKnockback()
    {
        //calculate knockback direction opposite to enemy's facing direction
        Vector3 knockDir = -transform.forward;
        knockDir.y = 0f;

        //fallback to backward direction if forward is invalid
        if (knockDir.sqrMagnitude < 0.0001f)
        {
            knockDir = Vector3.back;
        }

        knockDir.Normalize();

        //stop any pending movement restoration coroutine
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
        }

        //disable all AI behaviors during knockback
        SetBehavioursEnabled(false);

        //interrupt any active navigation
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        bool appliedPhysicsKnockback = false;

        //apply knockback via physics if rigidbody is available and not kinematic
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(knockDir * knockbackForce, ForceMode.VelocityChange);
            appliedPhysicsKnockback = true;
        }

        //fallback to position-based knockback if physics is unavailable
        if (!appliedPhysicsKnockback)
        {
            ApplyFallbackKnockback(knockDir);
        }

        //schedule behavior and movement restoration after lock duration
        restoreCoroutine = StartCoroutine(RestoreMovementAfterDelay());
    }

    private void ApplyFallbackKnockback(Vector3 knockDir)
    {
        //calculate target position offset from current location
        Vector3 targetPosition = transform.position + knockDir * (knockbackForce * 0.2f);

        //use navmesh warping for better pathfinding integration
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            //try to find a valid navmesh position near the target
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, Mathf.Max(0.5f, knockbackForce), navAgent.areaMask))
            {
                navAgent.Warp(hit.position);
                return;
            }

            //warp directly to target if sampling fails
            navAgent.Warp(targetPosition);
            return;
        }

        //direct position change if navmesh is unavailable
        transform.position = targetPosition;
    }

    private IEnumerator RestoreMovementAfterDelay()
    {
        //wait for knockback lock duration to expire
        yield return new WaitForSeconds(movementLockDuration);

        //resume navmesh agent pathfinding
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        //re-enable all AI behavior components
        SetBehavioursEnabled(true);
        restoreCoroutine = null;
    }

    private void SetBehavioursEnabled(bool isEnabled)
    {
        //guard against missing or empty behavior array
        if (enemyBehaviours == null || enemyBehaviours.Length == 0)
            return;

        //iterate through all behaviors and apply enabled state
        for (int i = 0; i < enemyBehaviours.Length; i++)
        {
            if (enemyBehaviours[i] != null)
                enemyBehaviours[i].enabled = isEnabled;
        }
    }
}