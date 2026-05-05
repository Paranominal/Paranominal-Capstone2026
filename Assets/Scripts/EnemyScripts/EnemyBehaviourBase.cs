using UnityEngine;

public abstract class EnemyBehaviourBase : MonoBehaviour
{
    //stores the shared vision sensor reference
    [SerializeField] protected EnemyVisionSensor vision;

    //tracks whether this enemy is currently paused by an encounter manager
    public bool IsPaused { get; private set; }

    //tracks whether this enemy has finished dying so behaviour subclasses can early-out
    public bool IsDying { get; private set; }

    //the encounter manager that spawned this enemy, if any
    private EnemyEncounterManager ownerSpawner;
    private bool isCreatedBySpawner;
    private bool hasReportedDeathToSpawner;

    //cache shared references before play starts
    protected virtual void Awake()
    {
        if (vision == null)
        {
            vision = GetComponent<EnemyVisionSensor>();
        }
    }

    //reports this enemy's death if it was spawned by an encounter manager
    protected virtual void OnDestroy()
    {
        ReportDeathToSpawner();
    }

    //check whether the sensor has a valid target
    protected bool HasVisionTarget => vision != null && vision.HasTarget;

    //expose the current sensor target
    protected Transform VisionTarget => vision != null ? vision.Target : null;

    //check if the target is currently visible
    protected bool SensorHasVision()
    {
        return vision != null && vision.IsTargetInVision();
    }

    //check if visibility has lasted long enough
    protected bool SensorHasVisionForDuration(float requiredDuration)
    {
        return vision != null && vision.IsTargetInVisionForDuration(requiredDuration);
    }

    //check if the target is seen or close enough
    protected bool SensorDetectsTarget()
    {
        return vision != null && vision.IsTargetDetected();
    }

    //stores a reference to the encounter manager that created this enemy
    public void SetOwnerSpawner(EnemyEncounterManager spawner)
    {
        ownerSpawner = spawner;
        isCreatedBySpawner = spawner != null;
    }

    //sets whether this enemy is currently paused, ignored if it is already dying
    public void SetPaused(bool isPaused)
    {
        if (IsDying)
        {
            return;
        }

        if (IsPaused == isPaused)
        {
            return;
        }

        IsPaused = isPaused;
        OnPauseStateChanged(isPaused);
    }

    //subclass hook for pause state changes, override to stop nav agents, animations, coroutines, etc.
    protected virtual void OnPauseStateChanged(bool isPaused)
    {
    }

    //handles the enemy's death and cleanup, can be called by subclasses when their health hits zero
    public virtual void Die()
    {
        if (IsDying)
        {
            return;
        }

        IsDying = true;

        OnDying();
        ReportDeathToSpawner();

        //subclasses can override OnDying to play a death animation before this destroy runs
        Destroy(gameObject);
    }

    //subclass hook for death effects (animation, sfx, drops, disabling colliders), no need to call base
    protected virtual void OnDying()
    {
    }

    //notifies the owning encounter manager that this enemy has died, guarded so it only fires once
    private void ReportDeathToSpawner()
    {
        if (hasReportedDeathToSpawner)
        {
            return;
        }

        if (!isCreatedBySpawner || ownerSpawner == null)
        {
            return;
        }

        hasReportedDeathToSpawner = true;
        ownerSpawner.NotifyEnemyDeath(this);
    }
}
