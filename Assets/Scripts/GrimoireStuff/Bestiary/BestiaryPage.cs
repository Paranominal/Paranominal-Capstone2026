using UnityEngine;
using TMPro;

// Summary: Root component of a Bestiary page prefab. The heading, flavour text and sprite are authored directly in the prefab.
// Only the kill count is filled in at runtime, by GrimoireBestiaryPanel.
public class BestiaryPage : MonoBehaviour
{
    [SerializeField] private TMP_Text killCountText;
    [Tooltip("{0} is replaced with the kill count.")]
    [SerializeField] private string killCountFormat = "Defeated: {0}";

    public void SetKillCount(int killCount)
    {
        if (killCountText != null)
            killCountText.SetText(string.Format(killCountFormat, killCount));
    }
}
