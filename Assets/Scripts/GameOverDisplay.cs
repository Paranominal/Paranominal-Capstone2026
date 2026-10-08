using System.Collections;
using UnityEngine;
using TMPro;

public class GameOverDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RollingCounter scoreCounter;

    [Header("Timing")]
    [Tooltip("delay before rolling after load")]
    [SerializeField] private float rollDelay = 0.5f;
    [Tooltip("delay between rank popping in and the high score celebration")]
    [SerializeField] private float highScoreDelay = 0.5f;

    [Header("Rank")]
    [Tooltip("starts hidden; its animator plays the pop when it's switched on")]
    [SerializeField] private GameObject rankParent;
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("New High Score")]
    [Tooltip("starts hidden; its animator plays the pop when it's switched on")]
    [SerializeField] private GameObject newHighScoreText;
    [SerializeField] private Color highScoreColour = new Color(1f, 0.35f, 0.7f);
    [SerializeField] private ParticleSystem[] confetti;

    [Header("Debug")]
    [Tooltip("fake score for testing the scene as opened directly")]
    [SerializeField] private int testScore = 22219;
    [SerializeField] private string testRank = "S";
    [Tooltip("fake celebration. untick before building!")]
    [SerializeField] private bool forceNewHighScore = false;

    private IEnumerator Start()
    {
        rankParent.SetActive(false);
        newHighScoreText.SetActive(false);

        int finalScore = GameOverHandler.FinalScore;
        string finalRank = GameOverHandler.FinalRank;
        if (finalScore == 0 && Application.isEditor)
        {
            Debug.Log("no final score, did you open this scene directly? rolling the test score instead");
            finalScore = testScore;
        }

        bool isNewHighScore = forceNewHighScore || IsNewHighScore(finalScore);

        yield return new WaitForSecondsRealtime(rollDelay);
        scoreCounter.SetValue(finalScore);

        while (scoreCounter.IsRolling)
            yield return null;

        ShowRank(finalRank);

        if (isNewHighScore)
            Celebrate();
    }

    private bool IsNewHighScore(int score)
    {
        if (score <= 0 || LeaderboardManager.Instance == null) return false;

        return score > LeaderboardManager.Instance.HighestScore;
    }

    private void ShowRank(string rank)
    {
        rankText.text = rank;
        rankParent.SetActive(true);
    }

    private void Celebrate() // party time motherfuckers!!!!!!!!!!!!!!!!
    {
        newHighScoreText.SetActive(true);
        scoreCounter.SetColour(highScoreColour);
        foreach (ParticleSystem burst in confetti)
            burst.Play();
    }
}
