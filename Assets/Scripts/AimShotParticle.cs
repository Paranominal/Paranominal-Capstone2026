using System;
using UnityEngine;

public class AimShotParticle : MonoBehaviour
{
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private ParticleEmitter emitter;

    void Reset()
    {
        TryGetComponent(out emitter);
    }

    void Awake()
    {
        if (playerLook == null)
        {
            Debug.LogWarning($"[{this}] No PlayerLook set for Aim Particle Emitter! this could be a mistake.");
            return;
        }

        emitter.ParticlesFired += Aim;
    }

    Vector3 targetAngle;
    void Aim()
    {
        targetAngle = Quaternion.LookRotation(playerLook.LookingAtEnvironment().point - transform.position).eulerAngles;
        transform.rotation = Quaternion.Euler(targetAngle);
    }
}
