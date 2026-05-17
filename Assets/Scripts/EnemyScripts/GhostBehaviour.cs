using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class GhostBehaviour : EnemyBehaviourBase
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

    //current ai state
    private EnemyState currentState = EnemyState.Chase;
    private Vector3 velocity;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();

        //make the collider a trigger so walls are ignored
        if (TryGetComponent<Collider>(out Collider col)) col.isTrigger = true;
    }

    //update the ghost state each frame
    private void Update()
    {
        if (!HasVisionTarget) return;

        //run the active state
        RunCurrentState();
    }

    //call the current state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Chase:
                PerformChase();
                break;
        }
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