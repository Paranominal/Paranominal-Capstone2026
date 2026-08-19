using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Summary: Pools world-space label instances that track world positions.
// Replaces ScreenSpacePromptPool. The focus controller calls Show() for each
// active label per frame, then Flush() to fade out any that weren't refreshed.
// Handles positioning, fade, and billboard/fixed-facing rotation.
public class WorldLabelPool : MonoBehaviour
{
    [SerializeField] private GameObject labelPrefab;
    [Tooltip("How quickly labels fade in and out.")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private int initialPoolSize = 4;
    [Tooltip("How quickly billboard labels ease their rotation toward the camera.")]
    [SerializeField] private float billboardSmoothSpeed = 8f;

    private readonly Dictionary<int, LabelEntry> active = new();
    private readonly Queue<LabelEntry> free = new();
    private readonly HashSet<int> touchedThisFrame = new();
    private readonly List<int> recycleBuffer = new();
    private Camera cam;
    private bool visible = true;

    private class LabelEntry
    {
        public Transform root;
        public CanvasGroup group;
        public TMP_Text label;
        public float currentAlpha;
        public float targetAlpha;
        public Vector3 worldPos;
        public Quaternion fixedRotation;
        public WorldLabelMode mode;
    }

    void Awake()
    {
        for (int i = 0; i < initialPoolSize; i++)
            free.Enqueue(CreateEntry());
    }

    // Summary: Mark a label as active this frame. Call once per visible label.
    // id should be the anchor's GetInstanceID() so the same object keeps the same entry.
    public void Show(int id, Vector3 worldPos, Quaternion rotation, string text, float alpha, WorldLabelMode mode)
    {
        touchedThisFrame.Add(id);

        if (!active.TryGetValue(id, out LabelEntry entry))
        {
            entry = free.Count > 0 ? free.Dequeue() : CreateEntry();
            entry.root.gameObject.SetActive(true);
            entry.currentAlpha = 0f;
            active[id] = entry;
        }

        entry.targetAlpha = alpha;
        entry.worldPos = worldPos;
        entry.fixedRotation = rotation;
        entry.mode = mode;
        entry.label.text = text;
    }

    // Summary: Called after all Show() calls for this frame. Any label that wasn't
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

    // Summary: Master on/off gate. Hiding snaps all labels to invisible immediately.
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
            LabelEntry entry = kvp.Value;

            // Position.
            entry.root.position = entry.worldPos;

            // Rotation: billboard with easing, or fixed facing.
            if (entry.mode == WorldLabelMode.FloatingLabel)
            {
                // Billboard: face toward camera position.
                Vector3 dirToCamera = cam.transform.position - entry.worldPos;
                if (dirToCamera.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(-dirToCamera);
                    entry.root.rotation = Quaternion.Slerp(
                        entry.root.rotation, targetRot,
                        billboardSmoothSpeed * Time.unscaledDeltaTime);
                }
            }
            else
            {
                entry.root.rotation = entry.fixedRotation;
            }

            // Fade (unscaled so it resolves even when timeScale is 0).
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
            LabelEntry entry = active[id];
            entry.root.gameObject.SetActive(false);
            active.Remove(id);
            free.Enqueue(entry);
        }
    }

    private LabelEntry CreateEntry()
    {
        GameObject go = Instantiate(labelPrefab, transform);
        go.SetActive(false);
        return new LabelEntry
        {
            root = go.transform,
            group = go.GetComponent<CanvasGroup>(),
            label = go.GetComponentInChildren<TMP_Text>(),
        };
    }
}