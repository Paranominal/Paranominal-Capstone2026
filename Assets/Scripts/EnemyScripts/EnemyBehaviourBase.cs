using UnityEngine;

public abstract class EnemyBehaviourBase : MonoBehaviour
{
    //stores the shared vision sensor reference
    [SerializeField] protected EnemyVisionSensor vision;

    //cache shared references before play starts
    protected virtual void Awake()
    {
        if (vision == null)
        {
            vision = GetComponent<EnemyVisionSensor>();
        }
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
}