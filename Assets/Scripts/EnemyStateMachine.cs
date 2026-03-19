using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    private bool isOnAttackCooldown;
    [SerializeField] private float meleeDamage = 5f;
    [SerializeField] private float meleeRange = 10f;

    [Header("Detection Ranges")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 10f;


    private bool isPlayerVisible;
    private bool isPlayerInRange;

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
    }

    void Update()
    {
        DetectPlayer();
        UpdateBehaviourState();
    }

    // Allow vision and engagement ranges to be seen in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    private void DetectPlayer()
    {
        isPlayerVisible = Physics.CheckSphere(transform.position, visionRange, playerLayerMask);
        isPlayerInRange = Physics.CheckSphere(transform.position, engagementRange, playerLayerMask);
    }

    private void MeleeAttack()
    {
        // TO-DO: Implement basic enemy melee attack
    }

    private void FindPatrolPoint()
    {
        float randomX = Random.Range(-patrolRadius, patrolRadius);
        float randomZ = Random.Range(-patrolRadius, patrolRadius);


        Vector3 potentialPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);


        if (Physics.Raycast(potentialPoint, -transform.up, 2f, terrainLayer))
        {
            currentPatrolPoint = potentialPoint;
            hasPatrolPoint = true;
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    private void PerformPatrol()
    {
        if (!hasPatrolPoint)
            FindPatrolPoint();


        if (hasPatrolPoint)
            navAgent.SetDestination(currentPatrolPoint);


        if (Vector3.Distance(transform.position, currentPatrolPoint) < 1f)
            hasPatrolPoint = false;
    }

    private void PerformChase()
    {
        if (playerTransform != null)
        {
            navAgent.SetDestination(playerTransform.position);
        }
    }

    private void PerformAttack()
    {
        navAgent.SetDestination(transform.position);


        if (playerTransform != null)
        {
            transform.LookAt(playerTransform);
        }


        if (!isOnAttackCooldown)
        {
            MeleeAttack();
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private void UpdateBehaviourState()
    {
        if (!isPlayerVisible && !isPlayerInRange)
        {
            PerformPatrol();
        }
        else if (isPlayerVisible && !isPlayerInRange)
        {
            PerformChase();
        }
        else if (isPlayerVisible && isPlayerInRange)
        {
            PerformAttack();
        }
    }
}
