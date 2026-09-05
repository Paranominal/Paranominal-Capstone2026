using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class EnemyAttack_AoE : EnemyAttack_Base
{
    [SerializeField] private DangerZone dangerZonePrefab;
    [Range(1,5)]
    [SerializeField] private float spellRadius = 2;
    public override IEnumerator WindUp(Vector3 targetPos)
    {
        attackState = AttackState.WindUp;
        SetWindupIndicator(true);
        DoDangerZone(true, targetPos);
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
        DoDangerZone(false, Vector3.zero);
    }
    public override void DoAttack(Vector3 targetPos)
    {
        currentAttack = Instantiate(DamageFieldPrefab, targetPos + (Vector3.up * DamageFieldHeight * 0.5001f), transform.rotation).GetComponent<DamageField>();
        currentAttack.DoDamageField(Damage, DamageWindow, spellRadius, DamageFieldHeight, targetLayers, this);
    }
    DangerZone currentDangerZone;
    public void DoDangerZone(bool warn, Vector3 targetPos)
    {
        if (!dangerZonePrefab) return;

        if (warn)
        {
            currentDangerZone = Instantiate(dangerZonePrefab, targetPos, quaternion.identity);
            currentDangerZone.Show(targetPos, spellRadius, WindUpTime);
        }
        else Destroy(currentDangerZone); currentDangerZone = null;
    }
}