using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Grimoire : MonoBehaviour
{
    public static Grimoire Instance;

    [SerializeField] private GrimoireCodex codex;

    [SerializeField] private GameObject textDisplay;

    void Awake()
    {
        Instance = this;
    }

    public void AddEntry(GrimoireEntry grimoireEntry)
    {
        Debug.Log(grimoireEntry + " was recieved within " + gameObject);
        codex.currentLogs.Add(grimoireEntry);
        Debug.Log("Added " + grimoireEntry + " to Codex");

        textDisplay.GetComponent<TextMeshProUGUI>().SetText(grimoireEntry.entryName);
    }
}
