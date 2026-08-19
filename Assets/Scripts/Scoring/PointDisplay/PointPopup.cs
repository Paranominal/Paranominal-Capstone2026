using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteBillboard))]
public class PointPopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;

    [Header("Lifetime")]
    [SerializeField] private float duration = 0.9f;
    [SerializeField] private float riseDistance = 0.6f;
    [SerializeField] private float fadeStart = 0.5f;  // time before fading starts

    private Vector3 startPos;
    private Color colour;
    private float elapsed;

    public void Play(string text, PointPopupStyle style, Vector3 position)
    {
        startPos = position;
        transform.position = position;

        label.text = text;
        colour = style.colour;
        label.color = colour;

        transform.localScale = Vector3.one * style.scaleMultiplier;

        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = startPos + Vector3.up * (riseDistance * t);

        // hold full opacity for the first stretch, then fade out
        colour.a = t < fadeStart ? 1f : 1f - Mathf.InverseLerp(fadeStart, 1f, t);
        label.color = colour;
    }
}
