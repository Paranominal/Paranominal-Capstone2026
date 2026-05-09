using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyVisionSensor))]
public class EyeBatBehaviour : EnemyBehaviourBase
{
    private enum State { Following, Swooping }

    //tunes hover height and turning speed
    [Header("General Movement")]
    [SerializeField] private float hoverHeight = 2.5f;
    [SerializeField] private float turnSpeed = 180f;

    //tunes chase distance and speed
    [Header("Following")]
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float keepDistance = 4f;

    //tunes the dive attack
    [Header("Swooping")]
    [SerializeField] private float swoopCooldown = 4f;
    [SerializeField] private float swoopSpeed = 10f;

    private State currentState = State.Following;
    private float nextSwoopTime;

    //cache references before play starts
    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (!HasVisionTarget) return;

        //run the active state logic
        RunCurrentState();
    }

    //dispatch the current state behavior
    private void RunCurrentState()
    {
        switch (currentState)
        {
            case State.Following:
                FollowLogic();
                CheckForSwoop();
                break;
            case State.Swooping:
                break;
        }
    }

    //move toward the player without crowding
    private void FollowLogic()
    {
        //stay a little back from the player
        Transform player = VisionTarget;
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        dirFromPlayer.y = 0;

        Vector3 followPos = player.position + (dirFromPlayer * keepDistance) + (Vector3.up * hoverHeight);
        MoveTowards(followPos, followSpeed);
        LookAt(player.position);
    }

    //start a dive when sight is active
    private void CheckForSwoop()
    {
        if (Time.time >= nextSwoopTime && SensorHasVision())
            StartCoroutine(SwoopRoutine());
    }

    //move with the rigidbody so collisions still matter
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 moveDir = (target - transform.position).normalized;
        Vector3 newPos = transform.position + moveDir * speed * Time.deltaTime;

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.MovePosition(newPos);
        }

        LookAt(target);
    }

    //turn smoothly toward the movement target
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

    //dash at the player, then return to start
    private IEnumerator SwoopRoutine()
    {
        currentState = State.Swooping;
        Vector3 startPos = transform.position;
        float elapsed = 0;
        Transform player = VisionTarget;
        Rigidbody rb = GetComponent<Rigidbody>();

        while (elapsed < 0.6f && player != null)
        {
            Vector3 targetPos = player.position + Vector3.up * 0.5f;
            Vector3 moveDir = (targetPos - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * swoopSpeed * Time.deltaTime;

            if (rb != null)
                rb.MovePosition(newPos);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rb = GetComponent<Rigidbody>();
        while (Vector3.Distance(transform.position, startPos) > 0.1f)
        {
            Vector3 moveDir = (startPos - transform.position).normalized;
            Vector3 newPos = transform.position + moveDir * swoopSpeed * 0.5f * Time.deltaTime;

            if (rb != null)
                rb.MovePosition(newPos);

            yield return null;
        }

        nextSwoopTime = Time.time + swoopCooldown;
        currentState = State.Following;
    }
}