using UnityEngine;
using System.Collections;

public class SpriteFX : MonoBehaviour
{
    [ColorUsage(true, true)]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private AnimationCurve flashCurve;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    private Material[] materials;


    void Awake()
    {
        // setup flash materials
        materials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            materials[i] = spriteRenderers[i].material;
        }
    }

    public void TriggerSpriteFlash(float duration = 0.25f)
    {
        StartCoroutine(SpriteFlash(duration));
    }

    private IEnumerator SpriteFlash(float duration)
    {
        SetFlashColor();
        float currentFlashAmount = 0f;
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;

            currentFlashAmount = Mathf.Lerp(1f, flashCurve.Evaluate(timeElapsed), timeElapsed / duration);
            SetFlashAmount(currentFlashAmount);

            yield return null;
        }
    }

    private void SetFlashColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            materials[i].SetColor("_Color_Override", flashColor);
        }
    }

    private void SetFlashAmount(float amount)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            materials[i].SetFloat("_Color_Override_Blend", amount);
        }
    }
}
