using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimationControl : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer enemySprite;
    [SerializeField] private EnemyStagger enemyStagger;
    [Header("Animation Control")]
    [Tooltip("1 = 4fps")]
    [SerializeField] private float animSpeed = 1;
    void Update()
    {
        SetAnimSpeed();
        StaggerSprite();
    }
    private void SetAnimSpeed()
    {
        animator.SetFloat("animSpeed", animSpeed);
    }
    private void StaggerSprite()
    {
        animator.SetBool("isStaggered", enemyStagger.IsStaggered);
    }
}
