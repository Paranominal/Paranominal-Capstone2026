using UnityEngine;

// Summary: ScriptableObject holding designer-facing parameters for a throwable item.
// Referenced by ItemDefinition (via a ThrowableDefinition field) and read at runtime
// by ThrownProjectile to configure its behaviour.
[CreateAssetMenu(fileName = "NewThrowable", menuName = "Items/Throwable Definition")]
public class ThrowableDefinition : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("The projectile prefab spawned on throw. Needs a Rigidbody and ThrownProjectile component.")]
    public GameObject projectilePrefab;

    [Header("Launch")]
    [Tooltip("Initial speed along the throw direction.")]
    public float launchSpeed = 20f;

    [Tooltip("Gravity multiplier. 0 = straight line, 1 = normal gravity, >1 = heavy arc.")]
    public float gravityScale = 1f;

    [Header("Orientation")]
    [Tooltip("VelocityAligned = faces travel direction (knives, arrows). Tumble = spins in the air (grenades, bottles). Fixed = no rotation after launch.")]
    public ThrowableOrientation orientation = ThrowableOrientation.VelocityAligned;

    [Tooltip("Spin speed in degrees/sec for Tumble mode. Ignored by other modes.")]
    public float tumbleSpeed = 720f;

    [Header("Damage")]
    public float damage = 10f;
    public float knockback = 5f;

    [Header("Lifetime")]
    [Tooltip("Seconds before the projectile auto-destroys if it hasn't hit anything.")]
    public float lifetime = 5f;
}