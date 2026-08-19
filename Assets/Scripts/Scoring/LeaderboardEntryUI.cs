using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class LeaderboardEntryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private GameObject highlightBackground;
    
    public void SetData(int position, LeaderboardEntry entry, bool isHighlighted)
    {
        if (positionText != null) positionText.text = $"{position}";
        if (nameText != null) nameText.text = entry.playerName;
        if (scoreText != null) scoreText.text = entry.score.ToString();
        if (rankText != null) rankText.text = entry.rank;

        if (highlightBackground != null)
        {
            highlightBackground.SetActive(isHighlighted);
        }
    }
}