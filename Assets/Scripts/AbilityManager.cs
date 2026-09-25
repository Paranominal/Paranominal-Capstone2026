using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [SerializeField] private ALTGrimoire grimoire;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private string dashEntryName;
    [Tooltip("The version of the dash ability to enable when obtained. Use 'charges' or 'cooldown'.")]
    [SerializeField] private string dashVersion;
    private bool dashObtained;

    // EDIT (special-shot): Special Shot unlock, same grimoire entry pattern as the dash.
    [SerializeField] private SpecialShot specialShot;
    [SerializeField] private string specialShotEntryName;
    private bool specialShotObtained;

    // EDIT (special-shot): fallback for cross-prefab reference.
    void Awake()
    {
        if (specialShot == null) specialShot = FindAnyObjectByType<SpecialShot>();
    }

    void Update()
    {
        if (!dashObtained && grimoire.entries.Exists(dashEntry => dashEntry.entryName == dashEntryName)) GainDash();
        // EDIT (special-shot): unlock once its entry is collected.
        if (!specialShotObtained && specialShot != null && grimoire.entries.Exists(entry => entry.entryName == specialShotEntryName)) GainSpecialShot();
    }

    void GainDash()
    {
        playerDash.dashEnabled = true;
        playerDash.DashVersionEnabled(dashVersion);
        dashObtained = true;
    }

    // EDIT (special-shot): unlocks the Special Shot.
    void GainSpecialShot()
    {
        specialShot.Unlock();
        specialShotObtained = true;
    }
}
