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

    // EDIT (auto-resolve): fallbacks for cross-prefab references + null guards.
    private void Awake()
    {
        fullWidth = barRect.rect.width;

        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (scoreManager != null)
        {
            scoreManager.OnPointsAdded += HandlePointsAdded;
            scoreManager.OnRankChanged += HandleRankChanged;
        }

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
                Debug.LogWarning("[SpiritBarUI] ALTGrimoire.instance is null, grimoire visibility toggle won't work.");
        }

        if (scoreManager != null)
        {
            scoreText.text = scoreManager.currentScore.ToString();
            rankText.text = scoreManager.CurrentRank;
            SetBarWidth(scoreManager.GetProgressToNextRank());
        }
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnPointsAdded -= HandlePointsAdded;
            scoreManager.OnRankChanged -= HandleRankChanged;
        }

        if (ALTGrimoire.instance != null)
            ALTGrimoire.instance.OnGrimoireToggled -= gameObject.SetActive;
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