using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DamageField : MonoBehaviour
{
    [HideInInspector] public bool hitRegistered;
    [HideInInspector] public bool attackComplete;
    private EnemyAttack attackProgenitor;
    private int damageOnHit;
    private float persistTime = 1;
    private LayerMask layerMask;
    private DamageInfo damageInfo;
    public void DoDamageField(LayerMask targetLayers)
    {
        DoDamageField(damageOnHit, persistTime, 1f, 1f, targetLayers, null);
    }
    public void DoDamageField(int damage, LayerMask targetLayers)
    {
        DoDamageField(damage, persistTime, 1f, 1f, targetLayers, null);
    }
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, 1f, 1f, targetLayers, null);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, radius, 1f, targetLayers, null);
    }
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers, EnemyAttack origin)
    {
        DoDamageField(damage, damageWindow, 1f, 1f, targetLayers, origin);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, float height, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, radius, height, targetLayers, null);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers, EnemyAttack origin)
    {
        DoDamageField(damage, damageWindow, radius, 1f, targetLayers, origin);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, float height, LayerMask targetLayers, EnemyAttack origin)
    {
        damageOnHit = damage;
        persistTime = damageWindow;
        transform.localScale = new Vector3(radius*2, height, radius*2);
        layerMask = targetLayers;
        attackProgenitor = origin;
        StartCoroutine(PersistTimer());
    }
    IEnumerator PersistTimer()
    {
        yield return new WaitForSeconds(persistTime);
        DisableDamageField();
        RemoveDamageField();
    }
    void DisableDamageField()
    {
        attackComplete = true;
        gameObject.SetActive(false);
    }
    void RemoveDamageField()
    {
        if (attackProgenitor == null) Destroy(gameObject);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!hitRegistered && layerMask.Contains(other.gameObject.layer)) Debug.Log($"[{this}] Hit registered against [{other.gameObject}]!");
        hitRegistered = true;
        damageInfo = new DamageInfo(
            damageOnHit,
            other.transform.position,
            attackProgenitor.transform.forward,
            attackProgenitor.gameObject
        );
        other.GetComponentInParent<IDamageable>().TakeDamage(damageInfo);
        DisableDamageField();
    }
}
