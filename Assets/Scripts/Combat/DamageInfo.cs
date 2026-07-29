using UnityEngine;

// Summary: Payload for a single damage event. Carries the data a damageable might
// reasonably want to know about a hit: how much, where, from where, and who caused it.
// Adding fields here is cheap. Adding parameters to TakeDamage later is expensive,
// so this struct is the place to grow the contract.
public struct DamageInfo
{
    // How much damage to apply.
    public int amount;

    // World-space point where the hit occurred. Useful for hit FX, blood, etc.
    public Vector3 hitPoint;

    // Normalized direction the hit came from. Useful for knockback and directional indicators.
    public Vector3 hitDirection;

    // The GameObject that caused the damage (typically the enemy). Useful for "killed by X" tracking.
    public GameObject source;

    // EDIT (weapon system): Force applied for knockback. Consumed by EnemyKnockback,
    // scaled by the receiver's knockbackResistance.
    public float knockbackForce;

    public DamageInfo(int amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source, float knockbackForce = 0f)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;
        this.source = source;
        this.knockbackForce = knockbackForce;
    }
}
