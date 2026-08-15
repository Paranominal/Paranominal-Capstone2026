using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAttack_Melee : EnemyAttack
{
    [SerializeField] private GameObject damageFieldPrefab;
    [SerializeField] private SpriteRenderer attackIndicator;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private int damage = 10;
    [Tooltip("Wind Up time in Seconds")]
    [SerializeField] private float windUpTime = 1f;
    [HideInInspector] public bool isWindingUp;
    [HideInInspector] public bool isAttacking;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float damageFieldRadius = 1f;
    [Tooltip("Number of seconds the Damage Field persists after appearing")]
    [SerializeField] private float damageWindow = 1f;
    [SerializeField] private bool doInterrupt = true;
    public List<DamageField> damageFields;
    public void DoAttack()
    {
        DoWindUp();
    }
    float windUp;
    private void DoWindUp()
    {
        windUp = 0;
        AttackWarning(true);
        while (windUp < windUpTime)
        {
            windUp += Time.deltaTime;
        }
        DoDamageField();
        AttackWarning(false);
    }
    private void DoDamageField()
    {
        DamageField damageField = Instantiate(damageFieldPrefab, transform.position + (transform.forward * attackDistance), transform.rotation).GetComponent<DamageField>();
        damageFields.Add(damageField);
        damageField.targetLayers = targetLayers;
        damageField.progenitorAttack = this;
        damageField.DoHurtbox(damage, damageWindow, targetLayers, damageFieldRadius);
    }    
    private void AttackWarning(bool warn)
    {
        attackIndicator.enabled = warn;
    }
}
