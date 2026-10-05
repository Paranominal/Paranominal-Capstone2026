using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// full screen noisy vignette transition that closes in from the edges and reopens in reverse
// persists across scenes and can be called from anywhere
public class TransitionManager : MonoBehaviour
{
    // global access point
    public static TransitionManager Instance { get; private set; }

    [Header("Look")]
    [SerializeField] private Shader transitionShader;               // assign NoiseVignetteTransition
    [SerializeField] private Color color = Color.black;             // colour of the covering mask
    [SerializeField, Range(0.01f, 1f)] private float softness = 0.15f;       // how soft the edge is
    [SerializeField, Range(0f, 1f)] private float noiseIntensity = 0.5f;     // how ragged the edge is
    [SerializeField, Range(1f, 20f)] private float noiseScale = 6f;          // size of the noise shapes
    [SerializeField] private float noiseSpeed = 0.3f;               // how fast the noise creeps inward
    [SerializeField] private float cycleDuration = 10f;             // time before the noise layers reset
    [SerializeField] private bool randomizeEachTime = true;         // new pattern every transition in

    [Header("Timing")]
    [SerializeField] private float duration = 0.9f;                 // seconds for a full transition
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);   // speed shaping
    [SerializeField] private int sortingOrder = 1000;               // canvas draw order

    // cached shader property ids are faster than using strings every frame
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int CycleId = Shader.PropertyToID("_CycleDuration");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int TimeId = Shader.PropertyToID("_ManualTime");

    private Canvas canvas;      // overlay canvas holding the mask
    private Material material;  // runtime copy of the shader material
    private float progress;     // zero is fully revealed and one is fully covered
    private int version;        // lets a newer transition cancel an older one

    // true once the screen is completely hidden
    public bool IsCovered => progress >= 1f;

    private void Awake()
    {
        // destroy duplicates so only one manager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        if (material != null) SetProgress(0f);   // start fully revealed
    }

    private void OnDestroy()
    {
        // only clear the instance if it is this one
        if (Instance == this) Instance = null;

        // clean up the runtime material
        if (material != null) Destroy(material);
    }

    private void Update()
    {
        // unscaled time keeps the noise moving while the game is paused
        if (material != null && canvas.enabled)
            material.SetFloat(TimeId, Time.unscaledTime);
    }

    // screen closes in from the edges until fully covered
    public IEnumerator TransitionIn()
    {
        // skip safely if setup failed
        if (material == null) return Empty();

        // pick a new pattern only when starting from fully revealed
        if (randomizeEachTime && progress <= 0f)
            material.SetFloat(SeedId, UnityEngine.Random.Range(0f, 100f));

        return MoveTo(1f);
    }

    // screen reopens from the centre outward as the exact reverse
    public IEnumerator TransitionOut() => material == null ? Empty() : MoveTo(0f);

    // cover then run an action while hidden then reveal
    public IEnumerator Transition(Action whileCovered)
    {
        yield return TransitionIn();
        whileCovered?.Invoke();
        yield return TransitionOut();
    }


    // coroutine that finishes instantly
    private static IEnumerator Empty() { yield break; }

    // animates progress toward the target value
    private IEnumerator MoveTo(float target)
    {
        // claim this run so older ones stop themselves
        int id = ++version;
        float start = progress;
        float distance = Mathf.Abs(target - start);
        if (distance <= 0f) yield break;

        // partial moves take proportionally less time
        float time = duration * distance;
        float elapsed = 0f;

        while (elapsed < time)
        {
            if (id != version) yield break;   // a newer transition took over

            // clamp the frame time so a lag spike cannot skip the animation
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            SetProgress(Mathf.Lerp(start, target, elapsed / time));
            yield return null;
        }

        // snap to the exact end value
        if (id == version) SetProgress(target);
    }

    // applies progress to the shader
    private void SetProgress(float p)
    {
        progress = Mathf.Clamp01(p);
        canvas.enabled = progress > 0f;   // hide the canvas when fully revealed
        material.SetFloat(ProgressId, ease.Evaluate(progress));
    }

    // creates the canvas image and material at runtime
    private void BuildUI()
    {
        // fall back to finding the shader by name
        if (transitionShader == null) transitionShader = Shader.Find("UI/NoiseVignetteTransition");
        if (transitionShader == null)
        {
            Debug.LogError("TransitionManager: assign the NoiseVignetteTransition shader in the Inspector.");
            return;
        }

        // overlay canvas that sits above the game
        var canvasGO = new GameObject("TransitionCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasGO.AddComponent<GraphicRaycaster>();   // blocks clicks while visible

        // image that fills the whole screen
        var imgGO = new GameObject("Vignette", typeof(RectTransform), typeof(RawImage));
        imgGO.transform.SetParent(canvasGO.transform, false);

        var rect = imgGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // copy inspector values into the material
        material = new Material(transitionShader);
        material.SetFloat(SoftnessId, softness);
        material.SetFloat(NoiseIntensityId, noiseIntensity);
        material.SetFloat(NoiseScaleId, noiseScale);
        material.SetFloat(NoiseSpeedId, noiseSpeed);
        material.SetFloat(CycleId, cycleDuration);
        material.SetFloat(SeedId, 0f);

        // apply colour and material to the image
        var img = imgGO.GetComponent<RawImage>();
        img.color = color;
        img.material = material;
    }
}
