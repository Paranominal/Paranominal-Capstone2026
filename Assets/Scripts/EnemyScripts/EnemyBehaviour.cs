using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    private enum BehaviourState {Idling, Chasing, Attacking, Waiting, Stunned, Spawning, Dying, Inactive};
    private BehaviourState behaviourState = BehaviourState.Inactive;

    [Header("Enemy Options")]
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool neverGiveUpChase;
    [SerializeField] private bool onlyChaseIfAttackReady;
    [Header("Weak Points")]
    [SerializeField] private WeakPointManager weakPointManager;
    [Header("Attack")]
    [SerializeField] private EnemyAttack_Base attack;
    [Header("Chase")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private float chaseSpeed = 5f;
    [Range(0,1)]
    [SerializeField] private float chaseEasing = 0.5f;
    [Tooltip("Overrides NavMeshAgent stopping distance, but will also be overriden by the range of added Attack Scripts.")]
    [SerializeField] private float chaseStopDistance = 2.5f;
    [Header("Stagger")]
    [SerializeField] private EnemyStagger stagger;
    // [SerializeField] private EnemyKnockback knockback;
    // [SerializeField] private float knockbackStrength = 5;
    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Transform playerTransform;
    [SerializeField] private float aggroRange = 10;
    // [SerializeField] private float attackRange = 3;
    [SerializeField] private bool skipSpawn;
    [Tooltip("Time in seconds it takes the enemy to spawn")]
    [SerializeField] private float spawnDelay = 3;
    public bool debugMode;
    // [Tooltip("Time in seconds it takes the enemy to engage the Player after getting aggro'd")]
    // [SerializeField] private float engageDelay = 1;

    private void Start()
    {
        if (skipSpawn) DoSpawn();
        else StartCoroutine(SpawnAnimation());;
        playerTransform = GameObject.FindWithTag("Player").transform;
        if (attack != null && navAgent != null) navAgent.stoppingDistance = attack.AttackRange;
        else if (navAgent != null) navAgent.stoppingDistance = chaseStopDistance;
        if (navAgent != null) navAgent.acceleration = 51 - chaseEasing * 50;
    }

    void Update()
    {
        StateControl();
        if (animator != null) Animations();
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
    void Animations()
    {
        if (behaviourState == BehaviourState.Idling || behaviourState == BehaviourState.Waiting) animator.SetTrigger("idle");
        else if (AttackEnabled() && attack.attackState == EnemyAttack_Melee.AttackState.WindUp) animator.SetTrigger("windUp");
        else if (AttackEnabled() && attack.attackState == EnemyAttack_Melee.AttackState.WindDown) animator.SetTrigger("attack");
        else if (behaviourState == BehaviourState.Chasing) animator.SetTrigger("chase");
        else if (behaviourState == BehaviourState.Spawning) animator.SetTrigger("spawn");
        else if (behaviourState == BehaviourState.Stunned) animator.SetTrigger("stun");

        if (behaviourState == BehaviourState.Spawning) animator.speed = 1 / spawnDelay;
        else if (AttackEnabled() && attack.attackState == EnemyAttack_Melee.AttackState.WindUp) animator.speed = 1 / attack.WindUpTime;
        else animator.speed = 1;
    }
}
