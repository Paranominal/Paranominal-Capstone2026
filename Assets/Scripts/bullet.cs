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
    private bool stuck = false;
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
    void DoEmbed(Transform transform)
    {
        this.transform.SetParent(transform);
        DoStop();
    }
    void HitWeakpoint(WeakPoint weakPoint)
    {
        weakPoint.OnHit(WeakPointType.Iron | WeakPointType.Silver);
    }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[{this}] hit! [{collision.gameObject}]");
        if (!collision.gameObject.isStatic) DoEmbed(collision.gameObject.transform);
        else DoStop();
        if (collision.gameObject.layer == weakpointLayer) HitWeakpoint(collision.gameObject.GetComponent<WeakPoint>());
    }

}
