using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAttack_Melee : EnemyAttack
{
    public enum AttackState { Ready, WindUp, Attacking, Cooldown }
    [HideInInspector]public AttackState attackState = AttackState.Ready;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private int damage = 10;
    [Tooltip("Time between each attack in Seconds")]
    [SerializeField] private float coolDownTime = 2f;
    [Tooltip("Wind Up time in Seconds")]
    [SerializeField] private float windUpTime = 1f;
    [SerializeField] private SpriteRenderer attackIndicator;
    [Header("Damage Field")]
    [SerializeField] private GameObject damageFieldPrefab;
    public float attackRange = 1f;
    [SerializeField] private float damageFieldRadius = 1f;
    [SerializeField] private float damageFieldHeight = 0.5f;
    [Tooltip("Number of seconds the Damage Field persists after appearing")]
    [SerializeField] private float damageWindow = 1f;
    // [SerializeField] private bool doInterrupt = true;
    [HideInInspector] public List<DamageField> damageFields;
    void Start()
    {
        SetAttackIndicator(false);
    }
    public void DoAttack()
    {
        if (attackState != AttackState.Ready) return;
        StartCoroutine(WindUp());
    }
    IEnumerator WindUp()
    {
        attackState = AttackState.WindUp;
        SetAttackIndicator(true);
        yield return new WaitForSeconds(windUpTime);
        DoDamageField();
        StartCoroutine(Cooldown());
        SetAttackIndicator(false);
    }
    IEnumerator Cooldown()
    {
        attackState = AttackState.Cooldown;
        yield return new WaitForSeconds(coolDownTime);
        attackState = AttackState.Ready;
    }
    private void DoDamageField()
    {
        DamageField damageField = Instantiate(damageFieldPrefab, transform.position + (transform.forward * attackRange) + (Vector3.up * damageFieldHeight * 0.5f), transform.rotation).GetComponent<DamageField>();
        damageFields.Add(damageField);
        damageField.DoDamageField(damage, damageWindow, damageFieldRadius, damageFieldHeight, targetLayers, this);
    }    
    private void SetAttackIndicator(bool warn)
    {
        attackIndicator.enabled = warn;
    }
}
