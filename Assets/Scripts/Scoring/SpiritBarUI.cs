using UnityEngine;
using UnityEngine.UI;

public class SpiritBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpiritBar spiritBar;
    [SerializeField] private Image barImage;

    [Header("Display")]
    [SerializeField] private float displayMax = 200f; // this is an arbitrary limit for display, if we set hard caps on spirit later this won't be necessary

    private void Update()
    {
        barImage.fillAmount = Mathf.Clamp01(spiritBar.VisibleSpirit / displayMax);
    }
}