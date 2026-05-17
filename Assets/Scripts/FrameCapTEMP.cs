using UnityEngine;

public class FrameCapTEMP : MonoBehaviour
{
    public int targetFrameRate = 60;
    void Start()
    {
        QualitySettings.vSyncCount = 0; // Set vSyncCount to 0 so that using .targetFrameRate is enabled.
        Application.targetFrameRate = targetFrameRate;
    }

}
