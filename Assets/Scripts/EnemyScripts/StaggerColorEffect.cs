using UnityEngine;

public class StaggerColorEffect : MonoBehaviour
{
    // [Header("Visual Effects")]

    // //self-adjusted color picker
    // private Color staggerColor = new Color(0.5f, 0.5f, 0.5f, 1f); 

    // private SpriteRenderer[] spriteRenderers;
    // private Color[] originalColors;
    // private bool isInitialized = false;

    // private void Initialize()
    // {
    //     if (isInitialized) return;

    //     spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    //     if (spriteRenderers != null && spriteRenderers.Length > 0)
    //     {
    //         originalColors = new Color[spriteRenderers.Length];

    //         for (int i = 0; i < spriteRenderers.Length; i++)
    //         {
    //             originalColors[i] = spriteRenderers[i].color;
    //         }

    //         isInitialized = true;
    //     }
    // }

    // //apply the stagger color effect to the sprite
    // public void ApplyStaggerColor(Color color)
    // {
    //     Initialize();

    //     if (!isInitialized)
    //         return;

    //     for (int i = 0; i < spriteRenderers.Length; i++)
    //     {
    //         Color newColor = staggerColor;
    //         if (spriteRenderers[i].gameObject.tag != "WeakPoint") spriteRenderers[i].color = newColor;
    //     }
    // }

    // //restore the sprite to its original color
    // public void RestoreOriginalColor()
    // {
    //     Initialize();

    //     if (!isInitialized)
    //         return;

    //     for (int i = 0; i < spriteRenderers.Length; i++)
    //     {
    //         spriteRenderers[i].color = originalColors[i];
    //     }
    // }
}
