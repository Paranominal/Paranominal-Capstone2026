using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Summary: Scene singleton (lives in SceneEssentials) that turns Special Shot results into hit feedback:
// hitstop, impact frame and camera shake. Collects the shot's results from WeaponEvents, then plays either
// the closest hit only or a chain through every target, closest first. Each hit freezes, then the impact
// frame and shake land together when the freeze ends. Chained hits overlap and fall off in strength.
// The chain is cancelled if the game pauses.
public class CameraEffectCoordinator : MonoBehaviour
{
    public static CameraEffectCoordinator Instance { get; private set; }

    [Header("References")]
    [Tooltip("Player's WeaponEvents. If null, searches the scene.")]
    [SerializeField] private WeaponEvents weaponEvents = null;
    [Tooltip("If null, searches this object then the scene.")]
    [SerializeField] private HitstopController hitstop = null;
    [Tooltip("If null, searches the scene.")]
    [SerializeField] private ImpactFrameController impactFrame = null;
    [Tooltip("Player's CameraEffects. If null, searches the scene.")]
    [SerializeField] private CameraEffects cameraEffects = null;
    [Tooltip("Scene PauseManager. If null, searches the scene.")]
    [SerializeField] private PauseManager pauseManager = null;

    [Header("Mode")]
    [Tooltip("On: effects play for every target hit, in order. Off: effects play once, on the closest hit.")]
    [SerializeField] private bool perTarget = true;
    [Tooltip("Real-time gap between one target's effects landing and the next target's hitstop.")]
    [SerializeField, Min(0f)] private float targetDelay = 0.08f;
    [Tooltip("Strength multiplier applied per target in the chain (hitstop and shake).")]
    [SerializeField, Range(0.1f, 1f)] private float falloff = 0.7f;

    [Header("Hitstop")]
    [Tooltip("Freeze length on the first hit, in real seconds. 0 = off.")]
    [SerializeField, Min(0f)] private float hitstopDuration = 0.1f;
    [Tooltip("Chained hitstops never drop below this.")]
    [SerializeField, Min(0f)] private float minHitstopDuration = 0.04f;

    [Header("Impact Frame")]
    [Tooltip("Play the impact frame when the Special Shot hits nothing.")]
    [SerializeField] private bool impactOnMiss = false;

    [Header("Camera Shake")]
    [SerializeField, Min(0f)] private float shakeIntensity = 0.8f;
    [Tooltip("Longer than the impact frame so the shake lingers after it.")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.45f;
    [Tooltip("Shake when the Special Shot hits nothing.")]
    [SerializeField] private bool shakeOnMiss = true;
    [Tooltip("Shake strength on a miss, as a multiplier of the hit intensity.")]
    [SerializeField, Range(0f, 1f)] private float missShakeScale = 0.4f;

    // results gathered between ShotFired and the end of the frame
    private readonly List<Vector3> pendingHits = new List<Vector3>();
    private Vector3 pendingMissPoint;
    private bool pendingMiss;
    private bool collecting;

    private Coroutine chain;

    private bool IsPaused => pauseManager != null && pauseManager.IsPaused;

    private void Awake()
    {
        // one per scene, destroy only this component so the rest of SceneEssentials survives
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CameraEffectCoordinator: duplicate found, removing this one.");
            Destroy(this);
            return;
        }
        Instance = this;

        if (weaponEvents == null) weaponEvents = FindAnyObjectByType<WeaponEvents>();
        if (hitstop == null) hitstop = GetComponent<HitstopController>();
        if (hitstop == null) hitstop = FindAnyObjectByType<HitstopController>();
        if (impactFrame == null) impactFrame = FindAnyObjectByType<ImpactFrameController>();
        if (cameraEffects == null) cameraEffects = FindAnyObjectByType<CameraEffects>();
        if (pauseManager == null) pauseManager = FindAnyObjectByType<PauseManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (weaponEvents == null)
            return;

        weaponEvents.ShotFired += OnShotFired;
        weaponEvents.ShotResolved += OnShotResolved;
    }

    private void OnDisable()
    {
        if (weaponEvents != null)
        {
            weaponEvents.ShotFired -= OnShotFired;
            weaponEvents.ShotResolved -= OnShotResolved;
        }

        collecting = false;
        StopChain();
    }

    private void Update()
    {
        // pause cuts the rest of the chain
        if (chain != null && IsPaused)
            StopChain();
    }

    // LateUpdate so every ShotResolved from this frame's shot is in before the chain starts
    private void LateUpdate()
    {
        if (!collecting)
            return;

        collecting = false;

        // a new shot replaces whatever is still chaining
        StopChain();

        if (pendingHits.Count > 0)
            chain = StartCoroutine(PlayChain(new List<Vector3>(pendingHits)));
        else if (pendingMiss)
            PlayMiss(pendingMissPoint);
    }

    private void OnShotFired(WeakPointType shotType)
    {
        if (shotType != WeakPointType.Special)
            return;

        pendingHits.Clear();
        pendingMiss = false;
        collecting = true;
    }

    // results arrive closest first, straight after ShotFired
    private void OnShotResolved(ShotResult result)
    {
        if (!collecting || result.ShotType != WeakPointType.Special)
            return;

        if (result.Outcome == ShotOutcome.SpecialHit)
        {
            pendingHits.Add(result.HitPoint);
        }
        else if (result.Outcome == ShotOutcome.SpecialMiss)
        {
            pendingMiss = true;
            pendingMissPoint = result.HitPoint;
        }
    }

    private IEnumerator PlayChain(List<Vector3> hits)
    {
        int count = perTarget ? hits.Count : 1;

        for (int i = 0; i < count; i++)
        {
            float strength = Mathf.Pow(falloff, i);

            // freeze first, effects land when it ends
            if (hitstop != null && hitstopDuration > 0f)
            {
                hitstop.Play(Mathf.Max(hitstopDuration * strength, minHitstopDuration));

                while (HitstopController.IsActive)
                    yield return null;
            }

            LandHit(hits[i], strength);

            // previous impact frame keeps playing through the gap and the next freeze
            if (i < count - 1)
                yield return new WaitForSecondsRealtime(targetDelay);
        }

        chain = null;
    }

    private void LandHit(Vector3 worldPosition, float strength)
    {
        if (impactFrame != null)
            impactFrame.Play(worldPosition);

        if (cameraEffects != null)
            cameraEffects.Shake(shakeIntensity * strength, shakeDuration);
    }

    // no hitstop on a miss, effects play straight away
    private void PlayMiss(Vector3 missPoint)
    {
        if (impactOnMiss && impactFrame != null)
            impactFrame.Play(missPoint);

        if (shakeOnMiss && cameraEffects != null)
            cameraEffects.Shake(shakeIntensity * missShakeScale, shakeDuration);
    }

    private void StopChain()
    {
        if (chain == null)
            return;

        StopCoroutine(chain);
        chain = null;
    }
}
