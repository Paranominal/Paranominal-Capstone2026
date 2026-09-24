using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: Detail view on the right page of the Full Interface. Panels call SetDetail to fill it, or Clear to blank it.
// Empty fields are hidden so partial entries (e.g. an unkilled Bestiary entry) don't leave gaps.
// The image frame gets a seeded random offset and tilt, matching the Minimised UI's polaroid.
public class GrimoireDetailView : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Short status line under the name, e.g. \"collected\" or a kill count.")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text flavourText;
    [SerializeField] private TMP_Text hintText;

    [Header("Image")]
    [SerializeField] private RawImage displayImage;
    [Tooltip("The polaroid frame holding the image. Offset and tilted per entry.")]
    [SerializeField] private RectTransform imageFrame;

    [Header("Polaroid Randomisation")]
    [SerializeField] private Vector2 maxOffset = new Vector2(8f, 15f);
    [SerializeField] private float maxRotation = 20f;

    private Vector2 frameBasePosition;

    private void Awake()
    {
        if (imageFrame != null)
            frameBasePosition = imageFrame.anchoredPosition;
    }

    // Summary: Fills the view. Pass empty strings for fields to hide, and null for no image.
    // layoutSeed keeps each entry's polaroid placement consistent between visits.
    public void SetDetail(string name, string info, string description, string flavour, string hint, Texture image, int layoutSeed)
    {
        SetField(nameText, name);
        SetField(infoText, info);
        SetField(descriptionText, description);
        SetField(flavourText, flavour);
        SetField(hintText, hint);
        SetImage(image, layoutSeed);
    }

    public void Clear()
    {
        SetDetail("", "", "", "", "", null, 0);
    }

    private void SetField(TMP_Text field, string value)
    {
        if (field == null) return;

        bool hasValue = !string.IsNullOrEmpty(value);
        field.gameObject.SetActive(hasValue);
        field.SetText(hasValue ? value : "");
    }

    private void SetImage(Texture image, int layoutSeed)
    {
        if (imageFrame == null) return;

        imageFrame.gameObject.SetActive(image != null);
        if (image == null) return;

        if (displayImage != null)
            displayImage.texture = image;

        // System.Random so we don't reseed Unity's global Random, which gameplay scripts rely on.
        System.Random random = new System.Random(layoutSeed);
        float offsetX = RandomRange(random, -maxOffset.x, maxOffset.x);
        float offsetY = RandomRange(random, -maxOffset.y, maxOffset.y);
        float rotation = RandomRange(random, -maxRotation, maxRotation);

        imageFrame.anchoredPosition = frameBasePosition + new Vector2(offsetX, offsetY);
        imageFrame.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private float RandomRange(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
