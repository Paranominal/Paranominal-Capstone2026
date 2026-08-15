using Unity.VisualScripting;
using UnityEngine;

public class DamageField : MonoBehaviour
{
    [HideInInspector] public EnemyAttack progenitorAttack;
    [HideInInspector] public bool hitRegistered;
    [HideInInspector] public bool attackComplete;
    private float attackDuration = 1;
    private int damageOnHit;
    private float persistTime;
    private LayerMask layerMask;

    void Update()
    {
        if (isActiveAndEnabled) DoTimer();
    }
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, targetLayers, 1f);
    }
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers, float radius)
    {
        transform.localScale = Vector3.one * radius;
        damageOnHit = damage;
        persistTime = damageWindow;
        layerMask = targetLayers;
    }
    void DisableHurtbox()
    {
        gameObject.SetActive(false);
        //Destroy(gameObject);
    }
    float timer = 0;
    void DoTimer()
    {
        if (timer >= attackDuration) DisableHurtbox();
        else timer += Time.deltaTime;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!hitRegistered && layerMask.Contains(other.gameObject.layer)) Debug.Log($"[{this}] Hit registered against [{other.gameObject}]!");
        hitRegistered = true;
        DisableHurtbox();
    }
}
