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
        if (lights[0] != null)
        {
            foreach (Light light in lights)
            {
                maxBrightness.Add(light.intensity);
                minBrightness.Add(light.intensity * lightFadePercent);
            }
        }
        if (sprites[0] != null)
        {
            foreach (SpriteRenderer sprite in sprites)
            {
                maxAlpha.Add(sprite.color.a);
                minAlpha.Add(sprite.color.a * spriteFadePercent);
            }
        }
    }
    public void DoFX()
    {
        if (lights[0] != null)
        {
            foreach (Light light in lights)
            {
                if (shoot.action.IsPressed()) light.intensity = maxBrightness[lights.IndexOf(light)];
                else light.intensity = minBrightness[lights.IndexOf(light)];
            }
        }
        if (sprites[0] != null)
        {
            foreach (SpriteRenderer sprite in sprites)
            {
                if (shoot.action.IsPressed()) sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, maxAlpha[sprites.IndexOf(sprite)]);
                else sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, minAlpha[sprites.IndexOf(sprite)]);
            }
        }
    }
}
