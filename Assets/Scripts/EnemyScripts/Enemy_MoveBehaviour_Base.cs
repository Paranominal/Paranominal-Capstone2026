using UnityEngine;

public abstract class Enemy_MoveBehaviour_Base : MonoBehaviour
{
    public enum ChaseState { Chasing, Strafing, Retreating, Returning, Inactive }
    private ChaseState state = ChaseState.Chasing;
    public ChaseState State => state;

    [HideInInspector] public Transform target;
    [HideInInspector] public bool chaseForbidden; // other scripts can tell this script to not chase for one reason or another. 

    // [SerializeField] private bool chasePlayer;
    [SerializeField] private float chaseSpeed = 5f;
    [Tooltip("How close the enemy stops to the player. Overridden by the shortest attack range if attacks are assigned.")]
    public float chaseStopDistance = 2.5f;
    [SerializeField] private bool onlyChaseIfAttackReady;
    [SerializeField] private bool neverGiveUpChase;

    public float ChaseSpeed => chaseSpeed;
    public bool OnlyChaseIfAttackReady => onlyChaseIfAttackReady;
    public bool NeverGiveUpChase => neverGiveUpChase;

    [Header("Keep to Spawn Location")]
    [SerializeField] private bool keepToSpawnRadius;
    [Tooltip("Max distance the enemy will chase from its spawn point. Ignored if neverGiveUpChase is on.")]
    [SerializeField] private float chaseTerritory = 20f;
    [Tooltip("If enabled, the enemy walks back to its spawn point after losing aggro instead of idling in place.")]
    [SerializeField] private bool returnToOrigin = true;
    [SerializeField] private float returnSpeed = 3f;
    private Vector3 spawnPosition;

    public bool KeepToSpawnRadius => keepToSpawnRadius;
    public float ChaseTerritory => chaseTerritory;
    public bool ReturnToOrigin => returnToOrigin;
    public float ReturnSpeed => returnSpeed;


    
    // Movement
    public abstract void InitializeMovement();
    public abstract void DoMove(Vector3 target, float stopDistance);
    public abstract void DoMove(Vector3 target, float speed, float stopDistance);
    public abstract void DoStop();
    public abstract bool HasReachedTarget();

    

    [Header("Retreat")]
    [Tooltip("If enabled, the enemy backs away from the player after finishing an attack.")]
    [SerializeField] private bool retreatAfterAttack;
    [SerializeField] private float retreatDistance = 5f;
    [SerializeField] private float retreatSpeed = 4f;

    public bool RetreatAfterAttack => retreatAfterAttack;
    private Vector3 retreatTarget;

    [Header("Strafe")]
    [Tooltip("If enabled, the enemy orbits the player while waiting for attack cooldown instead of standing still.")]
    [SerializeField] private bool strafeWhileWaiting;
    [SerializeField] private float strafeSpeed = 3f;
    [Tooltip("How often the enemy changes strafe direction in seconds.")]
    [SerializeField] private float strafeDirectionInterval = 2f;
    public bool StrafeWhileWaiting => strafeWhileWaiting;
    private float strafeDirection = 1f;
    private float strafeTimer;

    void Awake()
    {
        spawnPosition = transform.position;
        InitializeMovement();
    }

    void Update()
    {
        if (state == ChaseState.Strafing) StrafeState();
        
    }

    void StrafeState()
    {
        if (strafeWhileWaiting)
        {
            UpdateStrafe();

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

    // override in subclasses to check for obstacles between the enemy and the strafe target
    protected virtual bool IsStrafeClear(Vector3 target)
    {
        return true;
    }

    private Vector3 ComputeStrafeTarget(float direction)
    {
        if (target == null) return transform.position;

        Vector3 toEnemy = transform.position - target.position;
        toEnemy.y = 0f;
        float radius = chaseStopDistance;
        if (radius < 0.1f) return transform.position;

        Vector3 lateral = Vector3.Cross(Vector3.up, toEnemy.normalized) * direction;
        Vector3 aheadOnArc = transform.position + lateral * 2f;
        Vector3 fromPlayer = aheadOnArc - target.position;
        fromPlayer.y = 0f;

        return target.position + fromPlayer.normalized * radius;
    }


    public void EnterRetreat()
    {
        state = ChaseState.Retreating;
        Vector3 awayFromPlayer = (transform.position - target.position);
        awayFromPlayer.y = 0f;
        if (awayFromPlayer.sqrMagnitude < 0.001f) awayFromPlayer = -transform.forward;
        retreatTarget = transform.position + awayFromPlayer.normalized * retreatDistance;
    }

    private void ExitChase()
    {
        DoStop();
        if (returnToOrigin) state = ChaseState.Returning;
        else state = ChaseState.Inactive;
    }

    public void CheckOrigin()
    {
        // chase leash
        if (!NeverGiveUpChase)
        {
            float distFromSpawn = (transform.position - spawnPosition).magnitude;
            if (chaseForbidden || distFromSpawn > ChaseTerritory)
            {
                ExitChase();
                return;
            }
        }
    }
}
