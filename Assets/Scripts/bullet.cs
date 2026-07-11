using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class Bullet : MonoBehaviour
{
    [SerializeField] private LayerMask weakpointLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider bulletCollider;
    [SerializeField] private Rigidbody bulletRigidbody;
    public float speed = 1;
    public bool stuck;
    [SerializeField] private List<RotateEffect> rotationEffects;
    void Awake()
    {
        SetSpeed();
    }
    public void SetSpeed()
    {
        bulletRigidbody.AddForce(transform.forward * speed, ForceMode.VelocityChange);
    }
    void DoStop()
    {
        bulletRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        stuck = true;
        bulletCollider.enabled = false;
        foreach (RotateEffect rotate in rotationEffects)
        {
            rotate.enabled = false;
        }
    }
    // void DoEmbed(Transform transform) //problematic currently cause if the bullet gets deleted by a dying enemy, it goes missing from the list.
    // {
    //     this.transform.SetParent(transform);
    //     DoStop();
    // }
    void HitWeakpoint(WeakPoint weakPoint)
    {
        weakPoint.OnHit(WeakPointType.Iron);
    }
    private DamageInfo damageInfo;
    void HitEnemy(ToTough enemy)
    {
        Debug.Log($"[{this}] hit tough enemy!");
        damageInfo.amount = 1;
        damageInfo.source = this.gameObject;
        enemy.TakeDamage(damageInfo);
    }
    void OnCollisionEnter(Collision collision)
    {
        //damage
        if (collision.gameObject.layer == weakpointLayer.value) HitWeakpoint(collision.gameObject.GetComponent<WeakPoint>());
        else if (collision.gameObject.layer == enemyLayer.value) HitEnemy(collision.gameObject.GetComponent<ToTough>());
        //stick
        // if (!collision.gameObject.isStatic) DoEmbed(collision.gameObject.transform);
        // else 
        if (!collision.gameObject.isStatic) DoStop();
    }
}
