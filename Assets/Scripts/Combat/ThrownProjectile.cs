using UnityEngine;

// Summary: Attached to the throwable projectile prefab. Initialized by ThrowableHandler
// on spawn with values from ThrowableDefinition. Handles movement via Rigidbody,
// collision damage via DamageInfo, and lifetime cleanup.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ThrownProjectile : MonoBehaviour
{
    private Rigidbody rb;
    private float damage;
    private float knockback;
    private float lifetime;
    private float gravityScale;
    private ThrowableOrientation orientation;
    private GameObject thrower;

    // Summary: Called by ThrowableHandler immediately after instantiation.
    public void Initialize(ThrowableDefinition definition, Vector3 direction, GameObject throwerObj)
    {
        thrower = throwerObj;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        damage = definition.damage;
        knockback = definition.knockback;
        lifetime = definition.lifetime;
        gravityScale = definition.gravityScale;
        orientation = definition.orientation;

        // Launch
        rb.linearVelocity = direction.normalized * definition.launchSpeed;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Set up orientation mode
        if (orientation == ThrowableOrientation.Tumble)
        {
            Vector3 tumbleAxis = Random.onUnitSphere;
            rb.angularVelocity = tumbleAxis * (definition.tumbleSpeed * Mathf.Deg2Rad);
        }
        else if (orientation == ThrowableOrientation.Fixed)
        {
            rb.freezeRotation = true;
        }

        // Ignore collision between the projectile and the thrower
        Collider projectileCollider = GetComponent<Collider>();
        if (thrower != null && projectileCollider != null)
        {
            Collider[] throwerColliders = thrower.GetComponentsInChildren<Collider>();
            foreach (Collider c in throwerColliders)
                Physics.IgnoreCollision(projectileCollider, c);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (gravityScale != 0f)
            rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        // Align rotation to velocity each physics step
        if (orientation == ThrowableOrientation.VelocityAligned
            && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
        }
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't damage the thrower
        if (thrower != null && (collision.transform == thrower.transform
            || collision.transform.IsChildOf(thrower.transform)))
        {
            return;
        }

        // Try to deal damage
        IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Vector3 hitPoint = collision.contacts.Length > 0
                ? collision.contacts[0].point
                : transform.position;
            Vector3 hitDirection = (collision.transform.position - transform.position).normalized;

            DamageInfo info = new DamageInfo(
                Mathf.RoundToInt(damage),
                hitPoint,
                hitDirection,
                thrower,
                knockback
            );
            damageable.TakeDamage(info);
        }

        Destroy(gameObject);
    }
}