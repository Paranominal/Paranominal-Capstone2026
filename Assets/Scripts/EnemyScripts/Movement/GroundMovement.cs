// Summary:
// NavMeshAgent-based ground movement. Implements IEnemyMovement for use with Enemy.
// Handles chase, strafe (with NavMesh obstacle checking), retreat, return to origin, and chase territory management.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GroundMovement : MonoBehaviour, IEnemyMovement
{
    [Header("Chase")]
    [SerializeField] private bool chaseEnabled = true;
    [ShowIf("chaseEnabled")]
    [Tooltip("Default orbit/strafe radius. For multi-attack enemies, used as fallback when all attacks are on cooldown. Single-attack enemies use their attack range instead.")]
    [SerializeField] private float strafeRadius = 5f;
    [ShowIf("chaseEnabled")]
    [SerializeField] private float chaseSpeed = 5f;
    [ShowIf("chaseEnabled")]
    [Range(0, 1)]
    [Tooltip("Controls how quickly the agent accelerates. 0 = sluggish, 1 = snappy.")]
    [SerializeField] private float chaseEasing = 0.5f;
    [ShowIf("chaseEnabled")]
    [SerializeField] private bool onlyChaseIfAttackReady;

    [ShowIf("chaseEnabled", Header = "Chase Territory")]
    [SerializeField] private bool neverGiveUpChase;
    [ShowIf("neverGiveUpChase", false)]
    [Tooltip("Max distance the enemy will chase from its spawn point.")]
    [SerializeField] private float chaseRange = 20f;

    [Header("Return")]
    [SerializeField] private bool returnToOrigin = true;
    [ShowIf("returnToOrigin")]
    [SerializeField] private float returnSpeed = 3f;

    [Header("Retreat")]
    [SerializeField] private bool retreatEnabled;
    [ShowIf("retreatEnabled")]
    [SerializeField] private float retreatDistance = 5f;
    [ShowIf("retreatEnabled")]
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Strafe")]
    [SerializeField] private bool strafeEnabled;
    [ShowIf("strafeEnabled")]
    [SerializeField] private float strafeSpeed = 3f;
    [ShowIf("strafeEnabled")]
    [Tooltip("How often the enemy changes strafe direction in seconds.")]
    [SerializeField] private float strafeDirectionInterval = 2f;

    private NavMeshAgent navAgent;
    private Vector3 spawnPosition;

    // strafe
    private float strafeDirection = 1f;
    private float strafeTimer;

    public float StrafeRadius => strafeRadius;
    public bool ReturnEnabled => returnToOrigin;
    public bool RetreatEnabled => retreatEnabled;
    public bool StrafeEnabled => strafeEnabled;

    public bool HasReachedTarget
    {
        get
        {
            if (navAgent == null || !navAgent.isOnNavMesh) return true;
            if (navAgent.pathPending) return false;
            return !navAgent.hasPath || navAgent.remainingDistance <= navAgent.stoppingDistance + 0.1f;
        }
    }

    public void Initialize()
    {
        spawnPosition = transform.position;
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.acceleration = 51f - chaseEasing * 50f;
            navAgent.updateRotation = false;
        }
    }


    // Movement Commands
    public void Chase(Vector3 target, float stopDistance)
    {
        Move(target, chaseSpeed, stopDistance);
    }

    public void Strafe(Vector3 orbitCenter, float orbitRadius)
    {
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection *= -1f;
            strafeTimer = strafeDirectionInterval;
        }

        Vector3 target = ComputeStrafeTarget(orbitCenter, orbitRadius, strafeDirection);

        if (!IsStrafeClear(target))
        {
            target = ComputeStrafeTarget(orbitCenter, orbitRadius, -strafeDirection);
            if (!IsStrafeClear(target))
            {
                Stop();
                return;
            }
        }

        Move(target, strafeSpeed, 0.5f);
    }

    public void BeginRetreat(Vector3 awayFrom)
    {
        Vector3 dir = (transform.position - awayFrom);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
        Vector3 retreatTarget = transform.position + dir.normalized * retreatDistance;
        Move(retreatTarget, retreatSpeed, 0.5f);
    }

    public void BeginReturn()
    {
        Move(spawnPosition, returnSpeed, 0.5f);
    }

    public void Stop()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    public void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetPaused(bool paused)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        if (paused) Stop();
        else navAgent.isStopped = false;
    }

    public bool ShouldExitChase(bool playerInAggroRange)
    {
        if (neverGiveUpChase) return false;
        float distFromSpawn = (transform.position - spawnPosition).magnitude;
        return !playerInAggroRange || distFromSpawn > chaseRange;
    }

    public bool CanChase(bool anyAttackReady)
    {
        if (!chaseEnabled) return false;
        if (onlyChaseIfAttackReady && !anyAttackReady) return false;
        return true;
    }


    // Internals
    private void Move(Vector3 target, float speed, float stopDistance)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        if (navAgent.isStopped) navAgent.isStopped = false;
        navAgent.stoppingDistance = stopDistance;
        navAgent.speed = speed;
        navAgent.SetDestination(target);
    }

    private Vector3 ComputeStrafeTarget(Vector3 center, float radius, float direction)
    {
        Vector3 toEnemy = transform.position - center;
        toEnemy.y = 0f;
        if (radius < 0.1f) return transform.position;

        Vector3 lateral = Vector3.Cross(Vector3.up, toEnemy.normalized) * direction;
        Vector3 aheadOnArc = transform.position + lateral * 2f;
        Vector3 fromCenter = aheadOnArc - center;
        fromCenter.y = 0f;

        return center + fromCenter.normalized * radius;
    }

    private bool IsStrafeClear(Vector3 target)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return true;
        return !NavMesh.Raycast(transform.position, target, out NavMeshHit _, navAgent.areaMask);
    }
}
