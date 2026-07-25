using UnityEngine;
using TMPro;

public class GameOverDisplay : MonoBehaviour
{
    public TextMeshPro scoreText;

    private void Start()
    {
        if (GameOverHandler.FinalScore != null)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Final score: {GameOverHandler.FinalScore}";
            }
        }
        else { Debug.Log("uh oh!!!! you just opened this scene didn't you"); }
    }
}