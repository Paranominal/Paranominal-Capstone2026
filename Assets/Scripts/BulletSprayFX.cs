using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSprayFX : MonoBehaviour
{
    [SerializeField] private InputActionReference shoot;
    [SerializeField] private List<SpriteRenderer> sprites;
    [SerializeField] private List<Light> lights;
    [SerializeField] private float spriteFadePercent;
    [SerializeField] private float lightFadePercent;
    [SerializeField]private List<float> maxAlpha;
    [SerializeField]private List<float> minAlpha;
    [SerializeField]private List<float> maxBrightness;
    [SerializeField]private List<float> minBrightness;

    void Start()
    {
        //cache sprite and light values
        foreach (Light light in lights)
        {
            maxBrightness.Add(light.intensity);
            minBrightness.Add(light.intensity * lightFadePercent);
        }
        foreach (SpriteRenderer sprite in sprites)
        {
            maxAlpha.Add(sprite.color.a);
            minAlpha.Add(sprite.color.a * spriteFadePercent);
        }
    }
    public void DoFX()
    {
        int i = 0;
        foreach (Light light in lights)
        {
            if (shoot.action.IsPressed()) light.intensity = maxBrightness[i];
            else light.intensity = minBrightness[i];
        }
        foreach (SpriteRenderer sprite in sprites)
        {
            if (shoot.action.IsPressed()) sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, maxAlpha[i]);
            else sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, minAlpha[i]);
        }
        i++;
    }
}
