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

    [Tooltip("where the reel sits (from the left, starting at 0)")]
    [SerializeField] private float position = 0f;

    private RectTransform rectTransform;
    private int shownIndex = -1;
    private float appliedPosition;

    public string Symbols => symbols;
    public float Position => position;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    private void Start()
    {
        ApplyPosition();  // draw once so the texts don't show the editor placeholder
    }

    private void Update()
    {
        if (position != appliedPosition)
        {
            ApplyPosition();
        }
    }

    public void SetSymbol(char symbol)
    {
        int index = symbols.IndexOf(symbol);
        if (index < 0)
        {
            Debug.LogWarning($"{name}: '{symbol}' isn't in this column's symbols ({symbols})");
            return;
        }
        position = index;
        ApplyPosition();
    }

    private void ApplyPosition()
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

        appliedPosition = position;
        nextText.enabled = progress > 0f; // at rest only Current is needed, avoiding a startup visual bug
    }
}
