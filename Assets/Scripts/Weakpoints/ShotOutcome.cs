using UnityEngine;

public enum ShotOutcome
{
    Miss,           // did not hit a valid target
    WrongAmmo,      // hit a valid target with the wrong shot type
    EnemyHit,       // hit an enemy body or a warded weakpoint
    WeakPointHit    // correct type, landed hit
}

public static class ShotOutcomeExtensions
{
    // rewarded shots preserve ammo, heal fear and score points
    // including this so that not all of the prior bool checks have to be completely reworked
    public static bool IsRewarded(this ShotOutcome outcome) => outcome == ShotOutcome.WeakPointHit;
}
