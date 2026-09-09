// Summary:
// Flying/non-NavMesh enemy behaviour. Delegates movement to a serialized MonoBehaviour that implements IEnemyMovement, allowing different flight styles to be swapped in
// via the inspector. Overrides FacePlayer to use Rigidbody.MoveRotation to avoid jitter from directly setting transform.rotation on a physics-driven body.

using UnityEngine;

public class Enemy_Flying : Enemy
{
    [Header("Movement")]
    [Tooltip("Drop in any MonoBehaviour that implements IEnemyMovement.")]
    [SerializeField] private MonoBehaviour movementScript;

    private IEnemyMovement movement;
    private Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    }

    protected override void InitializeMovement()
    {
        if (movementScript != null)
            movement = movementScript as IEnemyMovement;

        if (movement == null)
            Debug.LogError($"[FlyingEnemyBehaviour] No valid IEnemyMovement script assigned on {gameObject.name}.", this);
    }

    // use MoveRotation through the Rigidbody to avoid jitter from setting transform.rotation directly
    protected override void FacePlayer()
    {
        if (playerTransform == null || rb == null) return;
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            rb.MoveRotation(Quaternion.LookRotation(dir));
    }

    protected override void DoMove(Vector3 target, float speed, float stopDistance)
    {
        if (movement == null) return;

        // disable altitude management when kamikaze chasing so the enemy flies directly at the player
        if (movement is FlyingMovement flying)
            flying.useAltitudeManagement = !ShouldUseDirectMovement;

        movement.MoveTo(target, speed, stopDistance);
    }

    protected override void DoStop()
    {
        if (movement != null) movement.Stop();
    }

    protected override bool HasReachedTarget()
    {
        if (movement == null) return true;
        return movement.HasReachedTarget;
    }

    protected override void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused && movement != null) movement.Stop();
    }

    // stop the movement script when the behaviour is disabled (e.g. during knockback)
    // so it doesn't keep driving toward its last target
    private void OnDisable()
    {
        if (movement != null) movement.Stop();
    }
}