using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAttack_Melee : EnemyAttack
{
    [Header("Preferences")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private int damage = 10;
    [SerializeField] private float coolDownTime = 2f;
    [Header("Damage Field")]
    [SerializeField] private GameObject damageFieldPrefab;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float damageFieldRadius = 1f;
    [Tooltip("Number of seconds the Damage Field persists after appearing")]
    [SerializeField] private float damageWindow = 1f;
    [Header("Windup")]
    [Tooltip("Wind Up time in Seconds")]
    [SerializeField] private SpriteRenderer attackIndicator;
    [SerializeField] private float windUpTime = 1f;
    [SerializeField] private bool doInterrupt = true;

    // other variables
    [HideInInspector] public List<DamageField> damageFields;
    [HideInInspector] public bool isWindingUp;
    [HideInInspector] public bool isOnCooldown;
    [HideInInspector] public bool isAttacking;
    public void DoAttack()
    {
        if (isOnCooldown) return;
        DoWindUp();
    }
    private void DoWindUp()
    {
        StartCoroutine(WindUp());
    }
    IEnumerator WindUp()
    {
        WindUpIndicator(true);
        yield return new WaitForSeconds(windUpTime);
        DoDamageField();
        StartCoroutine(Cooldown());
        WindUpIndicator(false);
    }
    IEnumerator Cooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(coolDownTime);
        isOnCooldown = false;
    }
    private void DoDamageField()
    {
        DamageField damageField = Instantiate(damageFieldPrefab, transform.position + (transform.forward * attackDistance), transform.rotation).GetComponent<DamageField>();
        damageFields.Add(damageField);
        damageField.progenitorAttack = this;
        damageField.DoDamageField(damage, damageWindow, targetLayers, damageFieldRadius);
    }    
    private void WindUpIndicator(bool warn)
    {
        attackIndicator.enabled = warn;
    }
}
