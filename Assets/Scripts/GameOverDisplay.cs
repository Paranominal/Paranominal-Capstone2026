using System.Collections;
using UnityEngine;

public class GameOverDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RollingCounter scoreCounter;

    [Header("Timing")]
    [Tooltip("delay before rolling to time with other animations")]
    [SerializeField] private float rollDelay = 0.5f;

    [Header("Debug")]
    [Tooltip("fake score for testing the scene as opened directly")]
    [SerializeField] private int testScore = 22219;

    private IEnumerator Start()
    {
        int finalScore = GameOverHandler.FinalScore;
        if (finalScore == 0 && Application.isEditor)
        {
            Debug.Log("no final score, did you open this scene directly? rolling the test score instead");
            finalScore = testScore;
        }

        yield return new WaitForSecondsRealtime(rollDelay);
        scoreCounter.SetValue(finalScore);
    }
}