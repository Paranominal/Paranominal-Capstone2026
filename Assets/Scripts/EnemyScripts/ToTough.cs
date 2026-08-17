using UnityEngine;

public class ToTough : MonoBehaviour, IDamageable
{
    private WeakPointManager wpManager;

    private void Awake()
    {
        wpManager = GetComponentInChildren<WeakPointManager>(true);

        HideWeakpoints();
    }

    private void HideWeakpoints()
    {
        if (wpManager != null)
        {
            wpManager.enabled = false;
            WeakPoint[] points = wpManager.GetComponentsInChildren<WeakPoint>(true);
            foreach (WeakPoint p in points)
            {
                p.Hide();
            }
        }
    }
}