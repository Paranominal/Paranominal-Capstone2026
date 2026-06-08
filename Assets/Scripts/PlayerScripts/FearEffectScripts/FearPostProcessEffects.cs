using UnityEngine;

// Summary: Handles fear-driven post-processing effects including vignette and chromatic aberration.
public class FearPostProcessEffects : MonoBehaviour
{
    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        // TODO: handle any effects that gradually get more intense rather than by fear-level
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        // TODO: handle any fear-level gated effects
    }
}