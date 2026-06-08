using UnityEngine;

// Summary: Coordinates all fear-driven effects by observing the player's fear status and distributing updates to focused sub-components.
public class FearEffectsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FearBar fearBar;
    [SerializeField] private PlayerStatus playerStatus;

    private FearPostProcessEffects postProcessEffects;
    private FearAudioEffects audioEffects;
    private FearIntrusiveEffects intrusiveEffects;

    private float normalizedFear;
    private bool isInEncounter;

    private void Awake()
    {
        postProcessEffects = GetComponent<FearPostProcessEffects>();
        audioEffects = GetComponent<FearAudioEffects>();
        intrusiveEffects = GetComponent<FearIntrusiveEffects>();

        fearBar.OnFearChanged += OnRankChanged;
    }

    private void OnDestroy()
    {
        fearBar.OnFearChanged -= OnRankChanged;
    }

    private void Update()
    {
        normalizedFear = 1f - ((float)fearBar.FearLevel / fearBar.MaxFear); // 0 = no fear, 1 = maximum fear
        isInEncounter = playerStatus.IsInEncounter;

        postProcessEffects?.UpdateIntensity(normalizedFear, isInEncounter);
        audioEffects?.UpdateIntensity(normalizedFear, isInEncounter);
    }

    private void OnRankChanged(FearBar.FearRank rank)
    {
        postProcessEffects?.OnRankChanged(rank);
        audioEffects?.OnRankChanged(rank);
        intrusiveEffects?.OnRankChanged(rank);
    }
}