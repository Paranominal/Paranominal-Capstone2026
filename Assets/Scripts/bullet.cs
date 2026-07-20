using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private LayerMask weakpointLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider bulletCollider;
    [SerializeField] private Rigidbody bulletRigidbody;
    public float speed = 1;
    public bool stuck;
    [SerializeField] private List<RotateEffect> rotationEffects;
    private void Start()
    {
        SetSpeed();
    }
    private void OnEnable()
    {
        SetSpeed();
    }
    private void OnDisable()
    {
        ResetBullet();
    }
    public void SetSpeed()
    {
        bulletRigidbody.AddForce(transform.forward * speed, ForceMode.VelocityChange);
    }
    void OnCollisionEnter(Collision collision)
    {
        //damage
        if (weakpointLayer.Contains(collision.gameObject.layer)) HitWeakpoint(collision.gameObject.GetComponent<WeakPoint>());
        else if (enemyLayer.Contains(collision.gameObject.layer)) HitEnemy(collision.gameObject.GetComponent<ToTough>());
        //stick
        if (!collision.gameObject.isStatic) DoStop();
    }
    void HitWeakpoint(WeakPoint weakPoint)
    {
        weakPoint.OnHit(WeakPointType.Iron);
    }
    private DamageInfo damageInfo;
    void HitEnemy(ToTough enemy)
    {
        Debug.Log($"[{this}] hit tough enemy!");
        damageInfo.amount = 1;
        damageInfo.source = gameObject;
        enemy.TakeDamage(damageInfo);
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
    void ResetBullet()
    {
        stuck = false;
        bulletRigidbody.linearVelocity = Vector3.zero;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
