using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAnimationControl : MonoBehaviour
{
    [SerializeField] private Animator animator;
    //[SerializeField] private EnemyBehaviourBase enemyBehaviour;
    [SerializeField] private EnemyStagger enemyStagger;

    void Update()
    {
        animator.SetBool("isStaggered", enemyStagger.IsStaggered);
    }
}
