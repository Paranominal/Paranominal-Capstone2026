// Summary:
// Instantiated damage volume. Persists for a set duration, deals damage to valid targets on contact via OnTriggerEnter. Supports optional persist-after-hit (stays active for full
// duration instead of deactivating on first contact) and damage-over-time (ticks damage while targets remain inside the field). These flags are set by the attack script that spawns the DamageField.

using System.Collections;
using UnityEngine;

public class DamageField : MonoBehaviour
{
    [HideInInspector] public bool hitRegistered;
    [HideInInspector] public bool attackComplete;

    private EnemyAttack_Base attackProgenitor;
    private int damageOnHit;
    private float persistTime = 1;
    private LayerMask layerMask;

    // persist and DoT flags, set by the spawning attack script
    private bool persistAfterHit;
    private bool damageOverTime;
    private float damageTickRate;
    private float lastTickTime;
    private int lastHitFrame = -1;


    // Overloads
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
    public void DoDamageField(int damage, float damageWindow, LayerMask targetLayers, EnemyAttack_Base origin)
    {
        DoDamageField(damage, damageWindow, 1f, 1f, targetLayers, origin);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, float height, LayerMask targetLayers)
    {
        DoDamageField(damage, damageWindow, radius, height, targetLayers, null);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, LayerMask targetLayers, EnemyAttack_Base origin)
    {
        DoDamageField(damage, damageWindow, radius, 1f, targetLayers, origin);
    }
    public void DoDamageField(int damage, float damageWindow, float radius, float height, LayerMask targetLayers, EnemyAttack_Base origin)
    {
        DoDamageField(damage, damageWindow, radius, height, targetLayers, origin, false, false, 0f);
    }

    // full overload with persist and DoT options
    public void DoDamageField(int damage, float damageWindow, float radius, float height, LayerMask targetLayers, EnemyAttack_Base origin, bool persist, bool dot, float tickRate)
    {
        damageOnHit = damage;
        persistTime = damageWindow;
        transform.localScale = new Vector3(radius * 2, height, radius * 2);
        layerMask = targetLayers;
        attackProgenitor = origin;
        persistAfterHit = persist;
        damageOverTime = dot;
        damageTickRate = tickRate;
        lastTickTime = -999f;
        StartCoroutine(PersistTimer());
    }


    // DamageField Lifetime
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

    // Player Contact
    void OnTriggerEnter(Collider other)
    {
        if (!layerMask.Contains(other.gameObject.layer)) return;
        if (Time.frameCount == lastHitFrame) return; // prevent multiple hits from multiple colliders in the same frame

        lastHitFrame = Time.frameCount;
        hitRegistered = true;
        lastTickTime = Time.time; // prevent immediate DoT tick after initial hit
        DealDamage(other);

        if (!persistAfterHit) DisableDamageField();
    }

    void OnTriggerStay(Collider other)
    {
        if (!damageOverTime) return;
        if (!layerMask.Contains(other.gameObject.layer)) return;
        if (Time.time - lastTickTime < damageTickRate) return;

        lastTickTime = Time.time;
        DealDamage(other);
    }

    private void DealDamage(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        Vector3 direction = attackProgenitor != null ? attackProgenitor.transform.forward : transform.forward;
        GameObject source = attackProgenitor != null ? attackProgenitor.gameObject : gameObject;

        DamageInfo info = new DamageInfo(damageOnHit, other.transform.position, direction, source);
        damageable.TakeDamage(info);
    }
}