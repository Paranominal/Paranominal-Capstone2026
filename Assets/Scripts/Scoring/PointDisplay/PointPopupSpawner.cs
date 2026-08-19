using System.Collections.Generic;
using UnityEngine;

public class PointPopupSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PointPopup popupPrefab;

    [Header("Pool")]
    [SerializeField, Tooltip("Maximum point popups on screen")] private int poolSize = 10;

    [Header("Styles")]
    [Tooltip("Add new styles in here, ascending by minPrecision")] // hell yeah its serialisable
    [SerializeField]
    private PointPopupStyle[] styles = new PointPopupStyle[]
    {
        new PointPopupStyle { label = "Normal",  minPrecision = 1,  suffix = "",  colour = new Color(0,255,133), scaleMultiplier = 1f },
        new PointPopupStyle { label = "Perfect", minPrecision = 10, suffix = "!", colour = new Color(255,0,181), scaleMultiplier = 1.35f },
    };

    private PointPopup[] pool;
    private int nextIndex;

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        pool = new PointPopup[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(popupPrefab, transform);
            pool[i].gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.OnPointsAwarded += HandlePointsAwarded;
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.OnPointsAwarded -= HandlePointsAwarded;
    }

    private void HandlePointsAwarded(int points, int precision, Vector3 worldPos)
    {
        PointPopupStyle style = StyleFor(precision);

        PointPopup popup = pool[nextIndex];
        nextIndex = (nextIndex + 1) % poolSize;

        popup.gameObject.SetActive(true);
        popup.Play($"{points} points{style.suffix}", style, worldPos);
    }

    private PointPopupStyle StyleFor(int precision)
    {
        PointPopupStyle chosen = styles[0];
        foreach (PointPopupStyle style in styles)
        {
            if (precision >= style.minPrecision)
                chosen = style;
        }
        return chosen;
    }
}
