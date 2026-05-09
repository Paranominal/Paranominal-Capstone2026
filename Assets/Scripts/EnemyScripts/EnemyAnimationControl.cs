using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAnimationControl : MonoBehaviour
{
    [SerializeField] private Animator animator;
    //[SerializeField] private EnemyBehaviourBase enemyBehaviour;
    [SerializeField] private EnemyStagger enemyStagger;
    [Header("Animation Control")]
    [Tooltip("1 = 4fps")]
    [SerializeField] private float animSpeed;
    [SerializeField] private Color stunColour;
    [SerializeField] private Animation stunAnimation;

    void Update()
    {
        animator.SetBool("isStaggered", enemyStagger.IsStaggered);
    }
}
