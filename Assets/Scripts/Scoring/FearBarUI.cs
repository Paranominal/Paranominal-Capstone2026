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

    private void Awake()
    {
        fullWidth = barRect.rect.width;
        fearBar.OnFearChanged += HandleFearChanged;
        rankText.text = "Fine";
        SetBarWidth(0f);
    }

    private void Start()
    {
        if (grimoireOnlyVisibility)
        {
            gameObject.SetActive(false);
            ALTGrimoire.instance.OnGrimoireToggled += gameObject.SetActive;
        }
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