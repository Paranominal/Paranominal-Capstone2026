using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneSelector : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private int maxNameLength = 24;

    private void Start()
    {
        CreateSceneButtons();
    }

    private void CreateSceneButtons()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            Button button = Instantiate(buttonPrefab, content, false);

            button.GetComponentInChildren<TMP_Text>().text = sceneName;

            int buildIndex = i;

            button.onClick.AddListener(() => LoadScene(buildIndex));
        }
    }

    private void LoadScene(int buildIndex)
    {
        SceneManager.LoadScene(buildIndex);
    }

    private string TruncateName(string name)
    {
        if (maxNameLength <= 0) return name;
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length <= maxNameLength) return name;

        int keep = Mathf.Max(0, maxNameLength - 3);
        if (keep == 0) return "...";
        return name.Substring(0, keep) + "...";
    }
}