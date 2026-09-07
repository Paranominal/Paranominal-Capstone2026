using System.Collections.Generic;
using UnityEngine;

public class PointPopupSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PointPopup popupPrefab;

    [Header("Pool")]
    [SerializeField, Tooltip("Maximum point popups on screen")] private int poolSize = 10;

    [Header("Tilt")]
    [Tooltip("Distance from enemy centre at which tilt reaches full strength.")]
    [SerializeField] private float tiltFalloff = 0.5f;

    [Header("Styles")]
    [Tooltip("Add new styles in here, ascending by minPrecision")] // hell yeah its serialisable
    [SerializeField]
    private PointPopupStyle[] styles = new PointPopupStyle[]
    {
        new PointPopupStyle { label = "Normal",  minPrecision = 1,  suffix = "",  colour = new Color(0,255,133), scaleMultiplier = 0.18f },
        new PointPopupStyle { label = "Perfect", minPrecision = 10, suffix = "!", colour = new Color(255,0,181), scaleMultiplier = 0.25f },
    };

    [Header("Miss Popup")]
    [SerializeField] private string missText = "Miss";
    [SerializeField]
    private PointPopupStyle missStyle = new PointPopupStyle { label = "Miss", suffix = "", colour = Color.grey, scaleMultiplier = 0.8f };

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
        {
            scoreManager.OnPointsAwarded += HandlePointsAwarded;
            scoreManager.OnShotMissed += HandleShotMissed;
        }
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnPointsAwarded -= HandlePointsAwarded;
            scoreManager.OnShotMissed -= HandleShotMissed;
        }
    }

    private void HandlePointsAwarded(int points, int precision, Vector3 worldPos, Vector3 ownerCentre)
    {
        PointPopupStyle style = StyleFor(precision);

        // which side of the enemy this weakpoint sits on from the player POV
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        float offset = Vector3.Dot(worldPos - ownerCentre, right);
        float side = Mathf.Clamp(offset / tiltFalloff, -1f, 1f);

        SpawnPopup($"{points}{style.suffix}", style, worldPos, side);
    }

    private void SpawnPopup(string text, PointPopupStyle style, Vector3 worldPos, float side)
    {
        PointPopup popup = pool[nextIndex];
        nextIndex = (nextIndex + 1) % poolSize;

        popup.gameObject.SetActive(true);
        popup.Play(text, style, worldPos, side);
    }

    private void HandleShotMissed(Vector3 worldPos)
    {
        // aint no side to pop from cuz aint no enemy cuz aint no globe earth
        SpawnPopup(missText, missStyle, worldPos, 0f);
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
