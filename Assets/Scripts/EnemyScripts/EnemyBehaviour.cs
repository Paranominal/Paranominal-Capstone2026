using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private enum EnemyState {Idle, Engaging, Spawning, Dying, Inactive};
    private EnemyState attackState = EnemyState.Inactive;

    [Header("Enemy Options")]
    [SerializeField] private bool alwaysAggro;
    [SerializeField] private bool chasePlayer;
    [SerializeField] private bool onlyEngageIfAttackReadied;
    [SerializeField] private EnemyAttack_Melee attack;
    
    public Transform playerTransform;
    [SerializeField] private float aggroRange = 10;
    // [SerializeField] private float attackRange = 3;
    [SerializeField] private bool skipSpawn;
    [Tooltip("Time in seconds it takes the enemy to spawn")]
    [SerializeField] private float spawnDelay = 3;
    [Header("Debugging")]
    bool debugMode;
    // [Tooltip("Time in seconds it takes the enemy to engage the Player after getting aggro'd")]
    // [SerializeField] private float engageDelay = 1;

    private void Start()
    {
        if (skipSpawn) attackState = EnemyState.Idle;
        else DoSpawn();
    }

    void Update()
    {
        if (debugMode) Debug.Log($"[{this}] EnemyState: [{attackState}]");
        switch (attackState)
        {
            case EnemyState.Idle:
                if (PlayerInAggroRange()) attackState = EnemyState.Engaging;
                return;
            case EnemyState.Engaging:
                LookAtPlayer();
                if (chasePlayer) ChasePlayer();
                if (debugMode) Debug.Log($"[{this}] PlayerInAttackRange: [{PlayerInAttackRange()}] | attack: [{attack}]");
                if (PlayerInAttackRange() && attack != null) DoAttack();
                return;
            case EnemyState.Spawning:
                return;
            case EnemyState.Dying:
                return;
            case EnemyState.Inactive:
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
        if ((transform.position - playerTransform.position).magnitude < attack.attackRange) return true;
        else return false;
    }
    void DoSpawn()
    {
        StartCoroutine(Spawn());
    }
    IEnumerator Spawn()
    {
        if (debugMode) Debug.Log($"[{this}] Spawning...");
        attackState = EnemyState.Spawning;

            Color color = Color.black;
        GetComponentInChildren<SpriteRenderer>().color = color;

        yield return new WaitForSeconds(spawnDelay);
        
            color = Color.white;
        GetComponentInChildren<SpriteRenderer>().color = color;

        if (PlayerInAggroRange()) attackState = EnemyState.Engaging;
        else attackState = EnemyState.Idle;
        if (debugMode) Debug.Log($"[{this}] Spawning complete");
        yield break;
    }
    void DoAttack()
    {
        if (attack.attackState == EnemyAttack_Melee.AttackState.Ready)
        {
        attack.InitiateAttack();
        if (debugMode) Debug.Log($"[{this}] Doing attack: [{attack}]");
        }
    }
}
