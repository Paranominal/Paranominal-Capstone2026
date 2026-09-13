using UnityEngine;
using TMPro;

public class GameOverDisplay : MonoBehaviour
{
    public TextMeshPro scoreText;

    private void Start()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Final score: {GameOverHandler.FinalScore}";
        }
    }
}