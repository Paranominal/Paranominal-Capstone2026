using UnityEngine;
using UnityEngine.Rendering;

public class FlareNoise : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LensFlareComponentSRP lensFlare;
    [Header("Options")]
    [SerializeField, Range(0,10)] private float flickerSpeed = 3f;
    [SerializeField] private bool intensityFlicker = true;
    [SerializeField, ShowIf("intensityFlicker", true)] private Vector2 intensityRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private bool scaleFlicker = false;
    [SerializeField, ShowIf("scaleFlicker", true)] private Vector2 scaleRange = new Vector2(0.9f, 1.1f);

    void Reset()
    {
        if (lensFlare == null) lensFlare = GetComponent<LensFlareComponentSRP>();
    }

    float noiseTime;
    void Awake()
    {
        noiseTime = Random.Range(0, 100);
    }
    
    void Update()
    {
        if (noiseTime >= 1000f) noiseTime = 0f;
        else noiseTime += Time.unscaledDeltaTime * flickerSpeed;
        float noise = Mathf.PerlinNoise(noiseTime, 0f);

        if (intensityFlicker) lensFlare.intensity = Mathf.Lerp(intensityRange.x, intensityRange.y, noise);
        if (scaleFlicker) lensFlare.scale = Mathf.Lerp(scaleRange.x, scaleRange.y, noise);
    }
}
