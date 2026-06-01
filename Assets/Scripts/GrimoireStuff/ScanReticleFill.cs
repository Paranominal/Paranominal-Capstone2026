using UnityEngine;
using UnityEngine.UI;

public class ScanReticle : MonoBehaviour
{
    [SerializeField] private Image progressRing;

    public void SetProgress(float t)
    {
        progressRing.fillAmount = t;
        gameObject.SetActive(t > 0f);
    }
}
