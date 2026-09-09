// Summary:
// Abstract base class for all enemy behaviour controllers. Owns the state machine, all shared toggles and parameters, animation, 
// class/death/summon logic, and spawner lifecycle. Subclasses implement movement (NavMeshAgent vs custom movement scripts).

using System.Collections;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class EnemyBehaviourBase : MonoBehaviour
{
    public enum BehaviourState { Inactive, Spawning, Idling, Chasing, Attacking, Waiting, Stunned, Returning, Retreating, Dying };
    public enum EnemyClass { Standard, Champion, Thrall };

    [Header("Enemy Options")]
    [SerializeField] private EnemyClass enemyClass = EnemyClass.Standard;
    [SerializeField] private bool skipSpawn;
    [Tooltip("Time in seconds it takes the enemy to spawn")]
    [SerializeField] private float spawnDelay = 3f;

    [Header("Aggro")]
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private float aggroRange = 10f;

    [Header("Chase")]
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool onlyChaseIfAttackReady;
    [SerializeField] private bool neverGiveUpChase;
    [Tooltip("Max distance the enemy will chase from its spawn point. Ignored if neverGiveUpChase is on.")]
    [SerializeField] private float chaseRange = 20f;
    [SerializeField] private float chaseSpeed = 5f;
    [Tooltip("How close the enemy stops to the player. Overridden by the shortest attack range if attacks are assigned.")]
    [SerializeField] private float chaseStopDistance = 2.5f;

    [Header("Return to Origin")]
    [Tooltip("If enabled, the enemy walks back to its spawn point after losing aggro instead of idling in place.")]
    [SerializeField] private bool returnToOrigin = true;
    [SerializeField] private float returnSpeed = 3f;

    [Header("Retreat")]
    [Tooltip("If enabled, the enemy backs away from the player after finishing an attack.")]
    [SerializeField] private bool retreatAfterAttack;
    [SerializeField] private float retreatDistance = 5f;
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Strafe")]
    [Tooltip("If enabled, the enemy orbits the player while waiting for attack cooldown instead of standing still.")]
    [SerializeField] private bool strafeWhileWaiting;
    [SerializeField] private float strafeSpeed = 3f;
    [Tooltip("How often the enemy changes strafe direction in seconds.")]
    [SerializeField] private float strafeDirectionInterval = 2f;

    [Header("Contact Damage")]
    [Tooltip("If enabled, the enemy deals damage on contact with the player and dies. For kamikaze-style enemies with no attack scripts.")]
    [SerializeField] private bool kamikazeOnContact;
    [SerializeField] private int contactDamage = 10;
    [Tooltip("Distance at which contact damage triggers.")]
    [SerializeField] private float contactRadius = 1f;

    [Header("Attacks")]
    [Tooltip("All attacks available to this enemy. Priority is determined by array order.")]
    [SerializeField] private EnemyAttack_Base[] attacks;

    [Header("Stagger")]
    [SerializeField] private EnemyStagger stagger;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Champion")]
    [SerializeField] private int numberOfPhases = 3;

    [Header("Summons (Champion Only)")]
    [SerializeField] private bool doSummons = true;
    public bool DoSummons
    {
        get => doSummons;
        set => doSummons = value;
    }
    [SerializeField] private int[] summonOnCycles = new int[] { 1 };
    [SerializeField] private GameObject summonsPrefab;
    [SerializeField] private int numberOfSummons = 3;
    [SerializeField] private float summonRadius = 3f;

    [Header("Vision")]
    [SerializeField] protected EnemyVisionSensor vision;

    [Header("Debug")]
    public bool debugMode;

    // state
    private BehaviourState behaviourState = BehaviourState.Inactive;
    public BehaviourState CurrentState => behaviourState;
    protected bool ShouldUseDirectMovement => kamikazeOnContact && behaviourState == BehaviourState.Chasing;

    protected Transform playerTransform;
    protected Vector3 spawnPosition;

    // active attack tracking
    private EnemyAttack_Base currentAttack;

    // retreat
    private Vector3 retreatTarget;

    // strafe
    private float strafeDirection = 1f;
    private float strafeTimer;

    // champion
    private int currentCycle = 0;

    // spawner integration
    public bool IsPaused { get; private set; }
    public bool IsDying { get; private set; }
    private IEnemySpawner ownerSpawner;
    private bool isCreatedBySpawner;
    private bool hasReportedDeathToSpawner;


    // Lifecycle
    protected virtual void Awake()
    {
        spawnPosition = transform.position;
        playerTransform = GameObject.FindWithTag("Player").transform;

        if (vision == null) vision = GetComponent<EnemyVisionSensor>();

        if (stagger && stagger.weakPointManager) stagger.weakPointManager.handleOwnDestruction = false;
        if (enemyClass == EnemyClass.Champion && stagger && stagger.weakPointManager)
            stagger.weakPointManager.dieOnWeakpointsComplete = false;

        InitializeMovement();

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

    protected virtual void OnDestroy()
    {
        ReportDeathToSpawner();
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

        // chase leash
        if (!neverGiveUpChase)
        {
            float distFromSpawn = (transform.position - spawnPosition).magnitude;
            if (!PlayerInAggroRange() || distFromSpawn > chaseRange)
            {
                ExitChase();
                return;
            }
        }

        FacePlayer();
        DoMove(playerTransform.position, chaseSpeed, GetChaseStopDistance());

        // kamikaze: deal damage on contact and die
        if (kamikazeOnContact) CheckContactDamage();

        if (debugMode) Debug.Log($"[{this}] Chasing to {playerTransform.position}");
    }

    private void AttackState()
    {
        // windup vulnerability flag
        if (stagger != null)
            stagger.windingUp = currentAttack != null && currentAttack.IsWindingUp;

        if (IsStunned()) { EnterStun(); return; }

        // attack still in progress
        if (currentAttack != null && currentAttack.IsAttacking) return;

        // attack finished
        currentAttack = null;
        if (stagger != null) stagger.windingUp = false;

        if (retreatAfterAttack) { EnterRetreat(); return; }
        if (AnyAttackEnabled() && PlayerInAnyAttackRange()) { behaviourState = BehaviourState.Waiting; return; }
        if (PlayerInAggroRange() && CanChase()) { behaviourState = BehaviourState.Chasing; return; }
        behaviourState = BehaviourState.Idling;
    }

    private void WaitState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }

        if (!PlayerInAnyAttackRange() && chasePlayer)
        {
            behaviourState = BehaviourState.Chasing;
            return;
        }
        if (!AnyAttackEnabled()) { behaviourState = BehaviourState.Idling; return; }

        if (strafeWhileWaiting)
        {
            UpdateStrafe();
            FacePlayer();

            Vector3 target = ComputeStrafeTarget(strafeDirection);

            // if blocked, try the other direction. if both blocked, stop and face the player.
            if (!IsStrafeClear(target))
            {
                target = ComputeStrafeTarget(-strafeDirection);
                if (!IsStrafeClear(target))
                {
                    DoStop();
                    return;
                }
            }

            DoMove(target, strafeSpeed, 0.5f);
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

        DoMove(spawnPosition, returnSpeed, 0.5f);
        if (HasReachedTarget()) behaviourState = BehaviourState.Idling;
    }

    private void RetreatState()
    {
        if (IsStunned()) { EnterStun(); return; }

        DoMove(retreatTarget, retreatSpeed, 0.5f);

        if (HasReachedTarget())
        {
            if (AnyAttackEnabled() && PlayerInAnyAttackRange()) { behaviourState = BehaviourState.Waiting; return; }
            if (PlayerInAggroRange() && CanChase()) { behaviourState = BehaviourState.Chasing; return; }
            behaviourState = BehaviourState.Idling;
        }
    }


    // State Transitions
    private void EnterAttack()
    {
        EnemyAttack_Base selected = SelectAttack();
        if (selected == null) return;

        currentAttack = selected;
        behaviourState = BehaviourState.Attacking;
        DoStop();
        currentAttack.PerformAttack(playerTransform);
        if (debugMode) Debug.Log($"[{this}] Attacking with [{currentAttack}]");
    }

    private void EnterStun()
    {
        behaviourState = BehaviourState.Stunned;
        DoStop();
        if (stagger != null) stagger.windingUp = false;
        if (currentAttack != null && currentAttack.IsAttacking) currentAttack.CancelAttack();
        currentAttack = null;
        if (!IsStunned()) stagger.TriggerStagger();
    }

    private void EnterRetreat()
    {
        behaviourState = BehaviourState.Retreating;
        Vector3 awayFromPlayer = (transform.position - playerTransform.position);
        awayFromPlayer.y = 0f;
        if (awayFromPlayer.sqrMagnitude < 0.001f) awayFromPlayer = -transform.forward;
        retreatTarget = transform.position + awayFromPlayer.normalized * retreatDistance;
    }

    private void ExitChase()
    {
        DoStop();
        if (returnToOrigin) behaviourState = BehaviourState.Returning;
        else behaviourState = BehaviourState.Idling;
    }


    // Attack Selection
    // select the highest-priority attack that's ready, in range, and passes ShouldUse
    private EnemyAttack_Base SelectAttack()
    {
        if (attacks == null || attacks.Length == 0) return null;

        float dist = DistanceToPlayer();

        // first pass: ready + in range + ShouldUse
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null || !attacks[i].isActiveAndEnabled) continue;
            if (!attacks[i].IsReady) continue;
            if (dist > attacks[i].AttackRange) continue;
            if (attacks[i].ShouldUse(playerTransform)) return attacks[i];
        }

        // fallback: ready + in range, ignore ShouldUse
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null || !attacks[i].isActiveAndEnabled) continue;
            if (!attacks[i].IsReady) continue;
            if (dist > attacks[i].AttackRange) continue;
            return attacks[i];
        }

        return null;
    }

    // can we attack right now?
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

    // is the player in range of any enabled attack?
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
        if (!chasePlayer) return false;
        if (onlyChaseIfAttackReady && !AnyAttackReady()) return false;
        return true;
    }

    // is any attack enabled on this enemy?
    private bool AnyAttackEnabled()
    {
        if (attacks == null) return false;
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] != null && attacks[i].isActiveAndEnabled) return true;
        }
        return false;
    }

    // is any attack off cooldown?
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

    // chase stops at the shortest attack range, or contact radius for kamikaze, or the manual stop distance
    private float GetChaseStopDistance()
    {
        if (attacks != null && attacks.Length > 0)
        {
            float minRange = chaseStopDistance;
            bool found = false;
            for (int i = 0; i < attacks.Length; i++)
            {
                if (attacks[i] == null || !attacks[i].isActiveAndEnabled) continue;
                if (!found || attacks[i].AttackRange < minRange)
                {
                    minRange = attacks[i].AttackRange;
                    found = true;
                }
            }
            if (found) return minRange;
        }

        // kamikaze enemies chase all the way to the player
        if (kamikazeOnContact) return 0f;

        return chaseStopDistance;
    }


    // Contact Damage
    private void CheckContactDamage()
    {
        if (DistanceToPlayer() > contactRadius) return;

        IDamageable damageable = playerTransform.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo(contactDamage, transform.position, transform.forward, gameObject);
        damageable.TakeDamage(info);
        Die();
    }


    // Strafe
    private void UpdateStrafe()
    {
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection *= -1f;
            strafeTimer = strafeDirectionInterval;
        }
    }

    private Vector3 ComputeStrafeTarget(float direction)
    {
        if (playerTransform == null) return transform.position;

        Vector3 toEnemy = transform.position - playerTransform.position;
        toEnemy.y = 0f;
        float radius = GetChaseStopDistance();
        if (radius < 0.1f) return transform.position;

        Vector3 lateral = Vector3.Cross(Vector3.up, toEnemy.normalized) * direction;
        Vector3 aheadOnArc = transform.position + lateral * 2f;
        Vector3 fromPlayer = aheadOnArc - playerTransform.position;
        fromPlayer.y = 0f;

        return playerTransform.position + fromPlayer.normalized * radius;
    }

    // override in subclasses to check for obstacles between the enemy and the strafe target
    protected virtual bool IsStrafeClear(Vector3 target)
    {
        return true;
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


    // Facing
    protected virtual void FacePlayer()
    {
        if (playerTransform == null) return;
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }


    // Champion / Death stuff
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
        else // Standard
        {
            if (stagger.weakPointManager.CyclesComplete > 0) Die();
        }
    }

    public virtual void Die()
    {
        if (IsDying) return;
        IsDying = true;
        behaviourState = BehaviourState.Dying;
        DoStop();

        OnDying();
        ReportDeathToSpawner();

        Destroy(gameObject);
    }

    protected virtual void OnDying() { }


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

        // state triggers
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

        // speed scaling
        if (behaviourState == BehaviourState.Spawning)
            animator.speed = 1f / spawnDelay;
        else if (windingUp && currentAttack.WindupDuration > 0f)
            animator.speed = 1f / currentAttack.WindupDuration;
        else
            animator.speed = 1f;
    }


    // Spawner Integration (gonna make new spawning system stuff soon anyway lol)
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
        OnPauseStateChanged(isPaused);
    }

    protected virtual void OnPauseStateChanged(bool isPaused) { }

    private void ReportDeathToSpawner()
    {
        if (hasReportedDeathToSpawner) return;
        if (!isCreatedBySpawner || ownerSpawner == null) return;
        if (ownerSpawner is UnityEngine.Object unityOwner && unityOwner == null) return;

        hasReportedDeathToSpawner = true;
        ownerSpawner.NotifyEnemyDeath(this);
    }


    // Vision stuff (are we even using this anymore idek)
    protected bool HasVisionTarget => vision != null && vision.HasTarget;
    protected Transform VisionTarget => vision != null ? vision.Target : null;
    protected bool SensorHasVision() => vision != null && vision.IsTargetInVision();
    protected bool SensorDetectsTarget() => vision != null && vision.IsTargetDetected();


    // Movement
    protected abstract void InitializeMovement();
    protected abstract void DoMove(Vector3 target, float speed, float stopDistance);
    protected abstract void DoStop();
    protected abstract bool HasReachedTarget();
}