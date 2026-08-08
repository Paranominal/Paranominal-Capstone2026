using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Summary: Pools screen-space UI elements that track world positions, replacing
// per-object world-space canvases. Lives on the HUD canvas. The focus controller
// calls Show() for each active WorldSpace prompt per frame, then Flush() to fade
// out any that weren't refreshed.
public class ScreenSpacePromptPool : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private GameObject promptPrefab; // needs CanvasGroup + TMP_Text
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private int initialPoolSize = 4;

    private readonly Dictionary<int, PromptEntry> active = new();
    private readonly Queue<PromptEntry> free = new();
    private readonly HashSet<int> touchedThisFrame = new();
    private readonly List<int> recycleBuffer = new();
    private Camera cam;
    private bool visible = true;
    private Canvas parentCanvas;

    private class PromptEntry
    {
        public RectTransform rect;
        public CanvasGroup group;
        public TMP_Text label;
        public float currentAlpha;
        public float targetAlpha;
        public Vector3 worldPos;
    }

    void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        for (int i = 0; i < initialPoolSize; i++)
            free.Enqueue(CreateEntry());
    }

    // Summary: Mark a prompt as active this frame. Call once per visible WorldSpace interactable.
    // id should be the interactable's GetInstanceID() so the same object keeps the same entry across frames.
    public void Show(int id, Vector3 worldPos, string text, float alpha)
    {
        touchedThisFrame.Add(id);

        if (!active.TryGetValue(id, out PromptEntry entry))
        {
            entry = free.Count > 0 ? free.Dequeue() : CreateEntry();
            entry.rect.gameObject.SetActive(true);
            entry.currentAlpha = 0f;
            active[id] = entry;
        }

        entry.targetAlpha = alpha;
        entry.worldPos = worldPos;
        entry.label.text = text;
    }

    // Summary: Called after all Show() calls for this frame. Any prompt that wasn't
    // touched this frame begins fading out.
    public void Flush()
    {
        foreach (var kvp in active)
        {
            if (!touchedThisFrame.Contains(kvp.Key))
                kvp.Value.targetAlpha = 0f;
        }
        touchedThisFrame.Clear();
    }

    // Summary: Master on/off gate. Hiding snaps all prompts to invisible immediately.
    public void SetVisible(bool state)
    {
        visible = state;
        if (!visible)
        {
            foreach (var kvp in active)
            {
                kvp.Value.targetAlpha = 0f;
                kvp.Value.currentAlpha = 0f;
                kvp.Value.group.alpha = 0f;
            }
        }
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        recycleBuffer.Clear();

        foreach (var kvp in active)
        {
            PromptEntry entry = kvp.Value;

            Vector3 screen = cam.WorldToScreenPoint(entry.worldPos);

            // Behind the camera: hide immediately, don't recycle yet (might come back).
            if (screen.z < 0f)
            {
                entry.group.alpha = 0f;
                continue;
            }

            // Convert screen point to local position within the container.
            Camera uiCam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screen, uiCam, out Vector2 localPos);
            entry.rect.anchoredPosition = localPos;

            // Fade. Unscaled so it resolves even when timeScale is 0 (grimoire).
            float target = visible ? entry.targetAlpha : 0f;
            entry.currentAlpha = Mathf.MoveTowards(
                entry.currentAlpha, target, fadeSpeed * Time.unscaledDeltaTime);
            entry.group.alpha = entry.currentAlpha;

            // Fully faded and not wanted: mark for recycling.
            if (entry.targetAlpha <= 0f && entry.currentAlpha <= 0f)
                recycleBuffer.Add(kvp.Key);
        }

        foreach (int id in recycleBuffer)
        {
            PromptEntry entry = active[id];
            entry.rect.gameObject.SetActive(false);
            active.Remove(id);
            free.Enqueue(entry);
        }
    }

    private PromptEntry CreateEntry()
    {
        GameObject go = Instantiate(promptPrefab, container);
        go.SetActive(false);
        return new PromptEntry
        {
            rect = go.GetComponent<RectTransform>(),
            group = go.GetComponent<CanvasGroup>(),
            label = go.GetComponentInChildren<TMP_Text>(),
        };
    }
}
