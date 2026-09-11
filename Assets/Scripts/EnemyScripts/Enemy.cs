// Summary:
// Core enemy behaviour controller. Owns the state machine, aggro, class/death/summon logic, animation, and spawner lifecycle. 
// Movement is delegated to a pluggable IEnemyMovement script. Attacks are modular components in a priority-ordered array.
// If no movement script is assigned, the enemy is stationary (idles and attacks in place).

using System.Collections;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    public enum BehaviourState { Inactive, Spawning, Idling, Chasing, Attacking, Waiting, Stunned, Returning, Retreating, Dying };
    public enum EnemyClass { Standard, Champion, Thrall };

    [Header("Enemy Options")]
    [SerializeField] private EnemyClass enemyClass = EnemyClass.Standard;
    [SerializeField] private bool skipSpawn;
    [ShowIf("skipSpawn", false)]
    [Tooltip("Time in seconds it takes the enemy to spawn.")]
    [SerializeField] private float spawnDelay = 3f;

    [Header("Aggro")]
    [SerializeField] private bool alwaysAggro;
    [ShowIf("alwaysAggro", false)]
    [SerializeField] private float aggroRange = 10f;

    [Header("Movement")]
    [Tooltip("Drop in any MonoBehaviour that implements IEnemyMovement. Leave empty for a stationary enemy.")]
    [SerializeField] private MonoBehaviour movementScript;

    [Header("Attacks")]
    [Tooltip("All attacks available to this enemy. Priority is determined by array order.")]
    [SerializeField] private EnemyAttack_Base[] attacks;

    [Header("Stagger")]
    [SerializeField] private EnemyStagger stagger;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [ShowIf("enemyClass", (int)EnemyClass.Champion, Header = "Champion")]
    [SerializeField] private int numberOfPhases = 3;

    [ShowIf("enemyClass", (int)EnemyClass.Champion, Header = "Summons (Champion Only)")]
    [SerializeField] private bool doSummons = false;
    public bool DoSummons
    {
        get => doSummons;
        set => doSummons = value;
    }
    [ShowIf("enemyClass", (int)EnemyClass.Champion)]
    [SerializeField] private int[] summonOnCycles = new int[] { 1 };
    [ShowIf("enemyClass", (int)EnemyClass.Champion)]
    [SerializeField] private GameObject summonsPrefab;
    [ShowIf("enemyClass", (int)EnemyClass.Champion)]
    [SerializeField] private int numberOfSummons = 3;
    [ShowIf("enemyClass", (int)EnemyClass.Champion)]
    [SerializeField] private float summonRadius = 3f;

    [Header("Debug")]
    public bool debugMode;

    // state
    private BehaviourState behaviourState = BehaviourState.Inactive;
    public BehaviourState CurrentState => behaviourState;

    private Transform playerTransform;
    private IEnemyMovement movement;

    // active attack tracking
    private EnemyAttack_Base currentAttack;

    // champion
    private int currentCycle = 0;

    // spawner integration
    public bool IsPaused { get; private set; }
    public bool IsDying { get; private set; }
    private IEnemySpawner ownerSpawner;
    private bool isCreatedBySpawner;
    private bool hasReportedDeathToSpawner;


    // Lifecycle
    private void Awake()
    {
        playerTransform = GameObject.FindWithTag("Player").transform;

        // initialize movement
        if (movementScript != null)
            movement = movementScript as IEnemyMovement;
        if (movement != null)
            movement.Initialize();

        // stagger/weakpoint setup
        if (stagger && stagger.weakPointManager) stagger.weakPointManager.handleOwnDestruction = false;
        if (enemyClass == EnemyClass.Champion && stagger && stagger.weakPointManager)
            stagger.weakPointManager.dieOnWeakpointsComplete = false;

        if (skipSpawn) DoSpawn();
        else StartCoroutine(SpawnSequence());
    }

    private void Reset()
    {
        if (!GetComponent<EnemyStagger>())
        {
            Debug.LogWarning($"[{this}] no Stagger component found! Adding one now.");
            gameObject.AddComponent(typeof(EnemyStagger));
        }
    }

    private void OnDestroy()
    {
        ReportDeathToSpawner();
    }

    private void OnDisable()
    {
        if (movement != null) movement.Stop();
    }

    private void Update()
    {
        if (IsPaused || IsDying) return;

        StateControl();
        if (animator != null) Animations();
        if (stagger && stagger.weakPointManager) CheckDie();
    }


    // State Machine
    private void StateControl()
    {
        if (debugMode) Debug.Log($"[{this}] State: [{behaviourState}]");

        switch (behaviourState)
        {
            case BehaviourState.Idling:     IdleState();      return;
            case BehaviourState.Chasing:    ChaseState();     return;
            case BehaviourState.Attacking:  AttackState();    return;
            case BehaviourState.Waiting:    WaitState();      return;
            case BehaviourState.Stunned:    StunState();      return;
            case BehaviourState.Returning:  ReturnState();    return;
            case BehaviourState.Retreating: RetreatState();   return;
            case BehaviourState.Spawning:   return;
            case BehaviourState.Dying:      return;
            case BehaviourState.Inactive:   return;
        }
    }

    private void IdleState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }
        if (!CanAttack() && PlayerInAnyAttackRange() && AnyAttackEnabled()) { behaviourState = BehaviourState.Waiting; return; }
        if (PlayerInAggroRange() && CanChase()) { behaviourState = BehaviourState.Chasing; return; }
    }

    private void ChaseState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }
        if (!CanAttack() && PlayerInAnyAttackRange() && AnyAttackEnabled()) { behaviourState = BehaviourState.Waiting; return; }

        if (movement != null && movement.ShouldExitChase(PlayerInAggroRange()))
        {
            ExitChase();
            return;
        }

        if (movement != null)
        {
            movement.FaceTarget(playerTransform.position);
            movement.Chase(playerTransform.position);
        }

        if (debugMode) Debug.Log($"[{this}] Chasing to {playerTransform.position}");
    }

    private void AttackState()
    {
        if (stagger != null)
            stagger.windingUp = currentAttack != null && currentAttack.IsWindingUp;

        if (IsStunned()) { EnterStun(); return; }

        // continue movement during windup if the attack allows it
        if (currentAttack != null && currentAttack.IsWindingUp && !attackMovementPaused)
        {
            if (movement != null && movement.StrafeEnabled)
                movement.Strafe(playerTransform.position);
            else if (movement != null)
                movement.FaceTarget(playerTransform.position);
        }

        // pause movement when windup ends and strike begins
        if (currentAttack != null && !currentAttack.IsWindingUp && !attackMovementPaused)
        {
            if (movement != null) movement.SetPaused(true);
            attackMovementPaused = true;
        }

        // attack still in progress
        if (currentAttack != null && currentAttack.IsAttacking) return;

        // attack finished
        currentAttack = null;
        if (stagger != null) stagger.windingUp = false;
        if (movement != null) movement.SetPaused(false);
        attackMovementPaused = false;

        if (movement != null && movement.RetreatEnabled) { EnterRetreat(); return; }
        if (AnyAttackEnabled() && PlayerInAnyAttackRange()) { behaviourState = BehaviourState.Waiting; return; }
        if (PlayerInAggroRange() && CanChase()) { behaviourState = BehaviourState.Chasing; return; }
        behaviourState = BehaviourState.Idling;
    }

    private void WaitState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }

        if (!PlayerInAnyAttackRange() && CanChase())
        {
            behaviourState = BehaviourState.Chasing;
            return;
        }
        if (!AnyAttackEnabled()) { behaviourState = BehaviourState.Idling; return; }

        if (movement != null && movement.StrafeEnabled)
        {
            FacePlayer();
            movement.Strafe(playerTransform.position);
        }
        else
        {
            FacePlayer();
        }
    }

    private void StunState()
    {
        if (!IsStunned()) behaviourState = BehaviourState.Idling;
    }

    private void ReturnState()
    {
        if (PlayerInAggroRange() && CanChase())
        {
            behaviourState = BehaviourState.Chasing;
            return;
        }
        if (movement == null || movement.HasReachedTarget)
            behaviourState = BehaviourState.Idling;
    }

    private void RetreatState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (movement == null || movement.HasReachedTarget)
        {
            if (AnyAttackEnabled() && PlayerInAnyAttackRange()) { behaviourState = BehaviourState.Waiting; return; }
            if (PlayerInAggroRange() && CanChase()) { behaviourState = BehaviourState.Chasing; return; }
            behaviourState = BehaviourState.Idling;
        }
    }


    // State Transitions
    private bool attackMovementPaused;

    private void EnterAttack()
    {
        EnemyAttack_Base selected = SelectAttack();
        if (selected == null) return;

        currentAttack = selected;
        behaviourState = BehaviourState.Attacking;

        // pause movement immediately unless the attack allows movement during windup
        attackMovementPaused = !currentAttack.MoveWhileWindingUp;
        if (attackMovementPaused && movement != null) movement.SetPaused(true);

        currentAttack.PerformAttack(playerTransform);
        if (debugMode) Debug.Log($"[{this}] Attacking with [{currentAttack}]");
    }

    private void EnterStun()
    {
        behaviourState = BehaviourState.Stunned;
        if (movement != null) { movement.SetPaused(false); movement.Stop(); }
        if (stagger != null) stagger.windingUp = false;
        if (currentAttack != null && currentAttack.IsAttacking) currentAttack.CancelAttack();
        currentAttack = null;
        attackMovementPaused = false;
        if (!IsStunned()) stagger.TriggerStagger();
    }

    private void EnterRetreat()
    {
        behaviourState = BehaviourState.Retreating;
        if (movement != null) movement.BeginRetreat(playerTransform.position);
    }

    private void ExitChase()
    {
        if (movement != null) movement.Stop();
        if (movement != null && movement.ReturnEnabled)
        {
            behaviourState = BehaviourState.Returning;
            movement.BeginReturn();
        }
        else
        {
            behaviourState = BehaviourState.Idling;
        }
    }


    // Attack Selection
    private EnemyAttack_Base SelectAttack()
    {
        if (attacks == null || attacks.Length == 0) return null;

        float dist = DistanceToPlayer();

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null || !attacks[i].isActiveAndEnabled) continue;
            if (!attacks[i].IsReady) continue;
            if (dist > attacks[i].AttackRange) continue;
            if (attacks[i].ShouldUse(playerTransform)) return attacks[i];
        }

        // fallback: ignore ShouldUse
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null || !attacks[i].isActiveAndEnabled) continue;
            if (!attacks[i].IsReady) continue;
            if (dist > attacks[i].AttackRange) continue;
            return attacks[i];
        }

        return null;
    }

    private bool CanAttack()
    {
        return SelectAttack() != null;
    }


    // Condition Checks
    private bool PlayerInAggroRange()
    {
        if (alwaysAggro) return true;
        return DistanceToPlayer() < aggroRange;
    }

    private bool PlayerInAnyAttackRange()
    {
        if (attacks == null) return false;
        float dist = DistanceToPlayer();
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] != null && attacks[i].isActiveAndEnabled && dist < attacks[i].AttackRange)
                return true;
        }
        return false;
    }

    private float DistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return (transform.position - playerTransform.position).magnitude;
    }

    private bool CanChase()
    {
        if (movement == null) return false;
        return movement.CanChase(AnyAttackReady());
    }

    private bool AnyAttackEnabled()
    {
        if (attacks == null) return false;
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] != null && attacks[i].isActiveAndEnabled) return true;
        }
        return false;
    }

    private bool AnyAttackReady()
    {
        if (attacks == null) return false;
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] != null && attacks[i].isActiveAndEnabled && attacks[i].IsReady) return true;
        }
        return false;
    }

    private bool IsStunned()
    {
        return stagger != null && stagger.IsStaggered;
    }


    // Facing
    private void FacePlayer()
    {
        if (playerTransform == null) return;
        if (movement != null)
        {
            movement.FaceTarget(playerTransform.position);
        }
        else
        {
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }


    // Spawn
    private void DoSpawn()
    {
        if (stagger != null && !stagger.canBeHit) stagger.canBeHit = true;
        if (PlayerInAggroRange() && CanChase()) behaviourState = BehaviourState.Chasing;
        else behaviourState = BehaviourState.Idling;
        if (debugMode) Debug.Log($"[{this}] Spawned.");
    }

    private IEnumerator SpawnSequence()
    {
        if (stagger != null && stagger.canBeHit) stagger.canBeHit = false;
        if (debugMode) Debug.Log($"[{this}] Spawning...");
        behaviourState = BehaviourState.Spawning;
        yield return new WaitForSeconds(spawnDelay);
        DoSpawn();
    }


    // Champion / Death
    private void CheckDie()
    {
        if (stagger == null || stagger.weakPointManager == null) return;

        if (enemyClass == EnemyClass.Champion)
        {
            if (stagger.weakPointManager.CyclesComplete >= numberOfPhases)
            {
                Die();
            }
            else if (stagger.weakPointManager.CyclesComplete > currentCycle)
            {
                currentCycle++;
                if (debugMode) Debug.Log($"[{this}] Cycle {currentCycle} complete.");
                if (doSummons && summonOnCycles != null && Array.IndexOf(summonOnCycles, currentCycle) != -1)
                    TriggerSummons();
            }
        }
        else if (enemyClass == EnemyClass.Thrall)
        {
            if (stagger.DamageTaken > 0) Die();
        }
        else
        {
            if (stagger.weakPointManager.CyclesComplete > 0) Die();
        }
    }

    public void Die()
    {
        if (IsDying) return;
        IsDying = true;
        behaviourState = BehaviourState.Dying;
        if (movement != null) movement.Stop();

        ReportDeathToSpawner();
        Destroy(gameObject);
    }


    // Summons
    private void TriggerSummons()
    {
        if (summonsPrefab == null)
        {
            Debug.LogWarning($"[{this}] Missing summon prefab!", gameObject);
            return;
        }

        if (debugMode) Debug.Log($"[{this}] Spawning {numberOfSummons} summons!");

        for (int i = 0; i < numberOfSummons; i++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * summonRadius;
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 targetPos = transform.position + spawnOffset;

            if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out UnityEngine.AI.NavMeshHit hit, summonRadius, UnityEngine.AI.NavMesh.AllAreas))
                Instantiate(summonsPrefab, hit.position, Quaternion.identity);
            else
                Instantiate(summonsPrefab, transform.position, Quaternion.identity);
        }
    }


    // Animation
    private void Animations()
    {
        bool windingUp = currentAttack != null && currentAttack.IsWindingUp;
        bool attacking = currentAttack != null && currentAttack.IsAttacking && !windingUp;

        if (behaviourState == BehaviourState.Idling || behaviourState == BehaviourState.Waiting)
            animator.SetTrigger("idle");
        else if (windingUp)
            animator.SetTrigger("windUp");
        else if (attacking)
            animator.SetTrigger("attack");
        else if (behaviourState == BehaviourState.Chasing || behaviourState == BehaviourState.Returning || behaviourState == BehaviourState.Retreating)
            animator.SetTrigger("chase");
        else if (behaviourState == BehaviourState.Spawning)
            animator.SetTrigger("spawn");
        else if (behaviourState == BehaviourState.Stunned)
            animator.SetTrigger("stun");

        if (behaviourState == BehaviourState.Spawning)
            animator.speed = 1f / spawnDelay;
        else if (windingUp && currentAttack.WindupDuration > 0f)
            animator.speed = 1f / currentAttack.WindupDuration;
        else
            animator.speed = 1f;
    }


    // Spawner Integration
    public void SetOwnerSpawner(IEnemySpawner spawner)
    {
        ownerSpawner = spawner;
        isCreatedBySpawner = spawner != null;
    }

    public void SetPaused(bool isPaused)
    {
        if (IsDying) return;
        if (IsPaused == isPaused) return;

        IsPaused = isPaused;
        if (movement != null) movement.SetPaused(isPaused);
    }

    private void ReportDeathToSpawner()
    {
        if (hasReportedDeathToSpawner) return;
        if (!isCreatedBySpawner || ownerSpawner == null) return;
        if (ownerSpawner is UnityEngine.Object unityOwner && unityOwner == null) return;

        hasReportedDeathToSpawner = true;
        ownerSpawner.NotifyEnemyDeath(this);
    }
}