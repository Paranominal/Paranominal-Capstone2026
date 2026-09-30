using System.Collections.Generic;

public enum ComboEffect { Increment, Neutral, Break }

public readonly struct OutcomeRules
{
    public readonly ComboEffect Combo;
    public readonly bool AwardsPoints;
    public readonly bool RetainsAmmo;
    public readonly bool ShowsMissPopup;

    public OutcomeRules(ComboEffect combo, bool awardsPoints, bool retainsAmmo, bool showsMissPopup)
    {
        Combo = combo;
        AwardsPoints = awardsPoints;
        RetainsAmmo = retainsAmmo;
        ShowsMissPopup = showsMissPopup;
    }
}

public enum ShotOutcome
{
    Miss,                // did not hit a valid target
    WrongAmmo,           // hit a valid target with the wrong shot type
    EnemyHit,            // hit an unstaggered enemy body or a warded weakpoint
    EnemyHitStaggered,   // hit a staggered enemy body
    ShootableTargetHit,  // hit a destructible/trigger
    WeakPointHit         // correct type, landed hit
}

public static class ShotOutcomeExtensions
{
    // AND i had to use a DICTIONARY EK i hope youre HAPPY
    // ive been thinking about how to do this ALL FUCKING DAY
    // WHO ACTUALLY USES DICTIONARIES???
    // ME APPARENTLY
    private static readonly Dictionary<ShotOutcome, OutcomeRules> rules = new Dictionary<ShotOutcome, OutcomeRules>
    {                                                   // combo                  points  keep ammo  miss popup
        { ShotOutcome.Miss,               new OutcomeRules(ComboEffect.Break,     false,  false,  true ) },
        { ShotOutcome.WrongAmmo,          new OutcomeRules(ComboEffect.Break,     false,  false,  false) },
        { ShotOutcome.EnemyHit,           new OutcomeRules(ComboEffect.Neutral,   false,  true,   false) },
        { ShotOutcome.EnemyHitStaggered,  new OutcomeRules(ComboEffect.Break,     false,  false,  false) },
        { ShotOutcome.ShootableTargetHit, new OutcomeRules(ComboEffect.Neutral,   false,  true,   false) },
        { ShotOutcome.WeakPointHit,       new OutcomeRules(ComboEffect.Increment, true,   true,   false) },
    };

    public static OutcomeRules Rules(this ShotOutcome outcome) => rules[outcome];

    // i'm keeping these as wrappers because i don't want to have to change everything againnnn
    public static bool IsRewarded(this ShotOutcome outcome) => rules[outcome].AwardsPoints;
    public static bool RetainsAmmo(this ShotOutcome outcome) => rules[outcome].RetainsAmmo;
}