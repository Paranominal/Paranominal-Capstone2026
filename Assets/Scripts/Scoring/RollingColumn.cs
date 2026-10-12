using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RollingColumn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI currentText; // symbol sitting in the window
    [SerializeField] private TextMeshProUGUI nextText;    // symbol rolling in from above
    [SerializeField] private LayoutElement layoutElement;

    [Header("Reel")]
    [Tooltip("every symbol the column can roll through in order")]
    [SerializeField] private string symbols = "0123456789";
    [Tooltip("width of the column when fully shown")]
    [SerializeField] private float width = 40f;

    private int shownIndex = -1;

    public string Symbols => symbols;

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

        float height = ((RectTransform)transform).rect.height;
        currentText.rectTransform.anchoredPosition = new Vector2(0f, -progress * height);       // slides down and out
        nextText.rectTransform.anchoredPosition = new Vector2(0f, (1f - progress) * height);    // slides down and in

        nextText.enabled = progress > 0f; // at rest only Current is needed
    }

    public void SetPresence(float presence) // 0 = folded away to nothing, 1 = full width
    {
        layoutElement.preferredWidth = width * presence;
        currentText.enabled = presence >= 1f; // hides the leading 0 while the first real digit rolls in
    }

    public void SetColour(Color colour)
    {
        currentText.color = colour;
        nextText.color = colour;
    }
}
