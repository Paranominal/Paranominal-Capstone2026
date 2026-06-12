using UnityEngine;
using TMPro;

public class SpiritBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("Display")]
    [SerializeField] private bool grimoireOnlyVisibility = true;

    private float fullWidth;

    private void Awake()
    {
        fullWidth = barRect.rect.width;
        scoreManager.OnPointsAdded += HandlePointsAdded;
        scoreManager.OnRankChanged += HandleRankChanged;
        SetBarWidth(0f);
    }

    private void Start()
    {
        if (grimoireOnlyVisibility)
        {
            gameObject.SetActive(false);
            ALTGrimoire.instance.OnGrimoireToggled += gameObject.SetActive;
        }

        scoreText.text = scoreManager.currentScore.ToString();
        rankText.text = scoreManager.CurrentRank;
        SetBarWidth(scoreManager.GetProgressToNextRank()); // calls new function in scoremanager to get the float
    }

    private void HandlePointsAdded(int newTotal)
    {
        scoreText.text = newTotal.ToString();
        SetBarWidth(scoreManager.GetProgressToNextRank());
    }

    private void HandleRankChanged(string newRank)
    {
        rankText.text = newRank;
    }

    private void SetBarWidth(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * fraction);
    }
}