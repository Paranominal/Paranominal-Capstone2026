using System.Collections.Generic;
using UnityEngine;

public class PointPopupSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PointPopup popupPrefab;

    [Header("Pool")]
    [SerializeField, Tooltip("Maximum point popups on screen")] private int poolSize = 10;

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
        PointPopup popup = pool[nextIndex];
        nextIndex = (nextIndex + 1) % poolSize;

        popup.gameObject.SetActive(true);
        popup.Play($"{points} points", worldPos, this);
    }
}
