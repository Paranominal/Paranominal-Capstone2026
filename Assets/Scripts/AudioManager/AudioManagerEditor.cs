#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private void OnEnable()
    {
        ((AudioManager)target).Resize();
    }
}
#endif