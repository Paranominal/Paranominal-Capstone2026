using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private enum State {Idle, Engaging, Spawning, Dying, Inactive};
    private State state = State.Inactive;

    [Header("Enemy Options")]
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool onlyEngageIfAttackReadied;
    [SerializeField] private EnemyAttack_Melee attack;
    
    public Transform playerTransform;
    [SerializeField] private float aggroRange = 10;
    [SerializeField] private float attackRange = 3;
    [SerializeField] private bool skipSpawnAnim;
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
        Debug.Log($"[{this}] State: [{state}]");
        switch (state)
        {
            case State.Idle:
                if (PlayerInAggroRange()) state = State.Engaging;
                return;
            case State.Engaging:
                LookAtPlayer();
                if (chasePlayer) ChasePlayer();
                Debug.Log($"[{this}] PlayerInAttackRange: [{PlayerInAttackRange()}] | attack: [{attack}]");
                if (PlayerInAttackRange() && attack != null) DoAttack();
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
    bool PlayerInAggroRange()
    {
        if ((transform.position - playerTransform.position).magnitude < aggroRange) return true;
        else return false;
    }
    bool PlayerInAttackRange()
    {
        if ((transform.position - playerTransform.position).magnitude < attackRange) return true;
        else return false;
    }
    void DoSpawn()
    {
        StartCoroutine(Spawn());
    }
    IEnumerator Spawn()
    {
        Debug.Log($"[{this}] Spawning...");
        state = State.Spawning;
        yield return new WaitForSeconds(engageDelay);
        
        if (PlayerInAggroRange()) state = State.Engaging;
        else state = State.Idle;
        Debug.Log($"[{this}] Spawning complete");
        yield break;
    }
    void DoAttack()
    {
        if (attack.isOnCooldown) return;
        Debug.Log($"[{this}] Doing attack: [{attack}]");
        attack.DoAttack();
    }
}
