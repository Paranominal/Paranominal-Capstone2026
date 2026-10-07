using UnityEngine;
using TMPro;

public class RollingColumn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI currentText; // symbol sitting in the window
    [SerializeField] private TextMeshProUGUI nextText;    // symbol rolling in from above

    [Header("Reel")]
    [Tooltip("every symbol the column can roll through in order")]
    [SerializeField] private string symbols = "0123456789";

    private RectTransform rectTransform;
    private int shownIndex = -1;

    public string Symbols => symbols;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    public void SetPosition(float position) // position in symbols, 2.5 = halfway between the third and fourth
    {
        int index = Mathf.FloorToInt(position);
        float progress = position - index;

        if (index != shownIndex)
        {
            currentText.text = symbols[index % symbols.Length].ToString();
            nextText.text = symbols[(index + 1) % symbols.Length].ToString();
            shownIndex = index;
        }

        float height = rectTransform.rect.height;
        currentText.rectTransform.anchoredPosition = new Vector2(0f, -progress * height);       // slides down and out
        nextText.rectTransform.anchoredPosition = new Vector2(0f, (1f - progress) * height);    // slides down and in

        nextText.enabled = progress > 0f; // at rest only Current is needed
    }
}
