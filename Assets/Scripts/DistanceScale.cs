using UnityEngine;

public class DistanceScale : MonoBehaviour
{
    [Range(0,1)]
    [SerializeField] private float influence = 1;
    [Range(0.01f,2)]
    [SerializeField] private float finalSizeModifier = 1;
    private Vector3 cachedScale;

    void Awake()
    {
        cachedScale = transform.localScale;
    }

    // lets pooled objects set their base scale on reuse and apply it immediately
    public void SetBaseScale(Vector3 baseScale)
    {
        cachedScale = baseScale;
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (Camera.main == null) return;

        float distance = (Camera.main.transform.position - transform.position).magnitude;
        float size = distance * Camera.main.fieldOfView * 0.01f;
        transform.localScale = (cachedScale + Vector3.one * size * influence) * finalSizeModifier;
        transform.forward = transform.position - Camera.main.transform.position;
    }
}
