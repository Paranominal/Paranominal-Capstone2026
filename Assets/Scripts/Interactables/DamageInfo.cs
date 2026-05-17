using UnityEngine;

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
    public int amount;

    /// <summary>World-space point where the hit occurred. Useful for hit FX, blood, etc.</summary>
    public Vector3 hitPoint;

    /// <summary>Normalized direction the hit came from. Useful for knockback and directional indicators.</summary>
    public Vector3 hitDirection;

    /// <summary>The GameObject that caused the damage (typically the enemy). Useful for "killed by X" tracking.</summary>
    public GameObject source;

    public DamageInfo(int amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;
        this.source = source;
    }
}
