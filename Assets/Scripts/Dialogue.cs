using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    // public TextMeshProUGUI action;
    // public TextMeshProUGUI itemName;
    // public TextMeshProUGUI description;
    // public GameObject itemDisplay;

    [SerializeField] private List<GameObject> pages;
    [SerializeField] private bool cursorLockOnClose;
    private int currentPage = 1;
    public int PageCount => pages.Count;
    public int CurrentPage => currentPage;

    private void Awake()
    {
        //hide all pages
        foreach (GameObject page in pages) page.SetActive(false);
        Debug.Log($"[{this}, {gameObject}]{currentPage} | {PageCount}");
        pages[currentPage - 1].SetActive(true);
    }

    public void GoToPage(int pageNumber)
    {
        if (pageNumber - 1 > pages.Count)
        {
            Debug.LogWarning($"[{this}] GoToPage number invalid!");
            return;
        }

        foreach (GameObject page in pages) page.SetActive(false);
        pages[pageNumber - 1].SetActive(true);
    }

    public void NextPage()
    {
        if (currentPage >= pages.Count)
        {
            Debug.LogWarning($"[{this}] NextPage invalid! There isn't one!");
            return;
        }

        foreach (GameObject page in pages) page.SetActive(false);
        currentPage++;
        pages[currentPage - 1].SetActive(true);
    }

    public void PreviousPage()
    {
        if (currentPage == 1)
        {
            Debug.LogWarning($"[{this}] PreviousPage invalid! There isn't one!");
            return;
        }

        foreach (GameObject page in pages) page.SetActive(false);
        currentPage--;
        pages[currentPage - 1].SetActive(true);
    }
}
