using UnityEngine;

public enum ShotOutcome
{
    Miss,                // did not hit a valid target
    WrongAmmo,           // hit a valid target with the wrong shot type
    WeakPointHit,        // correct type, landed hit
    EnemyHit,            // hit an unstaggered enemy body or a warded weakpoint
    EnemyHitStaggered,   // hit a staggered enemy body
    ShootableTargetHit   // hit a destructible/trigger
}

public static class ShotOutcomeExtensions
{
    // rewarded shots heal fear and score points but ammo retention now separate
    // including this so that not all of the prior bool checks have to be completely reworked
    public static bool IsRewarded(this ShotOutcome outcome) => outcome == ShotOutcome.WeakPointHit;

    // this is ammo retention so that unstaggered enemy hits keep the bullet without scoring
    public static bool RetainsAmmo(this ShotOutcome outcome) =>
        outcome == ShotOutcome.WeakPointHit
        || outcome == ShotOutcome.EnemyHit
        || outcome == ShotOutcome.ShootableTargetHit;
}
