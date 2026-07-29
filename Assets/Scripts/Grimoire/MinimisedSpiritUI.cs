using UnityEngine;
using TMPro;

// Summary: Displays the spirit meter on the left page of the minimised grimoire.
// Reads from ScoreManager (same data source as SpiritBarUI).
// Shows score, rank text, and a progress bar toward the next rank.
public class MinimisedSpiritUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private RectTransform barFillRect;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text rankText;

    private float fullWidth;

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (barFillRect != null)
            fullWidth = barFillRect.rect.width;
    }

    private void OnEnable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnPointsAdded += HandlePointsAdded;
            scoreManager.OnRankChanged += HandleRankChanged;

            // Refresh to current values.
            UpdateDisplay();
        }
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnPointsAdded -= HandlePointsAdded;
            scoreManager.OnRankChanged -= HandleRankChanged;
        }
    }

    private void HandlePointsAdded(int newTotal)
    {
        UpdateDisplay();
    }

    private void HandleRankChanged(string newRank)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (scoreManager == null) return;

        if (scoreText != null)
            scoreText.SetText(scoreManager.currentScore.ToString());

        if (rankText != null)
            rankText.SetText(scoreManager.CurrentRank);

        if (barFillRect != null)
        {
            float progress = Mathf.Clamp01(scoreManager.GetProgressToNextRank());
            barFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * progress);
        }
    }
}
