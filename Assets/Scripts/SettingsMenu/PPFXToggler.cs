using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


public class PPFXToggler : MonoBehaviour
{
    [SerializeField] private UniversalRendererData PC_Renderer;
    [SerializeField] private Toggle toggle;

    [SerializeField]
    private List<string> featureNames = new();

    private readonly List<ScriptableRendererFeature> features = new();

    private void Start()
    {
        foreach (var rendererFeature in PC_Renderer.rendererFeatures)
        {
            if (featureNames.Contains(rendererFeature.name))
            {
                features.Add(rendererFeature);
            }
        }

        // Initialize toggle from first feature
        if (features.Count > 0)
        {
            toggle.isOn = features[0].isActive;
        }

        toggle.onValueChanged.AddListener(SetFeaturesActive);
    }

    private void SetFeaturesActive(bool enabled)
    {
        foreach (var feature in features)
        {
            feature.SetActive(enabled);
        }
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(SetFeaturesActive);
    }

}