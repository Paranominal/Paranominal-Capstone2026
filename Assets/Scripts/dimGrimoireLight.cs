using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class dimGrimoireLight : MonoBehaviour
{
    private Light lightComponent;
    [SerializeField] private GrimoirePositioner positioner;
    private float cachedIntensity;
    [SerializeField] private float raisedMulti = 0.3f;
    [SerializeField] private float menuMulti = 0.2f;
    [SerializeField] private float speed = 1f;

    void Start()
    {
        lightComponent = GetComponent<Light>();
        cachedIntensity = lightComponent.intensity;
    }

    float targetIntensity;
    void Update()
    {
        if (positioner.position == GrimoirePositioner.GrimoirePosition.lowered || positioner.position == GrimoirePositioner.GrimoirePosition.origin)// if lowered, target = cached intensity * 1
        { targetIntensity = cachedIntensity; }
        if (positioner.position == GrimoirePositioner.GrimoirePosition.open)// if raised, target = raisedIntensity
        { targetIntensity = cachedIntensity * raisedMulti; }
        if (positioner.position == GrimoirePositioner.GrimoirePosition.menu)// if open, target = openIntensity
        { targetIntensity = cachedIntensity * menuMulti; }

        lightComponent.intensity = Mathf.Lerp(lightComponent.intensity, targetIntensity, speed);
    }
}