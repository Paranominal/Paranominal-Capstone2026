using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody projectileBody;

    [Header("Properties")]
    [SerializeField] private float baseDamage = 1.0f;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private float maxVelocity = 20.0f;
    [SerializeField] private float acceleration = 1.0f;
    [SerializeField] private float lifetime = 5.0f;

    private Vector3 travelDirection;
    private GameObject ownerEnemy;

    public void Initialize(
        GameObject owner,
        Vector3 direction,
        float baseDamage,
        float initialVelocity,
        float maxVelocity,
        float acceleration,
        float lifetime)
    {
        ownerEnemy = owner;
        travelDirection = direction.normalized;
        this.baseDamage = baseDamage;
        this.initialVelocity = initialVelocity;
        this.maxVelocity = maxVelocity;
        this.acceleration = acceleration;
        this.lifetime = lifetime;
    }

    private void Awake()
    {
        if (projectileBody == null)
        {
            projectileBody = GetComponent<Rigidbody>();
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void Start()
    {
        // Fallback in case no direction was provided by the launcher
        if (travelDirection.sqrMagnitude < 0.0001f)
        {
            if (playerTransform != null)
            {
                travelDirection = (playerTransform.position - transform.position).normalized;
            }
            else
            {
                travelDirection = transform.forward;
            }
        }

        if (projectileBody != null)
        {
            projectileBody.linearVelocity = travelDirection * initialVelocity;
        }
        else
        {
            Debug.LogWarning("Projectile: projectileBody is null. Cannot move projectile.");
        }

        // Ignore collision with the owner if both have colliders
        Collider projectileCollider = GetComponent<Collider>();

        if (ownerEnemy != null && projectileCollider != null)
        {
            Collider[] ownerColliders = ownerEnemy.GetComponentsInChildren<Collider>();

            foreach (Collider ownerCollider in ownerColliders)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCollider);
            }
        }

        // Rotate to face the travel direction
        if (travelDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
        }
    }

    private void FixedUpdate()
    {
        if (projectileBody != null)
        {
            float currentSpeed = projectileBody.linearVelocity.magnitude;

            // Accelerate along the stored launch direction, even if starting from rest
            currentSpeed += acceleration * Time.fixedDeltaTime;

            if (maxVelocity > 0f)
            {
                currentSpeed = Mathf.Min(currentSpeed, maxVelocity);
            }

            projectileBody.linearVelocity = travelDirection * currentSpeed;
        }
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (playerTransform != null && (collision.transform == playerTransform || collision.transform.IsChildOf(playerTransform)))
        {
            Debug.Log("Enemy projectile hit Player!");
            Destroy(gameObject);
            return;
        }

        Debug.Log("Enemy projectile hit something else.");
        Destroy(gameObject);
    }
}