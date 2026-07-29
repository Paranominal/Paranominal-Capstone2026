using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FearBarUI : MonoBehaviour
{
    [Header("References")] // can you tell im trying to be better about serializing 
    [SerializeField] private FearBar fearBar;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("Display")]
    [SerializeField] private bool grimoireOnlyVisibility = true;

    private float fullWidth;

    // EDIT (auto-resolve): fallbacks for cross-prefab references + null guards.
    private void Awake()
    {
        fullWidth = barRect.rect.width;

        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();

        if (fearBar != null)
            fearBar.OnFearChanged += HandleFearChanged;

        rankText.text = "Fine";
        SetBarWidth(0f);
    }

    private void Start()
    {
        if (grimoireOnlyVisibility)
        {
            gameObject.SetActive(false);

            if (ALTGrimoire.instance != null)
                ALTGrimoire.instance.OnGrimoireToggled += gameObject.SetActive;
            else
                Debug.LogWarning("[FearBarUI] ALTGrimoire.instance is null, grimoire visibility toggle won't work.");
        }
    }

    private void OnDestroy()
    {
        if (fearBar != null)
            fearBar.OnFearChanged -= HandleFearChanged;

        if (ALTGrimoire.instance != null)
            ALTGrimoire.instance.OnGrimoireToggled -= gameObject.SetActive;
    }

    private void HandleFearChanged(FearBar.FearRank rank)
    {
        SetBarWidth(fearBar.FearLevel / fearBar.MaxFear);
        rankText.text = rank.ToString();
    }

    private void SetBarWidth(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * fraction); // bitch this was hard to find
    }
}