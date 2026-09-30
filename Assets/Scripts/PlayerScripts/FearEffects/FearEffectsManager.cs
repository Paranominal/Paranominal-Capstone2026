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

    // EDIT (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        postProcessEffects = GetComponent<FearPostProcessEffects>();
        audioEffects = GetComponent<FearAudioEffects>();
        intrusiveEffects = GetComponent<FearIntrusiveEffects>();

        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();
        if (playerStatus == null)
            playerStatus = FindAnyObjectByType<PlayerStatus>();

        if (fearBar != null)
            fearBar.OnFearChanged += OnRankChanged;
    }

    private void OnDestroy()
    {
        if (fearBar != null)
            fearBar.OnFearChanged -= OnRankChanged;
    }

    private void Update()
    {
        if (fearBar == null || playerStatus == null) return;

        normalizedFear = (float)fearBar.FearLevel / fearBar.MaxFear;
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
