using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    // events that can be subscribed to for easy reference to current loading state (good for disabling input during load if necessary)
    public event Action<int> OnLoadStarted;
    public event Action<float> OnLoadProgress;
    public event Action<int> OnLoadCompleted;

    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadTime = 2f; // ensures loading screen doesn't just flash / flicker
    [SerializeField] private GameObject loadingScreenPrefab;
    private GameObject loadingScreen;
    public bool IsLoading { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        loadingScreen = Instantiate(loadingScreenPrefab, transform);
        loadingScreen.SetActive(false);

    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void LoadScene(int buildIndex)
    {
        if (IsLoading)
        {
            return;
        }

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"LoadingManager: build index {buildIndex} does not exist in Build Settings. Make sure it is set.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        IsLoading = true;
        OnLoadStarted?.Invoke(buildIndex);

        loadingScreen.SetActive(true);
        PauseManager.Instance?.PauseGame();

        AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);
        operation.allowSceneActivation = false;

        float loadTimeElapsed = 0;

        while (operation.progress < 0.9f || loadTimeElapsed < minimumLoadTime)
        {
            loadTimeElapsed += Time.unscaledDeltaTime;
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            OnLoadProgress?.Invoke(loadProgress);
            yield return null;
        }

        OnLoadProgress?.Invoke(1f);
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        PauseManager.Instance?.ResumeGame();
        loadingScreen.SetActive(false);
        OnLoadCompleted?.Invoke(buildIndex);
    }


}
