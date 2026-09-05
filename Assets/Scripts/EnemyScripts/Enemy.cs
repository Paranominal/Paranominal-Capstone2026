using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System;
using Unity.VisualScripting;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    public enum BehaviourState { Idling, Chasing, Attacking, Waiting, Stunned, Spawning, Dying, Inactive };
    public enum EnemyClass { Standard, Champion, Thrall };

    [Header("Enemy Options")]
    [SerializeField] private EnemyClass enemyClass = EnemyClass.Standard;
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private float aggroRange = 10;
    [SerializeField] private bool skipSpawn;
    [Tooltip("Time in seconds it takes the enemy to spawn")]
    [SerializeField] private float spawnDelay = 3;
    // [Header("Weak Points")]
    // [SerializeField] private WeakPointManager weakPointManager;
    // [Header("Class")]
    // public EnemyClass enemyClass = EnemyClass.Standard;
    [Header("Attack")]
    [SerializeField] private EnemyAttack_Base attack;
    [Header("Chase")]
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool onlyChaseIfAttackReady;
    [SerializeField] private bool neverGiveUpChase;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private float chaseSpeed = 5f;
    [Range(0, 1)]
    [SerializeField] private float chaseEasing = 0.5f;
    [Tooltip("Overrides NavMeshAgent stopping distance, but will also be overriden by the range of added Attack Scripts.")]
    [SerializeField] private float chaseStopDistance = 2.5f;
    [Header("Stagger")]
    [SerializeField] private EnemyStagger stagger;
    [SerializeField] private int numberOfPhases = 3;
    [SerializeField] private int phaseDelay = 1;
    // [SerializeField] private EnemyKnockback knockback;
    // [SerializeField] private float knockbackStrength = 5;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Header("Summons ( CHAMPION ONLY )")]
    [SerializeField] private bool doSummons = true; //toggle or not
    public bool DoSummons
    {
        get => doSummons;
        set => doSummons = value;
    }
    [SerializeField] private int[] summonOnCycles = new int[] { 1 }; //this is an array, 0 would mean after the first cycle
    [SerializeField] private Enemy summonsPrefab;
    [SerializeField] private int numberOfSummons = 3;
    [SerializeField] private float summonRadius = 3f;
    [Header("Debug")]
    public bool debugMode;

    private Transform playerTransform;
    private BehaviourState behaviourState = BehaviourState.Inactive;
    // private int WeakpointCycle => !stagger.weakPointManager ? stagger.weakPointManager.CyclesComplete : 0;

    private void Reset()
    {
        if (!GetComponent<EnemyStagger>())
        {
            Debug.LogWarning($"[{this}] no Stagger component found! Adding one now.");
            gameObject.AddComponent(typeof(EnemyStagger));
        }   
    }

    private void Awake()
    {
        if (skipSpawn) DoSpawn();
        else StartCoroutine(SpawnAnimation());
        playerTransform = GameObject.FindWithTag("Player").transform;
        if (stagger && stagger.weakPointManager) stagger.weakPointManager.handleOwnDestruction = false;
        // if (enemyClass.GetType() == typeof(EnemyClass_Champion)) weakPointManager.dieOnWeakpointsComplete = false;
        if (enemyClass == EnemyClass.Standard) stagger.weakPointManager.dieOnWeakpointsComplete = false;
        if (attack != null && navAgent != null) navAgent.stoppingDistance = attack.AttackRange;
        else if (navAgent != null) navAgent.stoppingDistance = chaseStopDistance;
        if (navAgent != null) navAgent.acceleration = 51 - chaseEasing * 50;
    }
    void Update()
    {
        StateControl();
        if (animator != null) Animations();
        if (stagger.weakPointManager) CheckDie();
    }

    void StateControl()
    {
        if (debugMode) Debug.Log($"[{this}] BehaviourState: [{behaviourState}]");
        switch (behaviourState)
        {
            case BehaviourState.Idling:
                if (PlayerInAggroRange() && CanChase()) behaviourState = BehaviourState.Chasing;
                else if (IsStunned()) DoStun();
                if (!AttackReady() && AttackEnabled() && PlayerInAttackRange()) behaviourState = BehaviourState.Waiting;
                else if (AttackReady() && PlayerInAttackRange()) DoAttack();
                return;
            case BehaviourState.Chasing:
                LookAtPlayer();
                if (debugMode) Debug.Log($"[{this}] ChaseReady(): {ChaseReady()} | TargetPos(): {TargetPos()}");
                if (ChaseReady()) ChasePlayer();
                if (!AttackReady() && AttackEnabled() && PlayerInAttackRange()) behaviourState = BehaviourState.Waiting;
                else if (AttackReady() && PlayerInAttackRange()) DoAttack();
                else if (IsStunned()) DoStun();
                if (!neverGiveUpChase && !PlayerInAggroRange()) behaviourState = BehaviourState.Idling;
                return;
            case BehaviourState.Attacking:
                if (IsWindingUp()) stagger.windingUp = true;
                else if (stagger != null) stagger.windingUp = false;
                if (!IsAttacking() && PlayerInAttackRange()) behaviourState = BehaviourState.Waiting;
                else if (!IsAttacking() && PlayerInAggroRange() && CanChase()) behaviourState = BehaviourState.Chasing;
                else if (!IsAttacking()) behaviourState = BehaviourState.Idling;
                else if (IsStunned()) DoStun();
                return;
            case BehaviourState.Waiting:
                LookAtPlayer();
                if (AttackReady() && PlayerInAttackRange()) DoAttack();
                else if (IsStunned()) DoStun();
                else if ((AttackReady() || !PlayerInAttackRange()) && chasePlayer) behaviourState = BehaviourState.Chasing;
                else if (!AttackEnabled()) behaviourState = BehaviourState.Idling;
                return;
            case BehaviourState.Stunned:
                if (!IsStunned()) behaviourState = BehaviourState.Idling;
                return;
            case BehaviourState.Spawning:
                return;
            case BehaviourState.Dying:
                return;
            case BehaviourState.Inactive:
                return;
        }
    }
    bool CanChase()
    {
        if (!chasePlayer) return false;
        else if (onlyChaseIfAttackReady && !AttackReady()) return false;
        else return true;
    }
    void LookAtPlayer()
    {
        transform.LookAt(playerTransform);
    }
    bool ChaseReady()
    {
        if (TargetPos() == transform.position) return false;
        if (behaviourState == BehaviourState.Stunned) return false;
        // from nak script
        if (chasePlayer && navAgent != null && navAgent.isOnNavMesh) return true;
        else return false;
    }
    void ChasePlayer()
    {
        // from nak script
        if (navAgent.isStopped) navAgent.isStopped = false;
        navAgent.speed = chaseSpeed;
        navAgent.SetDestination(TargetPos());
        if (debugMode) Debug.Log($"[{this}] Chasing to {TargetPos()}");
    }
    Vector3 TargetPos()
    {
        if (playerTransform != null) return playerTransform.position;
        else return transform.position;
    }
    bool PlayerInAggroRange()
    {
        if (alwaysAggro) return true;
        if ((transform.position - playerTransform.position).magnitude < aggroRange) return true;
        else return false;
    }
    bool PlayerInAttackRange()
    {
        if ((transform.position - playerTransform.position).magnitude < attack.AttackRange) return true;
        else return false;
    }
    bool IsAttacking()
    {
        if (attack == null) return false;
        if (attack.attackState == EnemyAttack_Base.AttackState.Attacking || IsWindingUp() || IsWindingDown()) return true;
        else return false;
    }
    bool IsWindingUp()
    {
        if (attack == null) return false;
        if (attack.attackState == EnemyAttack_Base.AttackState.WindUp) return true;
        else return false;
    }
    bool IsWindingDown()
    {
        if (attack == null) return false;
        if (attack.attackState == EnemyAttack_Base.AttackState.WindDown) return true;
        else return false;
    }
    void DoSpawn()
    {
        if (PlayerInAggroRange() && CanChase()) behaviourState = BehaviourState.Chasing;
        else behaviourState = BehaviourState.Idling;
        if (stagger != null && !stagger.canBeHit) stagger.canBeHit = true;
        if (debugMode) Debug.Log($"[{this}] Spawned.");
    }
    IEnumerator SpawnAnimation()
    {
        if (stagger != null && stagger.canBeHit) stagger.canBeHit = false;
        if (debugMode) Debug.Log($"[{this}] Spawning...");
        behaviourState = BehaviourState.Spawning;
        yield return new WaitForSeconds(spawnDelay);
        DoSpawn();
        yield break;
    }
    void DoAttack()
    {
        if (debugMode) Debug.Log($"[{this}] PlayerInAttackRange: [{PlayerInAttackRange()}] | attack: [{attack}]");
        behaviourState = BehaviourState.Attacking;
        attack.InitiateAttack(playerTransform.position);
        if (debugMode) Debug.Log($"[{this}] Doing attack: [{attack}]");
    }
    bool AttackReady()
    {
        if (!AttackEnabled()) return false;
        if (AttackEnabled() && attack.attackState == EnemyAttack_Base.AttackState.Ready) return true;
        else return false;
    }
    bool AttackEnabled()
    {
        if (attack == null) return false;
        if (!attack.isActiveAndEnabled) return false;
        else return true;
    }
    void DoStun()
    {
        behaviourState = BehaviourState.Stunned;
        if (navAgent != null && !navAgent.isStopped)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
        if (!IsStunned()) stagger.TriggerStagger();
        if (IsAttacking()) attack.InterruptAttack();
    }
    bool IsStunned()
    {
        if (stagger != null && stagger.IsStaggered) return true;
        else return false;
    }

    void TriggerSummons()
    {
        if (summonsPrefab == null)
        {
            Debug.LogWarning($"[{this}] Missing minion prefab! Attach it, don't @ me :C.", gameObject);
            return;
        }
        Debug.Log($"[{this}] Spawning {numberOfSummons} minions!");


        for (int i = 0; i < numberOfSummons; i++)
        {
            //circle around miniboss
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * summonRadius;
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 targetSpawnPos = transform.position + spawnOffset;

            //try to prevent spawning in walls by validating navs
            Enemy spawnedMinion = null;
            if (UnityEngine.AI.NavMesh.SamplePosition(targetSpawnPos, out UnityEngine.AI.NavMeshHit hit, summonRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnedMinion = Instantiate(summonsPrefab, hit.position, Quaternion.identity);
            }
            else
            {
                //fallback to positional if navmesh fails
                spawnedMinion = Instantiate(summonsPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    private int currentCycle = 0;
    private void CheckDie()
    {
        if (stagger && enemyClass == EnemyClass.Champion)
        {
            if (stagger.weakPointManager.CyclesComplete >= numberOfPhases) Die();
            else if (stagger.weakPointManager.CyclesComplete > currentCycle)
            {
                currentCycle++;
                Debug.Log($"Cycle {stagger.weakPointManager.CyclesComplete} complete.");
                if (doSummons && summonOnCycles != null && Array.IndexOf(summonOnCycles, currentCycle) != -1)
                TriggerSummons();
            }
        }
        else if (stagger && enemyClass == EnemyClass.Thrall)
        {
            if (stagger.DamageTaken > 0) Die();
        }
        else if (stagger && stagger.weakPointManager && stagger.weakPointManager.CyclesComplete > 0) Die();
    }

    private bool isDying;
    public void Die()
    {
        if (isDying) return;
        isDying = true;

        // do death anim here!

        Destroy(gameObject);
    }

    void Animations()
    {
        if (behaviourState == BehaviourState.Idling || behaviourState == BehaviourState.Waiting) animator.SetTrigger("idle");
        else if (AttackEnabled() && attack.attackState == EnemyAttack_Base.AttackState.WindUp) animator.SetTrigger("windUp");
        else if (AttackEnabled() && attack.attackState == EnemyAttack_Base.AttackState.WindDown) animator.SetTrigger("attack");
        else if (behaviourState == BehaviourState.Chasing) animator.SetTrigger("chase");
        else if (behaviourState == BehaviourState.Spawning) animator.SetTrigger("spawn");
        else if (behaviourState == BehaviourState.Stunned) animator.SetTrigger("stun");

        if (behaviourState == BehaviourState.Spawning) animator.speed = 1 / spawnDelay;
        else if (AttackEnabled() && attack.attackState == EnemyAttack_Base.AttackState.WindUp) animator.speed = 1 / attack.WindUpTime;
        else animator.speed = 1;
    }
}
