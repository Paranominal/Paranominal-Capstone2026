using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloodSplatter : MonoBehaviour
{
    [SerializeField] ParticleSystem collisionParticle;
    [SerializeField] DecalProjector bloodSplatterPrefab;
    [Header("Splatter Size")]
    [SerializeField] float maxSplatterSize = 5;
    [SerializeField] float minSplatterSize = 2;
    private List<ParticleCollisionEvent> collisionEvents;

    void Reset()
    {
        collisionParticle = GetComponent<ParticleSystem>();
    }

    void Awake()
    {
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    public void EnemyShot(DamageInfo info)
    {
        if (collisionParticle == null)
        {
            Debug.LogWarning($"[{this}] No Blood Splatter set on ({gameObject})!! This is likely a mistake. Fix it by adding a Particle System to the Script.");
            return;
        }

        transform.position = info.hitPoint;
        transform.rotation = Quaternion.LookRotation(info.hitDirection);
        TriggerParticles();
    }

    private void TriggerParticles()
    {
        collisionParticle.Play();
    }

    private void OnParticleCollision(GameObject other)
    {
        collisionParticle.GetCollisionEvents(other, collisionEvents);

        foreach (ParticleCollisionEvent collision in collisionEvents)
        {
            DecalProjector splatter = Instantiate(
                bloodSplatterPrefab,
                collision.intersection + (collision.normal / 5),
                Quaternion.LookRotation(collision.velocity)
            );
            splatter.transform.localScale = Vector3.one * Random.Range(1, maxSplatterSize);
        }
    }
}
