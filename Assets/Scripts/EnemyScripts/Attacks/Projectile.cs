// Summary:
// Self-managing projectile. Accelerates along its launch direction, deals damage to
// IDamageable targets on trigger contact, and destroys itself on hit or after its lifetime expires.
// Can optionally spawn a DamageField on the ground where it strikes.
// The projectile's collider should be set to Is Trigger on the prefab.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour, IDamageable
{
    [Header("Properties")]
    // EDIT (projectile-shot-types): replaces canBeShot. Only the ticked shot types can destroy this projectile.
    [Tooltip("Shot types that can destroy this projectile. Nothing ticked = can't be shot down (hitting it counts as wrong ammo).")]
    [SerializeField] private ShotTypeMask destroyableBy = ShotTypeMask.None;
    [SerializeField] private int damage = 8;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private float lifetime = 5f;
    [Tooltip("Layers the projectile can interact with. Leave at Everything to hit all layers.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    // EDIT (projectile-aoe): optional damage field spawned on the ground at the impact point.
    // Only spawns on an actual hit, not on expiry or when shot down.
    [Header("Damage Field On Impact")]
    [Tooltip("Spawn a DamageField on the ground where this projectile strikes.")]
    [SerializeField] private bool spawnDamageField;
    // EDIT (showif-uitk): everything below only shows while spawnDamageField is on.
    [ShowIf("spawnDamageField")]
    [SerializeField] private DamageField damageFieldPrefab;
    [ShowIf("spawnDamageField")]
    [SerializeField] private int damageFieldDamage = 5;
    [Tooltip("How long the damage field stays active.")]
    [ShowIf("spawnDamageField")]
    [SerializeField] private float damageFieldDuration = 1f;
    [ShowIf("spawnDamageField")]
    [SerializeField] private float damageFieldRadius = 1.5f;
    [ShowIf("spawnDamageField")]
    [SerializeField] private float damageFieldHeight = 1f;
    [Tooltip("Layers the damage field can damage (e.g. Player).")]
    [ShowIf("spawnDamageField")]
    [SerializeField] private LayerMask damageFieldTargetLayers;
    [Tooltip("Stays active for the full duration instead of switching off after the first hit.")]
    [ShowIf("spawnDamageField")]
    [SerializeField] private bool damageFieldPersist;
    [Tooltip("Keeps dealing damage while targets stay inside the field.")]
    [ShowIf("spawnDamageField")]
    [SerializeField] private bool damageFieldDamageOverTime;
    [ShowIf("spawnDamageField")]
    [SerializeField] private float damageFieldTickRate = 0.5f;

    // EDIT (showif-uitk): ShowIf header replaces [Header] so it hides with the fields.
    [Tooltip("Layers counted as ground when placing the damage field. Exclude the Player layer.")]
    [ShowIf("spawnDamageField", Header = "Ground Check")]
    [SerializeField] private LayerMask groundLayers = 1;
    [Tooltip("How far down to look for ground. No ground found = no damage field.")]
    [ShowIf("spawnDamageField")]
    [SerializeField] private float maxGroundCheckDistance = 10f;

    // EDIT (projectile-aoe): how far the ground ray starts back along the travel direction, so it doesn't start inside a wall or floor.
    private const float SurfaceBackOffset = 0.25f;

    private Rigidbody rb;
    private Vector3 travelDirection;
    private GameObject owner;
    private bool initialized;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    // EDIT (projectile-shot-types): true if the given shot type is allowed to destroy this projectile.
    public bool CanBeDestroyedBy(ShotTypeMask shotType) => shotType != ShotTypeMask.None && (destroyableBy & shotType) != 0;

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

    // EDIT (ranged-multi): sets owner and direction only, keeping the stats set on the prefab.
    public void Initialize(GameObject owner, Vector3 direction)
    {
        this.owner = owner;
        travelDirection = direction.normalized;
        initialized = true;
        if (debugMode) Debug.Log($"[Projectile] Initialized with prefab stats. Owner: {owner.name}, Damage: {damage}, Direction: {travelDirection}, Speed: {initialVelocity}->{maxVelocity}, Accel: {acceleration}");
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
        {
            Quaternion facing = Quaternion.LookRotation(travelDirection, Vector3.up) * Quaternion.Euler(0f, 0f, 0f);
            transform.rotation = facing;
            if (rb != null)
            {
                rb.rotation = facing;
                rb.angularVelocity = Vector3.zero;
            }
        }
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
        // skip layers the projectile shouldn't interact with
        if (hitLayers != (hitLayers | (1 << other.gameObject.layer))) return;

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
            DamageInfo info = new DamageInfo(damage, other.ClosestPoint(transform.position), travelDirection, gameObject);
            if (debugMode) Debug.Log($"[Projectile] Found IDamageable on '{damageable}' (via '{other.gameObject.name}'). Dealing {damage} damage.");
            damageable.TakeDamage(info);
        }
        else
        {
            if (debugMode) Debug.Log($"[Projectile] No IDamageable found on '{other.gameObject.name}' or any parent. Hierarchy root: '{other.transform.root.name}'");
        }

        // EDIT (projectile-aoe): spawn the damage field after the direct hit, so both apply.
        if (spawnDamageField) SpawnDamageField(damageable);

        if (debugMode) Debug.Log($"[Projectile] Destroying projectile after hitting '{other.gameObject.name}'.");
        Destroy(gameObject);
    }

    // EDIT (projectile-aoe): finds the ground below the hit and spawns the damage field on it.
    // Damageable hit (the player): ground below the target. World hit (floor, wall): ground below the impact point.
    private void SpawnDamageField(IDamageable damageable)
    {
        if (damageFieldPrefab == null)
        {
            Debug.LogWarning($"[Projectile] Spawn Damage Field is on but no prefab is assigned on {gameObject.name}.", this);
            return;
        }

        Vector3 rayStart = damageable is Component target
            ? target.transform.position + Vector3.up * SurfaceBackOffset
            : transform.position - travelDirection * SurfaceBackOffset;

        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, maxGroundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (debugMode) Debug.Log($"[Projectile] No ground found within {maxGroundCheckDistance} below {rayStart}. Damage field skipped.");
            return;
        }

        // same placement as EnemyAttack_AoE so the field sits on the ground rather than half inside it
        Vector3 spawnPos = groundHit.point + (Vector3.up * damageFieldHeight * 0.5001f);
        DamageField field = Instantiate(damageFieldPrefab, spawnPos, Quaternion.identity);
        field.DoDamageField(damageFieldDamage, damageFieldDuration, damageFieldRadius, damageFieldHeight, damageFieldTargetLayers, null, damageFieldPersist, damageFieldDamageOverTime, damageFieldTickRate);

        if (debugMode) Debug.Log($"[Projectile] Spawned damage field at {spawnPos}.");
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        // EDIT (projectile-shot-types): only destroyed by an allowed shot type.
        if (CanBeDestroyedBy(damageInfo.shotType))
        {
            if (debugMode) Debug.Log($"[Projectile] Shot down by {damageInfo.shotType}.");
            Destroy(gameObject);
        }
    }
}