using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class GhostBehaviour_PrototypeAttack : EnemyBehaviourBase
{
    private enum EnemyState { Chase }

    //base movement tuning
    [Header("General Movement")]
    [SerializeField] private float floatHeight = 0.6f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float turnSpeed = 120f;

    //follow movement settings
    [Header("Following")]
    [SerializeField] private float followSpeed = 3.5f;
    [SerializeField] private float keepDistance = 2.0f;

    //contact attack configuration
    [Header("Contact Attack")]
    [Tooltip("Hitbox child component. Activated permanently on spawn - the Ghost's body is the attack.")]
    [SerializeField] private Hitbox hitbox;
    [Tooltip("Damage dealt to the player on contact. Logged only until PlayerStatus is implemented.")]
    [SerializeField] private int contactDamage = 10;

    private Vector3 velocity;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
        //make the body collider a trigger so walls are ignored
        if (TryGetComponent<Collider>(out Collider col)) col.isTrigger = true;

        if (hitbox == null) hitbox = GetComponentInChildren<Hitbox>();
    }

    private void Start()
    {
        //the Ghost's body is its attack - activate the hitbox at spawn and never deactivate.
        //since the player is never in the same room as a paused enemy, the hitbox can stay
        //active for the entire life of the ghost.
        if (hitbox != null)
        {
            DamageInfo info = new DamageInfo(contactDamage, transform.position, Vector3.zero, gameObject);
            hitbox.Activate(info);
            hitbox.OnContact += HandleContact;
        }
        else
        {
            Debug.LogError($"[GhostBehaviour] No Hitbox assigned on {gameObject.name}. Ghost will not deal damage.", this);
        }
    }

    //unsubscribe from the hitbox before the base class reports death to the encounter manager
    protected override void OnDestroy()
    {
        if (hitbox != null) hitbox.OnContact -= HandleContact;
        base.OnDestroy();
    }

    //zero the velocity so the ghost doesn't drift while the player is in another room.
    //the hitbox stays active because the player is never in the same room as a paused enemy.
    protected override void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            velocity = Vector3.zero;
            StopAllCoroutines();
        }
    }

    //called when the hitbox makes contact with anything on its hitLayers - the Ghost dies on touch
    private void HandleContact(Collider other)
    {
        //future hooks: spawn death VFX, play sound, drop a pickup, etc.
        //route through Die() so the encounter manager is notified
        Die();
    }

    //update the ghost state each frame
    private void Update()
    {
        //paused or dying enemies skip all logic
        if (IsPaused || IsDying) return;
        if (!HasVisionTarget) return;

        //always chase when a vision target exists (room-based encounters)
        PerformChase();
    }

    //trail the player at a safe distance
    private void PerformChase()
    {
        Transform player = VisionTarget;
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        Vector3 followPos = player.position + (dirFromPlayer * keepDistance) + (Vector3.up * floatHeight);

        MoveTowards(followPos, followSpeed);
        LookAt(player.position);
    }

    //move using acceleration for a floaty feel
    private void MoveTowards(Vector3 target, float maxSpeed)
    {
        //ease into the desired velocity
        Vector3 toTarget = target - transform.position;
        Vector3 desiredVelocity = toTarget.normalized * maxSpeed;

        velocity = Vector3.MoveTowards(velocity, desiredVelocity, acceleration * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.01f) LookAt(transform.position + velocity);
    }

    //turn toward the movement direction
    private void LookAt(Vector3 target)
    {
        Vector3 lookDir = (target - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}
