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
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private float defaultMinimumLoadTime = 2f;
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
        LoadScene(buildIndex, defaultMinimumLoadTime);
    }

    public void LoadScene(int buildIndex, float minimumTime)
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
        StartCoroutine(LoadSceneRoutine(buildIndex, minimumTime));
    }

    private IEnumerator LoadSceneRoutine(int buildIndex, float minimumTime)
    {
        IsLoading = true;
        OnLoadStarted?.Invoke(buildIndex);
        yield return TransitionManager.Instance?.TransitionIn();
        loadingScreen.SetActive(true);
        PauseManager.Instance?.PauseGame();
        yield return TransitionManager.Instance?.TransitionOut();

        AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);
        operation.allowSceneActivation = false;

        float loadTimeElapsed = 0;

        while (operation.progress < 0.9f || loadTimeElapsed < minimumTime)
        {
            loadTimeElapsed += Time.unscaledDeltaTime;
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            OnLoadProgress?.Invoke(loadProgress);
            yield return null;
        }

        OnLoadProgress?.Invoke(1f);

        PauseManager.Instance?.ResumeGame();

        yield return TransitionManager.Instance?.TransitionIn();
        operation.allowSceneActivation = true; 

        while (!operation.isDone)
        {
            yield return null;
        }

        
        loadingScreen.SetActive(false);
        IsLoading = false;
        OnLoadCompleted?.Invoke(buildIndex);
        yield return TransitionManager.Instance?.TransitionOut();
    }


}
