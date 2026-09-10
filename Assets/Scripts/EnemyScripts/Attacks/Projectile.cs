// Summary:
// Self-managing projectile. Accelerates along its launch direction, deals damage to
// IDamageable targets on trigger contact, and destroys itself on hit or after its lifetime expires.
// The projectile's collider should be set to Is Trigger on the prefab.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private int damage = 8;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody rb;
    private Vector3 travelDirection;
    private GameObject owner;
    private bool initialized;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    public void Initialize(GameObject owner, Vector3 direction, int damage, float initialVelocity, float maxVelocity, float acceleration, float lifetime)
    {
        this.owner = owner;
        travelDirection = direction.normalized;
        this.damage = damage;
        this.initialVelocity = initialVelocity;
        this.maxVelocity = maxVelocity;
        this.acceleration = acceleration;
        this.lifetime = lifetime;
        initialized = true;
        if (debugMode) Debug.Log($"[Projectile] Initialized. Owner: {owner.name}, Damage: {damage}, Direction: {travelDirection}, Speed: {initialVelocity}->{maxVelocity}, Accel: {acceleration}");
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (!initialized || travelDirection.sqrMagnitude < 0.0001f)
        {
            travelDirection = transform.forward;
            if (debugMode) Debug.LogWarning($"[Projectile] Not initialized or no direction. Falling back to transform.forward.");
        }

        if (rb != null)
        {
            rb.linearVelocity = travelDirection * initialVelocity;
            if (debugMode) Debug.Log($"[Projectile] Start velocity: {rb.linearVelocity}, Is Trigger: {GetComponent<Collider>()?.isTrigger}, Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }

        if (travelDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float currentSpeed = rb.linearVelocity.magnitude;
        currentSpeed += acceleration * Time.fixedDeltaTime;

        if (maxVelocity > 0f)
            currentSpeed = Mathf.Min(currentSpeed, maxVelocity);

        rb.linearVelocity = travelDirection * currentSpeed;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            if (debugMode) Debug.Log($"[Projectile] Expired without hitting anything.");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugMode) Debug.Log($"[Projectile] OnTriggerEnter hit: '{other.gameObject.name}' | Layer: {LayerMask.LayerToName(other.gameObject.layer)} | Tag: {other.gameObject.tag} | IsTrigger: {other.isTrigger}", other.gameObject);

        // don't hit the enemy that fired us
        if (owner != null && (other.transform == owner.transform || other.transform.IsChildOf(owner.transform)))
        {
            if (debugMode) Debug.Log($"[Projectile] Skipped '{other.gameObject.name}' (owner or child of owner).");
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            GameObject source = owner != null ? owner : gameObject;
            DamageInfo info = new DamageInfo(damage, other.ClosestPoint(transform.position), travelDirection, source);
            if (debugMode) Debug.Log($"[Projectile] Found IDamageable on '{damageable}' (via '{other.gameObject.name}'). Dealing {damage} damage.");
            damageable.TakeDamage(info);
        }
        else
        {
            if (debugMode) Debug.Log($"[Projectile] No IDamageable found on '{other.gameObject.name}' or any parent. Hierarchy root: '{other.transform.root.name}'");
        }

        if (debugMode) Debug.Log($"[Projectile] Destroying projectile after hitting '{other.gameObject.name}'.");
        Destroy(gameObject);
    }
}