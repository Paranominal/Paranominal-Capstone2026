using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [SerializeField] private SoundDataSO music;
    [SerializeField] private float transitionDuration;
    public enum MusicMode { FixedDuration, PlayUntil};
    [SerializeField] private MusicMode mode = MusicMode.FixedDuration;
}
