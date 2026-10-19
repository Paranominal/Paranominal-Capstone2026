using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ParticleEmitter : MonoBehaviour
{
    // Note Limitations: this script only listens to collisions 
    // called by the primary emitter, and not any sub-emitters.

    [Header("References")]
    [Tooltip("The Primary particle emitter")]
    [SerializeField] private ParticleSystem particle;
    [Header("Options")]
    [SerializeField] private bool loopOnAwake;
    [Header("Decals")]
    [SerializeField] private DecalProjector decalPrefab;
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
        if (loopOnAwake) LoopParticles(true);
    }

    public void TriggerParticles()
    {
        particle.Play();
        ParticlesFired?.Invoke();
    }

    public void LoopParticles(bool set)
    {
        if (set) particle.Play();
        else particle.Stop();
    }

    private void OnParticleCollision(GameObject other)
    {
        particle.GetCollisionEvents(other, collisionEvents);

        // check for decal
        if (!decalPrefab)
        {
            Debug.LogWarning($"[{gameObject}, {this}] No Decal Set! You can probably turn Collision Messages off on [{particle}]");
            return;
        }

        Debug.Log($"Created {collisionEvents.Count} splats");
        foreach (ParticleCollisionEvent collision in collisionEvents)
        {
            DecalProjector splatter = Instantiate(
                decalPrefab,
                collision.intersection + (collision.normal / 5),
                Quaternion.LookRotation(collision.velocity)
            );
            splatter.transform.localScale = Vector3.one * UnityEngine.Random.Range(minDecalSize, maxDecalSize);
            DecalPool.RegisterDecal(splatter);
        }
    }
}
