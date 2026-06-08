using UnityEngine;

// Summary: Handles fear-driven audio effects including audio clips and audio mixer changes
public class FearAudioEffects : MonoBehaviour
{
    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        // TODO: handles increasing audio intensity by raw fear-value rather than fear-level
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        // TODO: handle audio effects gated by fear-level
    }
}