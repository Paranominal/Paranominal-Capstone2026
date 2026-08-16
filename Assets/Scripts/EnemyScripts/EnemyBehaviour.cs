using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private enum BehaviourState {Idle, Chase, Attacking, Waiting, Stunned, Spawning, Dying, Inactive};
    private BehaviourState behaviourState = BehaviourState.Inactive;

    [Header("Enemy Options")]
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool neverGiveUpChase;
    [SerializeField] private bool onlyChaseIfAttackReadied;
    [SerializeField] private EnemyAttack_Melee attack;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    public Transform playerTransform;
    [SerializeField] private float aggroRange = 10;
    // [SerializeField] private float attackRange = 3;
    [SerializeField] private bool skipSpawn;
    [Tooltip("Time in seconds it takes the enemy to spawn")]
    [SerializeField] private float spawnDelay = 3;
    [Header("Debugging")]
    bool debugMode;
    // [Tooltip("Time in seconds it takes the enemy to engage the Player after getting aggro'd")]
    // [SerializeField] private float engageDelay = 1;

    private void Start()
    {
        if (skipSpawn) behaviourState = BehaviourState.Idle;
        else DoSpawn();
    }

    void Update()
    {
        StateControl();
        OnlyChaseIfAttackReadied();
        if (animator != null) Animations();
    }
    void StateControl()
    {
        if (debugMode) Debug.Log($"[{this}] BehaviourState: [{behaviourState}]");
        switch (behaviourState)
        {
            case BehaviourState.Idle:
                if (PlayerInAggroRange() && permitEngage) behaviourState = BehaviourState.Chase;
                return;
            case BehaviourState.Chase:
                LookAtPlayer();
                if (chasePlayer) ChasePlayer();
                if (!AttackReady() && PlayerInAttackRange() && attack != null) behaviourState = BehaviourState.Waiting;
                else if (AttackReady() && PlayerInAttackRange() && attack != null) DoAttack();
                //exit state
                if (!neverGiveUpChase && !PlayerInAggroRange()) behaviourState = BehaviourState.Idle;
                return;
            case BehaviourState.Attacking:
                //exit state
                if (!IsAttacking() && PlayerInAttackRange()) behaviourState = BehaviourState.Waiting;
                else if (!IsAttacking() && PlayerInAggroRange() && permitEngage) behaviourState = BehaviourState.Chase;
                else if (!IsAttacking()) behaviourState = BehaviourState.Idle;
                return;
            case BehaviourState.Waiting:
                if (AttackReady() && PlayerInAttackRange()) DoAttack();
                else if (AttackReady() || !PlayerInAttackRange()) behaviourState = BehaviourState.Chase;
                return;
            case BehaviourState.Stunned:
                return;
            case BehaviourState.Spawning:
                return;
            case BehaviourState.Dying:
                return;
            case BehaviourState.Inactive:
                return;
        }
    }
    bool permitEngage = true;
    void OnlyChaseIfAttackReadied()
    {
        if (onlyChaseIfAttackReadied && !AttackReady()) permitEngage = false;
        else permitEngage = true;
    }
    void LookAtPlayer()
    {
        transform.LookAt(playerTransform);
    }
    void ChasePlayer()
    {
        return;
    }
    bool PlayerInAggroRange()
    {
        if ((transform.position - playerTransform.position).magnitude < aggroRange) return true;
        else return false;
    }
    bool PlayerInAttackRange()
    {
        if ((transform.position - playerTransform.position).magnitude < attack.attackRange) return true;
        else return false;
    }
    bool IsAttacking()
    {
        if (attack.attackState == EnemyAttack_Melee.AttackState.Attacking) return true;
        else if (attack.attackState == EnemyAttack_Melee.AttackState.WindUp) return true;
        else return false;
    }
    void DoSpawn()
    {
        StartCoroutine(Spawn());
    }
    IEnumerator Spawn()
    {
        if (debugMode) Debug.Log($"[{this}] Spawning...");
        behaviourState = BehaviourState.Spawning;
        yield return new WaitForSeconds(spawnDelay);
        if (PlayerInAggroRange()) behaviourState = BehaviourState.Chase;
        else behaviourState = BehaviourState.Idle;
        if (debugMode) Debug.Log($"[{this}] Spawning complete");
        yield break;
    }
    void DoAttack()
    {
        if (debugMode) Debug.Log($"[{this}] PlayerInAttackRange: [{PlayerInAttackRange()}] | attack: [{attack}]");
        behaviourState = BehaviourState.Attacking;
        attack.InitiateAttack();
        if (debugMode) Debug.Log($"[{this}] Doing attack: [{attack}]");
    }
    bool AttackReady()
    {
        if (attack.attackState == EnemyAttack_Melee.AttackState.Ready) return true;
        else return false;
    }
    void Animations()
    {
        if (behaviourState == BehaviourState.Idle || behaviourState == BehaviourState.Waiting) animator.SetTrigger("idle");
        else if (attack != null && attack.attackState == EnemyAttack_Melee.AttackState.WindUp) animator.SetTrigger("windUp");
        else if (behaviourState == BehaviourState.Chase) animator.SetTrigger("aggro");
        else if (behaviourState == BehaviourState.Spawning) animator.SetTrigger("spawn");

        if (behaviourState == BehaviourState.Spawning) animator.speed = 1 / spawnDelay;
        else if (attack.attackState == EnemyAttack_Melee.AttackState.WindUp) animator.speed = 1 / attack.windUpTime;
        else animator.speed = 1;
    }
}
