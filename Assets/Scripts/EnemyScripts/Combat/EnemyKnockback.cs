using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 15f; 
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

        //disable all behaviors during knockback
        SetBehavioursEnabled(false);

        //start the controlled knockback slide
        restoreCoroutine = StartCoroutine(KnockbackRoutine(knockDir));
    }

    private IEnumerator KnockbackRoutine(Vector3 knockDir)
    {
        float elapsedTime = 0f;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            //interrupt any active navigation
            navAgent.isStopped = true;
            navAgent.ResetPath();

            //smoothly push the enemy without leaving the navmesh
            while (elapsedTime < movementLockDuration)
            {
                navAgent.Move(knockDir * (knockbackForce * Time.deltaTime));
                elapsedTime += Time.deltaTime;
                //wait for the next frame
                yield return null; 
            }

            navAgent.isStopped = false;
        }
        else if (rb != null && !rb.isKinematic)
        {
            //disable the navmeshagent entirely so physics work properly (yes, this is a fallback)
            if (navAgent != null) navAgent.enabled = false;
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(knockDir * knockbackForce, ForceMode.VelocityChange);

            //wait for knockback lock duration to expire
            yield return new WaitForSeconds(movementLockDuration);
            
            rb.linearVelocity = Vector3.zero;
            if (navAgent != null) navAgent.enabled = true;
        }

        //re-enable all behavior components
        SetBehavioursEnabled(true);
        restoreCoroutine = null;
    }

    private void SetBehavioursEnabled(bool isEnabled)
    {
        //guard against missing or empty behavior array
        if (enemyBehaviours == null || enemyBehaviours.Length == 0) return;

        //iterate through all behaviors and apply enabled state
        for (int i = 0; i < enemyBehaviours.Length; i++)
        {
            if (enemyBehaviours[i] != null)
                enemyBehaviours[i].enabled = isEnabled;
        }
    }
}