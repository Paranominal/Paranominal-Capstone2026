using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private string dashEntryName;
    [Tooltip("The version of the dash ability to enable when obtained. Use 'charges' or 'cooldown'.")]
    [SerializeField] private string dashVersion;
    private bool dashObtained;

    void Update()
    {
        if (!dashObtained && grimoire.entries.Exists(dashEntry => dashEntry.entryName == dashEntryName)) GainDash();
    }

    void GainDash()
    {
        playerDash.dashEnabled = true;
        playerDash.DashVersionEnabled(dashVersion);
        dashObtained = true;
    }
}
