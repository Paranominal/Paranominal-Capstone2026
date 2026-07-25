using System.Collections;
using UnityEngine;

public class dimGrimoireLight : MonoBehaviour
{

    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private float dimFactor = 1f;
    private AnimationCurve lightCurve;
    private float cachedIntensity;
    private float dimmedIntensity;
    private Light lightComponent;

    private bool dimming;
    private bool brightening;
    private float currentChange;

    [SerializeField] private bool debugMode = false;

    void Start()
    {
        lightComponent = gameObject.GetComponent<Light>();
        cachedIntensity = lightComponent.intensity;
        dimmedIntensity = lightComponent.intensity * dimFactor;

        lightCurve = AnimationCurve.Linear(0, dimmedIntensity, 1, cachedIntensity);
    }

    void Update()
    {
        // if (grimoire.grimoireActive) lightComponent.intensity = dimmedIntensity;
        // else lightComponent.intensity = cachedIntensity;

        if (lightComponent.intensity > dimmedIntensity && grimoire.grimoireActive)
        {
            DoDim();
        }
        else if (lightComponent.intensity < cachedIntensity && !grimoire.grimoireActive)
        {
            DoBrighten();
        }

        if (debugMode) doDebugLog();
    }
    void DoDim()
    {
        StartCoroutine(Dim());
    }
    void DoBrighten()
    {
        StartCoroutine(Brighten());
    }
    
    private IEnumerator Dim()
    {
        if (dimming) yield break;
        dimming = true;
        if (debugMode) Debug.Log("start dimming");

        while (lightComponent.intensity > dimmedIntensity && !brightening)
        {
            currentChange -= Time.unscaledDeltaTime;
            lightComponent.intensity = lightCurve.Evaluate(currentChange);
            yield return null;
        }

        if (debugMode) Debug.Log("end dimming");
        dimming = false;
        yield break;
    }
    private IEnumerator Brighten()
    {
        if (brightening) yield break;
        brightening = true;
        if (debugMode) Debug.Log("start brightening");

        while (lightComponent.intensity < cachedIntensity && !dimming)
        {
            currentChange += Time.unscaledDeltaTime;
            lightComponent.intensity = lightCurve.Evaluate(currentChange);
            yield return null;
        }

        if (debugMode) Debug.Log("end brightening");
        brightening = false;
        yield break;
    }

    void doDebugLog()
    {
        Debug.Log($"current light intensity: [{lightComponent.intensity}]");
        Debug.Log($"current change: [{currentChange}]");
        Debug.Log($"cached intensity: [{cachedIntensity}]");
        Debug.Log($"dimmed intensity: [{dimmedIntensity}]");
        Debug.Log($"grimoire.grimoireActive: [{grimoire.grimoireActive}]");
        Debug.Log($"dimming: [{dimming}]");
        Debug.Log($"brightening: [{brightening}]");
    }
}
