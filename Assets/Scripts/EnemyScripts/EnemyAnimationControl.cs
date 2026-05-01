using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAnimationControl : MonoBehaviour
{
    [SerializeField] private Animator animator;
    //[SerializeField] private EnemyBehaviourBase enemyBehaviour;
    [SerializeField] private EnemyStagger enemyStagger;
    [SerializeField] private WeakPointManager weakPointManager;
    private bool isStunned = false;

    void Update()
    {
        animator.SetBool("isStaggered", enemyStagger.IsStaggered);
        if (enemyStagger.IsStaggered) ShowWeakness();
        if (!enemyStagger.IsStaggered) HideWeakness();
    }
    public void ShowWeakness()
    {
        if (isStunned) return;
        isStunned = true;
        weakPointManager.ScanStun();
        Debug.Log("Did Scan Stun to: " + this);
    }
    public void HideWeakness()
    {
        if (!isStunned) return;
        isStunned = false;
        weakPointManager.HideCurrentWeakpoint();
        Debug.Log(this + "'s Stun wore off!");
    }
}
