using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeDetector : MonoBehaviour
{
    public static SceneChangeDetector Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Object.FindAnyObjectByType<SceneChangeDetector>() == null)
        {
            var go = new GameObject("SceneChangeDetector");
            go.AddComponent<SceneChangeDetector>();
            Object.DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Object.DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Loaded scene: {scene.name} (mode: {mode})");
    }
}
