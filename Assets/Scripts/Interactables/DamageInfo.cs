using UnityEngine;

// EDIT (projectile-shot-types): player shot types as flags, so a projectile can allow more than one.
// None means "not a player shot" (enemy attacks etc.), which keeps every existing DamageInfo call unaffected.
[System.Flags]
public enum ShotTypeMask
{
    None = 0,
    Iron = 1 << 0,
    Silver = 1 << 1,
    Special = 1 << 2
}

// EDIT (projectile-shot-types): converts the weapon's WeakPointType into a ShotTypeMask flag.
public static class ShotTypeMaskExtensions
{
    public static ShotTypeMask ToShotMask(this WeakPointType type)
    {
        switch (type)
        {
            case WeakPointType.Iron: return ShotTypeMask.Iron;
            case WeakPointType.Silver: return ShotTypeMask.Silver;
            case WeakPointType.Special: return ShotTypeMask.Special;
            default: return ShotTypeMask.None;
        }
    }
}

/// <summary>
/// Payload for a single damage event. Carries the data a damageable might
/// reasonably want to know about a hit: how much, where, from where, and
/// who caused it.
///
/// Adding fields here is cheap. Adding parameters to TakeDamage later is
/// expensive - so this struct is the place to grow the contract.
/// </summary>
public struct DamageInfo
{
    /// <summary>How much damage to apply.</summary>
    public float amount;

    /// <summary>World-space point where the hit occurred. Useful for hit FX, blood, etc.</summary>
    public Vector3 hitPoint;

    /// <summary>Normalized direction the hit came from. Useful for knockback and directional indicators.</summary>
    public Vector3 hitDirection;

    /// <summary>The GameObject that caused the damage (typically the enemy). Useful for "killed by X" tracking.</summary>
    public GameObject source;

    // EDIT (projectile-shot-types): the player shot type that caused this hit. None for anything that isn't a player shot.
    public ShotTypeMask shotType;

    public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;
        this.source = source;
        this.shotType = ShotTypeMask.None; // EDIT (projectile-shot-types)
    }

    // EDIT (projectile-shot-types): overload for player shots that need to carry their shot type.
    public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source, ShotTypeMask shotType)
        : this(amount, hitPoint, hitDirection, source)
    {
        this.shotType = shotType;
    }
}
