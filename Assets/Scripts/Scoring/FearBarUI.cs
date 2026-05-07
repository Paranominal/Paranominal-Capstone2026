using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FearBarUI : MonoBehaviour
{
    [Header("References")] // can you tell im trying to be better about serializing 
    [SerializeField] private FearBar fearBar;
    [SerializeField] private Image barImage;
    [SerializeField] private TextMeshProUGUI rankText;

    private void Awake()
    {
        fearBar.OnFearChanged += HandleFearChanged;
        rankText.text = "Fine";
    }

    private void HandleFearChanged(FearBar.FearRank rank)
    {
        barImage.fillAmount = Mathf.Clamp01((float)fearBar.FearLevel / fearBar.MaxFear);
        rankText.text = rank.ToString();
    }
}