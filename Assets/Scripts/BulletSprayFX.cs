using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSprayFX : MonoBehaviour
{
    [SerializeField] private InputActionReference shoot;
    [SerializeField] private List<SpriteRenderer> sprites;
    [SerializeField] private float maxAlpha;
    [SerializeField] private float minAlpha;
    public void DoFX()
    {
        foreach (SpriteRenderer sprite in sprites)
        {
            if (shoot.action.IsPressed()) sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, maxAlpha);
            else sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, minAlpha);
        }
    }
}
