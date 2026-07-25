using System.Drawing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private EnemyStagger stagger;
    [SerializeField] private EnemyKnockback knockback;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;

    public float animationSpeed = 1f;

    public bool doDebugLog = false;
    void Update()
    {
        IdleMove();
        if (stagger != null) Stagger(); //if the enemy has staggers, do stagger animations
        if (knockback != null) Knockback(); //if the enemy takes knockback, do knockback animations

        if (doDebugLog) DoDebug();
    }

    void IdleMove() //the idle/move base animation of the enemy
    {
        if (navAgent.isStopped) return;
        if (stagger.IsStaggered) return;

        if (navAgent.velocity == Vector3.zero) animator.speed = 0; //of the enemy is motionless, halt animations
        else animator.speed = animationSpeed;
    }
    void Stagger()
    {
        animator.SetBool("staggered", stagger.IsStaggered);
    }
    void Knockback()
    {
        animator.SetBool("knockedBack", navAgent.isStopped);
    }
    void DoDebug()
    {
        Debug.Log($"{this} navAgent is stopped:[{navAgent.isStopped}]");
        Debug.Log($"{this} is staggered:[{stagger.IsStaggered}]");
    }
}
