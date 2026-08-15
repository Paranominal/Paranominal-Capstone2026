using UnityEngine;

public class DamageField : MonoBehaviour
{
    [HideInInspector] public EnemyAttack progenitorAttack;
    [HideInInspector] public LayerMask targetLayers;
    public float attackDuration;
    private bool hitRegistered;
    private bool missed;
    float timer = 0;
    void Update()
    {
        if (timer >= attackDuration) DisableField();
        else timer += Time.deltaTime;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!hitRegistered && targetLayers.Contains(other.gameObject.layer)) Debug.Log($"[{this}] Hit registered against [{other.gameObject}]!");
        hitRegistered = true;
    }
    void DisableField()
    {
        missed = true;
        gameObject.SetActive(false);
    }
    public void DoHurtbox(int damage, float damageWindow, LayerMask targetLayers)
    {
        DoHurtbox(damage, damageWindow, targetLayers, 1f);
    }
    public void DoHurtbox(int damage, float damageWindow, LayerMask targetLayers, float radius)
    {
        
    }
}
