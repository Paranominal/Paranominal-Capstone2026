using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DamageField : MonoBehaviour
{
    [HideInInspector] public bool hitRegistered;
    [HideInInspector] public bool attackComplete;
    private EnemyAttack attackProgenitor;
    private int damageOnHit = 10;
    private float persistTime = 1;
    private LayerMask layerMask;
    public void DoDamageField(LayerMask targetLayers)
    {
        DoDamageField(damageOnHit, persistTime, 1f, targetLayers, 1f, null);
    }
    public void DoDamageField(int damage, LayerMask targetLayers)
    {
        DoDamageField(damage, persistTime, 1f, targetLayers, 1f, null);
    }
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, 1f, targetLayers, 1f, null);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, radius, targetLayers, 1f, null);
    }
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers, EnemyAttack origin)
    {
        DoDamageField(damage, damageWindow, 1f, targetLayers, 1f, origin);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers, float height)
    {
        DoDamageField(damage, damageWindow, radius, targetLayers, height, null);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers, EnemyAttack origin)
    {
        DoDamageField(damage, damageWindow, radius, targetLayers, 1f, origin);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers, float height, EnemyAttack origin)
    {
        transform.localScale = new Vector3(radius, height, radius);
        damageOnHit = damage;
        persistTime = damageWindow;
        layerMask = targetLayers;
        attackProgenitor = origin;
        StartCoroutine(PersistTimer());
    }
    IEnumerator PersistTimer()
    {
        yield return new WaitForSeconds(persistTime);
        DisableHurtbox();
    }
    void DisableHurtbox()
    {
        gameObject.SetActive(false);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!hitRegistered && layerMask.Contains(other.gameObject.layer)) Debug.Log($"[{this}] Hit registered against [{other.gameObject}]!");
        hitRegistered = true;
        DisableHurtbox();
    }
}
