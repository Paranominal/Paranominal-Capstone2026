using UnityEngine;

public struct ShotResult
{
    public WeakPointType ShotType;
    public ShotOutcome Outcome;
    public float Accuracy;    // 0-1, only meaningful on weakpoint hits
    public Vector3 HitPoint;  // world position, for point popups
    public Vector3 OwnerCentre;
}
