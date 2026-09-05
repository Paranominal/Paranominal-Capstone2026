using System.Collections;
using UnityEngine;

public class EnemyAttack_Melee : EnemyAttack_Base
{
    [SerializeField] private SpriteRenderer attackIndicator;

    void Awake()
    {
        SetAttackIndicator(false);
        SetWindupIndicator(false);
    }
    public override void DoAttack(Vector3 targetPos)
    {
        currentAttack = Instantiate(DamageFieldPrefab, transform.position + (transform.forward * AttackRange / 2) + (Vector3.up * DamageFieldHeight * 0.5001f), transform.rotation).GetComponent<DamageField>();
        currentAttack.DoDamageField(Damage, DamageWindow, AttackRange/2, DamageFieldHeight, targetLayers, this);
    }
    public override IEnumerator WindUp(Vector3 targetPos)
    {
        attackState = AttackState.WindUp;
        SetWindupIndicator(true);
        SetAttackIndicator(true);
        // yield return new WaitForSeconds(windUpTime);
        while (currentWindUpTime > 0)
        {
            currentWindUpTime -= Time.deltaTime;
            yield return null;
        }
        DoAttack(targetPos);
        if (currentAttack != null && !currentAttack.attackComplete && !currentAttack.hitRegistered) StartCoroutine(WaitForAttackEnd());
        else DoCooldown();
        SetWindupIndicator(false);
        SetAttackIndicator(false);
    }
    public virtual void SetAttackIndicator(bool warn)
    {
        if (attackIndicator) attackIndicator.enabled = warn;
    }
    public override void InterruptAttack()
    {
        StopCoroutine(WindUp(Vector3.zero));
        DoCooldown(2);
        SetWindupIndicator(false);
        SetAttackIndicator(false);
    }
}