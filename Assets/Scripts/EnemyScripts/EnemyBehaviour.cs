using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyBehaviour : MonoBehaviour
{
    private enum State {Idle, Engaging, Spawning, Dying, Inactive};
    private State state = State.Inactive;

    [Header("Enemy Options")]
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool onlyEngageIfAttackReadied;
    [SerializeField] private List<EnemyAttack> attacktypes;
    
    public Transform playerTransform;
    [SerializeField] private float aggroRange;
    [SerializeField] private bool skipSpawnAnim;
    [SerializeField] private float attackCooldown;
    [Tooltip("Time in seconds it takes before the enemy will engage the Player after spawning")]
    [SerializeField] private float engageDelay;
    float engageTimer;

    private void Start()
    {
        if (skipSpawnAnim) state = State.Idle;
        else DoSpawn();
    }

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                if (CheckForPlayer()) state = State.Engaging;
                return;
            case State.Engaging:
                LookAtPlayer();
                if (chasePlayer) ChasePlayer();
                return;
            case State.Spawning:
                return;
            case State.Dying:
                return;
            case State.Inactive:
                return;
        }
    }
    void LookAtPlayer()
    {
        transform.LookAt(playerTransform);
    }
    void ChasePlayer()
    {
        return; // chase code would go here
    }
    bool CheckForPlayer()
    {
        if ((transform.position - playerTransform.position).magnitude < aggroRange) return true;
        else return false;
    }
    void DoSpawn()
    {
        state = State.Spawning;
        while (engageTimer < engageDelay)
        {
            engageTimer += Time.deltaTime;
        }
        if (CheckForPlayer()) state = State.Engaging;
        else state = State.Idle;
    }
    void DoEngageDelay()
    {
        
    }
}
