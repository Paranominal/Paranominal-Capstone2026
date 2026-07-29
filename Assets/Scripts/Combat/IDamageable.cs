using UnityEngine;

// Summary: Implemented by anything that can take damage from a Hitbox.
// PlayerStatus will implement this once it exists; other damageables
// (destructible props, NPCs, etc.) can also implement it without any
// changes to the attack pipeline.
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}
