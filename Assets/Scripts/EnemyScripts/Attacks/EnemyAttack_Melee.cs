using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAttack_Melee : EnemyAttack
{
    public enum AttackState { Ready, WindUp, Attacking, Cooldown }
    public AttackState attackState = AttackState.Ready;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private int damage = 10;
    [Tooltip("Time between each attack in Seconds")]
    [SerializeField] private float coolDownTime = 2f;
    [Tooltip("Wind Up time in Seconds")]
    public float windUpTime = 1f;
    [SerializeField] private SpriteRenderer attackIndicator;
    [Header("Damage Field")]
    [SerializeField] private GameObject damageFieldPrefab;
    public float attackRange = 1f;
    [SerializeField] private float damageFieldHeight = 0.5f;
    [Tooltip("Number of seconds the Damage Field persists after appearing")]
    [SerializeField] private float damageWindow = 1f;
    // [SerializeField] private bool doInterrupt = true;
    void Start()
    {
        SetAttackIndicator(false);
    }
    public void InitiateAttack()
    {
        if (attackState != AttackState.Ready) return;
        currentWindUpTime = windUpTime;
        StartCoroutine(WindUp());
    }
    float currentWindUpTime;
    IEnumerator WindUp()
    {
        attackState = AttackState.WindUp;
        SetAttackIndicator(true);
        // yield return new WaitForSeconds(windUpTime);
        while (currentWindUpTime > 0)
        {
            currentWindUpTime -= Time.deltaTime;
            yield return null;
        }
        DoDamageField();
        StartCoroutine(Cooldown());
        SetAttackIndicator(false);
    }
    float currentCooldownTime;
    void DoCooldown(float multiplier)
    {
        currentCooldownTime = coolDownTime * multiplier;
        StartCoroutine(Cooldown());
    }
    IEnumerator Cooldown() // additional multipler incase we want interrupts to be more impactful
    {
        attackState = AttackState.Cooldown;
        // yield return new WaitForSeconds(coolDownTime * multiplier);
        while (currentCooldownTime > 0)
        {
            currentCooldownTime -= Time.deltaTime;
            yield return null;
        }
        attackState = AttackState.Ready;
    }
    private void DoDamageField()
    {
        Instantiate(damageFieldPrefab, transform.position + (transform.forward * attackRange/2) + (Vector3.up * damageFieldHeight * 0.5001f), transform.rotation).GetComponent<DamageField>().DoDamageField(damage, damageWindow, attackRange/2, damageFieldHeight, targetLayers, this);
    }
    private void SetAttackIndicator(bool warn)
    {
        attackIndicator.enabled = warn;
    }
    bool isInterrupted;
    public bool IsInterrupted => isInterrupted; 
    public void InterruptAttack()
    {
        StopCoroutine(WindUp());
        DoCooldown(2);
        SetAttackIndicator(false);
    }
}
