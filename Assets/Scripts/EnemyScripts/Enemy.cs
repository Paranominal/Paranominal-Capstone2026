// Summary:
// Core enemy behaviour controller. Owns the state machine, aggro, class/death/summon logic, animation, and spawner lifecycle. 
// Movement is delegated to a pluggable IEnemyMovement script. If no movement script is assigned, the enemy is stationary (idles and attacks in place).
// Attacks are modular components in a priority-ordered array.
// EDIT (attack-priority): the enemy fires the highest-priority attack that's ready and in range, and moves towards the range of the
// highest-priority attack that's ready. If every attack is on cooldown, it holds its current position.
// EDIT (boss): key methods are virtual and core references protected so Enemy_Boss can extend this class.

using System.Collections;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    // EDIT (boss): added Downed (state machine does nothing while in it, used by Enemy_Boss).
    public enum BehaviourState { Inactive, Spawning, Idling, Chasing, Attacking, Waiting, Stunned, Returning, Retreating, Dying, Downed };
    // EDIT (boss): added Boss. Set automatically by Enemy_Boss, not meant to be picked on a plain Enemy.
    public enum EnemyClass { Standard, Champion, Thrall, Boss };

    [Header("Enemy Options")]
    // EDIT (boss): protected so Enemy_Boss can lock it to Boss
    [SerializeField] protected EnemyClass enemyClass = EnemyClass.Standard;
    [SerializeField] private bool skipSpawn;
    [ShowIf("skipSpawn", false)]
    [Tooltip("Time in seconds it takes the enemy to spawn.")]
    [SerializeField] private float spawnDelay = 1f;
    // EDIT (special-shot): immune enemies block the Special Shot, unless it also hits their exposed Special weakpoint.
    [Tooltip("Blocks the Special Shot. Only its Special weakpoint can be hit by it, and the shot only pierces through when that weakpoint is hit.")]
    [SerializeField] private bool immuneToSpecialShot;
    public bool ImmuneToSpecialShot => immuneToSpecialShot;

    [Header("Aggro")]
    [SerializeField] private bool alwaysAggro;
    [ShowIf("alwaysAggro", false)]
    [SerializeField] private float aggroRange = 10f;

    [Header("Movement")]
    [Tooltip("Drop in any MonoBehaviour that implements IEnemyMovement. Leave empty for a stationary enemy.")]
    [SerializeField] private MonoBehaviour movementScript;

    [Header("Attacks")]
    // EDIT (attack-priority): tooltip updated to spell out how priority works.
    [Tooltip("Top of the list = highest priority. The enemy uses the highest-priority attack that's ready and in range, " +
             "and moves towards the range of the highest-priority attack that's ready. If all attacks are on cooldown, it holds position.")]
    [SerializeField] private EnemyAttack_Base[] attacks;

    [Header("Stagger")]
    // EDIT (boss): protected for Enemy_Boss
    [SerializeField] protected EnemyStagger stagger;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Sound")]
    [SerializeField] private SoundPlayer soundPlayer;

    [Header("Particle Emissions")]
    [SerializeField] private ParticleEmitter bloodFxEmitter;
    [SerializeField] private ParticleEmitter immuneFxEmitter;

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

    #if UNITY_EDITOR
    [ShowIf("debugMode", Header = "Runtime State (Play Mode)")]
    [SerializeField] private string _state = "Inactive";
    [ShowIf("debugMode")]
    [SerializeField] private string _activeAttack = "None";
    // EDIT (attack-priority): shows which attack the enemy is currently moving into range for
    [ShowIf("debugMode")]
    [SerializeField] private string _targetAttack = "None";
    [ShowIf("debugMode")]
    [SerializeField] private float _orbitDistance;
    #endif

    // state
    // EDIT (boss): behaviourState, playerTransform and movement are protected for Enemy_Boss
    protected BehaviourState behaviourState = BehaviourState.Inactive;
    public BehaviourState CurrentState => behaviourState;

    protected Transform playerTransform;
    protected IEnemyMovement movement;

    // active attack tracking
    private EnemyAttack_Base currentAttack;

    // EDIT (attack-priority): attack cache, refreshed once per frame so selection isn't run several times
    private EnemyAttack_Base selectedAttack;    // highest-priority attack that's ready and in range
    private EnemyAttack_Base readyAttack;       // highest-priority attack that's ready, regardless of range
    private bool holdingPosition;
    private const float RangeTolerance = 0.5f;

    // champion
    private int currentCycle = 0;

    // spawner integration
    public bool IsPaused { get; private set; }
    public bool IsDying { get; private set; }
    private IEnemySpawner ownerSpawner;
    private bool isCreatedBySpawner;
    private bool hasReportedDeathToSpawner;

    // animation trigger guard (prevents re-queuing the same trigger every frame)
    private string lastAnimTrigger;

    // events
    public event Action<Enemy> ImmuneHit;


    // Lifecycle
    // EDIT (boss): virtual so Enemy_Boss can extend it
    protected virtual void Awake()
    {
        playerTransform = GameObject.FindWithTag("Player").transform;

        // initialize movement
        if (movementScript != null)
            movement = movementScript as IEnemyMovement;
        if (movement != null)
            movement.Initialize();

        // stagger/weakpoint setup
        if (stagger && stagger.weakPointManager) stagger.weakPointManager.handleOwnDestruction = false;
        // EDIT (boss): bosses also reuse their weakpoints between phases
        if ((enemyClass == EnemyClass.Champion || enemyClass == EnemyClass.Boss) && stagger && stagger.weakPointManager)
            stagger.weakPointManager.dieOnWeakpointsComplete = false;

        // EDIT (boss): always listen for shots so immune feedback still plays without a blood emitter assigned
        if (stagger) stagger.EnemyShot += EnemyShot;

        if (skipSpawn) DoSpawn();
        else StartCoroutine(SpawnSequence());
    }

    private void Reset()
    {
        // auto-find stagger
        if (!GetComponent<EnemyStagger>())
        {
            Debug.LogWarning($"[{this}] no Stagger component found! Adding one now.");
            gameObject.AddComponent(typeof(EnemyStagger));
        }
        stagger = GetComponent<EnemyStagger>();

        // auto-find animator
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // auto-find movement script
        if (movementScript == null)
        {
            foreach (var comp in GetComponents<MonoBehaviour>())
            {
                if (comp is IEnemyMovement && comp != this)
                {
                    movementScript = comp;
                    break;
                }
            }
        }

        // auto-find attacks
        if (attacks == null || attacks.Length == 0)
        {
            var found = GetComponents<EnemyAttack_Base>();
            if (found.Length > 0) attacks = found;
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

        // EDIT (attack-priority): refresh the attack cache before the state machine reads it
        RefreshAttackCache();

        StateControl();
        if (animator != null) Animations();
        if (stagger && stagger.weakPointManager) CheckDie();

        #if UNITY_EDITOR
        _state = behaviourState.ToString();
        _activeAttack = currentAttack != null ? currentAttack.GetType().Name : "None";
        _targetAttack = readyAttack != null ? readyAttack.GetType().Name : "None (holding)";
        _orbitDistance = GetEffectiveOrbitDistance();
        #endif
    }


    // State Machine
    private void StateControl()
    {
        if (debugMode) Debug.Log($"[{this}] State: [{behaviourState}]");

        // EDIT (attack-priority): any state other than Waiting clears the hold flag so the next hold stops movement again
        if (behaviourState != BehaviourState.Waiting) holdingPosition = false;

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
            // EDIT (boss): Downed holds until Enemy_Boss releases it
            case BehaviourState.Downed:     return;
        }
    }

    private void IdleState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }

        // EDIT (attack-priority): shared engagement check replaces the old "any attack in range" logic
        behaviourState = GetEngagedState();
    }

    private void ChaseState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }

        // EDIT (attack-priority): only keep chasing while the ready attack is out of reach
        BehaviourState next = GetEngagedState();
        if (next == BehaviourState.Idling) { ExitChase(); return; }
        if (next != BehaviourState.Chasing) { behaviourState = next; return; }

        if (movement != null && movement.ShouldExitChase(PlayerInAggroRange()))
        {
            ExitChase();
            return;
        }

        if (movement != null)
        {
            movement.FaceTarget(playerTransform.position);
            movement.Chase(playerTransform.position, GetEffectiveOrbitDistance());
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
            // EDIT (attack-priority): orbit at the current attack's range during windup so the enemy stays in range of it
            if (movement != null && movement.StrafeEnabled)
                movement.Strafe(playerTransform.position, currentAttack.AttackRange);
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

        // EDIT (attack-priority): next state comes from the shared engagement check
        behaviourState = GetEngagedState();
    }

    private void WaitState()
    {
        if (IsStunned()) { EnterStun(); return; }
        if (CanAttack()) { EnterAttack(); return; }

        // EDIT (attack-priority): move on to the next ready attack's range instead of waiting for cooldowns
        BehaviourState next = GetEngagedState();
        if (next == BehaviourState.Chasing) { behaviourState = BehaviourState.Chasing; return; }
        if (next == BehaviourState.Idling) { ExitChase(); return; }

        // EDIT (attack-priority): every attack is on cooldown, so stop once and hold position while facing the player
        if (readyAttack == null)
        {
            if (!holdingPosition)
            {
                if (movement != null) movement.Stop();
                holdingPosition = true;
            }
            FacePlayer();
            return;
        }
        holdingPosition = false;

        // an attack is ready but can't fire from here (e.g. inside its min range)
        if (movement != null && movement.StrafeEnabled)
        {
            FacePlayer();
            movement.Strafe(playerTransform.position, GetEffectiveOrbitDistance());
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
        // EDIT (attack-priority): re-engage if an attack can fire or a ready attack needs chasing
        if (PlayerInAggroRange() && (CanAttack() || GetEngagedState() == BehaviourState.Chasing))
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
            // EDIT (attack-priority): next state comes from the shared engagement check
            behaviourState = GetEngagedState();
        }
    }


    // State Transitions
    private bool attackMovementPaused;

    private void EnterAttack()
    {
        // EDIT (attack-priority): uses the cached selection
        EnemyAttack_Base selected = selectedAttack;
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

    // EDIT (boss): cancels any attack in progress and stops movement. Used by Enemy_Boss when entering Downed.
    protected void CancelCurrentAttack()
    {
        if (currentAttack != null && currentAttack.IsAttacking) currentAttack.CancelAttack();
        currentAttack = null;
        attackMovementPaused = false;
        if (stagger != null) stagger.windingUp = false;
        if (movement != null) { movement.SetPaused(false); movement.Stop(); }
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

    // EDIT (attack-priority): single place that decides what the enemy does when it isn't attacking.
    //   - A ready attack is out of reach and the enemy can chase -> Chasing (towards that attack's range).
    //   - Player is engaged (in aggro or any attack range) -> Waiting (holds if all attacks are on cooldown).
    //   - Otherwise -> Idling.
    private BehaviourState GetEngagedState()
    {
        float dist = DistanceToPlayer();

        // already chasing: the movement script's leash decides when to give up, not aggro range
        bool chaseAllowed = CanChase() && (PlayerInAggroRange() || behaviourState == BehaviourState.Chasing);

        if (readyAttack != null && dist > readyAttack.AttackRange + RangeTolerance && chaseAllowed)
            return BehaviourState.Chasing;

        if (AnyAttackEnabled() && (PlayerInAggroRange() || PlayerInAnyAttackRange()))
            return BehaviourState.Waiting;

        return BehaviourState.Idling;
    }


    // Attack Selection
    // EDIT (attack-priority): caches both selections once per frame
    private void RefreshAttackCache()
    {
        selectedAttack = SelectAttack();
        readyAttack = GetFirstReadyAttack();
    }

    private EnemyAttack_Base SelectAttack()
    {
        if (attacks == null || attacks.Length == 0) return null;

        float dist = DistanceToPlayer();

        for (int i = 0; i < attacks.Length; i++)
        {
            // EDIT (boss): shared check so Enemy_Boss can gate attacks by phase
            if (!CanUseAttack(attacks[i])) continue;
            if (!attacks[i].IsReady) continue;
            // EDIT (attack-priority): range check now respects min range
            if (!attacks[i].IsInRange(dist, RangeTolerance)) continue;
            if (attacks[i].ShouldUse(playerTransform)) return attacks[i];
        }

        // fallback: ignore ShouldUse
        for (int i = 0; i < attacks.Length; i++)
        {
            // EDIT (boss): shared check so Enemy_Boss can gate attacks by phase
            if (!CanUseAttack(attacks[i])) continue;
            if (!attacks[i].IsReady) continue;
            // EDIT (attack-priority): range check now respects min range
            if (!attacks[i].IsInRange(dist, RangeTolerance)) continue;
            return attacks[i];
        }

        return null;
    }

    // EDIT (boss): whether an attack can be considered at all. Enemy_Boss overrides this to gate attacks by phase.
    protected virtual bool CanUseAttack(EnemyAttack_Base attack)
    {
        return attack != null && attack.isActiveAndEnabled;
    }

    // EDIT (attack-priority): reads the cached selection instead of re-running it
    private bool CanAttack()
    {
        return selectedAttack != null;
    }

    // returns the highest-priority ready attack regardless of distance
    private EnemyAttack_Base GetFirstReadyAttack()
    {
        if (attacks == null) return null;
        for (int i = 0; i < attacks.Length; i++)
        {
            // EDIT (boss): shared check so Enemy_Boss can gate attacks by phase
            if (!CanUseAttack(attacks[i])) continue;
            if (!attacks[i].IsReady) continue;
            return attacks[i];
        }
        return null;
    }

    // EDIT (attack-priority): simplified. The enemy moves to the range of its highest-priority ready attack.
    // If nothing is ready, it holds its current distance. StrafeRadius is only used by enemies with no attacks.
    private float GetEffectiveOrbitDistance()
    {
        if (attacks == null || attacks.Length == 0)
            return movement != null ? movement.StrafeRadius : 2.5f;

        if (readyAttack != null) return readyAttack.AttackRange;

        return DistanceToPlayer();
    }


    // Condition Checks
    private bool PlayerInAggroRange()
    {
        if (alwaysAggro) return true;
        return DistanceToPlayer() < aggroRange;
    }

    // max range only, used to decide if the player is engaged
    private bool PlayerInAnyAttackRange()
    {
        if (attacks == null) return false;
        float dist = DistanceToPlayer();
        for (int i = 0; i < attacks.Length; i++)
        {
            // EDIT (boss): shared check
            if (CanUseAttack(attacks[i]) && dist < attacks[i].AttackRange + RangeTolerance)
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
            // EDIT (boss): shared check
            if (CanUseAttack(attacks[i])) return true;
        }
        return false;
    }

    private bool AnyAttackReady()
    {
        if (attacks == null) return false;
        for (int i = 0; i < attacks.Length; i++)
        {
            // EDIT (boss): shared check
            if (CanUseAttack(attacks[i]) && attacks[i].IsReady) return true;
        }
        return false;
    }

    // EDIT (boss): protected for Enemy_Boss
    protected bool IsStunned()
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
    // EDIT (boss): virtual so Enemy_Boss can replace it with phase handling
    protected virtual void CheckDie()
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

    // Michael edit (special-shot): Special Shot body hit. Standard and Thrall enemies die, Champions are instantly staggered.
    // Returns true if the hit did something, so the shot knows whether to count it as a SpecialHit.
    // EDIT (boss): virtual so Enemy_Boss can handle Downed
    public virtual bool HandleSpecialShotHit()
    {
        if (IsDying) return false;
        if (immuneToSpecialShot) return false;
        if (stagger != null && !stagger.canBeHit) return false; // still spawning

        if (enemyClass == EnemyClass.Champion)
        {
            if (stagger == null || IsStunned()) return false;
            if (debugMode) Debug.Log($"[{this}] Staggered by Special Shot");
            stagger.TriggerStagger();
            return true;
        }

        if (debugMode) Debug.Log($"[{this}] Killed by Special Shot");
        Die();
        return true;
    }

    // EDIT (boss): virtual so the Main boss can take its Sub-boss and minions with it
    public virtual void Die()
    {
        if (IsDying) return;
        IsDying = true;
        behaviourState = BehaviourState.Dying;
        if (movement != null) movement.Stop();
        if ( bloodFxEmitter) bloodFxEmitter.TriggerParticles();
        if (stagger != null) stagger.canBeHit = false;
        DisableColliders();

        ReportDeathToSpawner();

        // play dissolve if available, otherwise destroy immediately
        var dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.OnDissolveComplete += () => Destroy(gameObject);
            dissolve.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DisableColliders()
    {
        foreach (Collider enemyCollider in GetComponentsInChildren<Collider>(true))
        {
            if (enemyCollider != null)
                enemyCollider.enabled = false;
        }
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

        string trigger = null;

        if (behaviourState == BehaviourState.Idling || behaviourState == BehaviourState.Waiting)
            trigger = "idle";
        else if (windingUp)
            trigger = "windUp";
        else if (attacking)
            trigger = "attack";
        else if (behaviourState == BehaviourState.Chasing || behaviourState == BehaviourState.Returning || behaviourState == BehaviourState.Retreating)
            trigger = "chase";
        else if (behaviourState == BehaviourState.Spawning)
            trigger = "spawn";
        else if (behaviourState == BehaviourState.Stunned)
            trigger = "stun";
        // EDIT (boss): Downed animation (Animator needs a "downed" trigger)
        else if (behaviourState == BehaviourState.Downed)
            trigger = "downed";

        // only set the trigger when the desired animation actually changes
        if (trigger != null && trigger != lastAnimTrigger)
        {
            animator.SetTrigger(trigger);
            lastAnimTrigger = trigger;
        }

        if (behaviourState == BehaviourState.Spawning)
            animator.speed = 1f / spawnDelay;
        else if (windingUp && currentAttack.WindupDuration > 0f)
            animator.speed = 1f / currentAttack.WindupDuration;
        else
            animator.speed = 1f;
    }

    private void EnemyShot(DamageInfo info, bool wasDamaged)
    {
        if (!wasDamaged)
        {
            ImmuneHit?.Invoke(this);
            ImmuneFX(info);
        }
        else BloodSplatterFX(info);
    }

    private void ImmuneFX(DamageInfo info)
    {
        // EDIT (boss): null guard, the immune emitter is optional
        if (soundPlayer) soundPlayer.PlaySound(1);
        if (immuneFxEmitter == null) return;
        immuneFxEmitter.transform.position = info.hitPoint;
        immuneFxEmitter.transform.rotation = Quaternion.LookRotation(info.hitDirection);
        immuneFxEmitter.TriggerParticles();
    }

    private void BloodSplatterFX(DamageInfo info)
    {
        // EDIT (boss): null guard, matches the change to the EnemyShot subscription
        if (bloodFxEmitter == null) return;
        bloodFxEmitter.transform.position = info.hitPoint;
        bloodFxEmitter.transform.rotation = Quaternion.LookRotation(info.hitDirection);
        bloodFxEmitter.TriggerParticles();
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


    // Scene Gizmos
    #if UNITY_EDITOR
    // EDIT (boss): virtual so Enemy_Boss can add its own gizmos
    protected virtual void OnDrawGizmosSelected()
    {
        // aggro range (yellow)
        if (!alwaysAggro)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }

        // attack ranges (red = max, orange = min, one set per attack)
        if (attacks != null)
        {
            for (int i = 0; i < attacks.Length; i++)
            {
                if (attacks[i] == null) continue;

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, attacks[i].AttackRange);

                // EDIT (attack-priority): min range
                if (attacks[i].MinRange > 0f)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f);
                    Gizmos.DrawWireSphere(transform.position, attacks[i].MinRange);
                }
            }
        }

        // EDIT (attack-priority): strafe radius is only used by enemies with no attacks, so only draw it then (cyan)
        IEnemyMovement mov = movementScript as IEnemyMovement;
        if (mov != null && (attacks == null || attacks.Length == 0))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, mov.StrafeRadius);
        }

        // effective orbit distance during play mode (green)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, GetEffectiveOrbitDistance());
        }
    }
    #endif
}
