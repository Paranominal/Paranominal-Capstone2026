using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ParticleEmitter : MonoBehaviour
{
    // Note Limitations: this script only listens to collisions 
    // called by the primary emitter, and not any sub-emitters.

    [Tooltip("The Primary particle emitter")]
    [SerializeField] ParticleSystem particle;
    [Header("Decals")]
    [SerializeField] DecalProjector decalPrefab;
    [Header("Decal Size")]
    [SerializeField] float maxDecalSize = 5;
    [SerializeField] float minDecalSize = 1;
    private List<ParticleCollisionEvent> collisionEvents;
    public event Action ParticlesFired;

    void Reset()
    {
        particle = GetComponent<ParticleSystem>();
    }

    void Awake()
    {
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    // public void EnemyShot(DamageInfo info)
    // {
    //     if (particle == null)
    //     {
    //         Debug.LogWarning($"[{this}] No Blood Splatter set on ({gameObject})!! This is likely a mistake. Fix it by adding a Particle System to the Script.");
    //         return;
    //     }

    //     transform.position = info.hitPoint;
    //     transform.rotation = Quaternion.LookRotation(info.hitDirection);
    //     TriggerParticles();
    // }

    public void TriggerParticles()
    {
        particle.Play();
        ParticlesFired?.Invoke();
    }

    private void OnParticleCollision(GameObject other)
    {
        particle.GetCollisionEvents(other, collisionEvents);

        foreach (ParticleCollisionEvent collision in collisionEvents)
        {
            DecalProjector splatter = Instantiate(
                decalPrefab,
                collision.intersection + (collision.normal / 5),
                Quaternion.LookRotation(collision.velocity)
            );
            splatter.transform.localScale = Vector3.one * UnityEngine.Random.Range(minDecalSize, maxDecalSize);
        }
    }
}
