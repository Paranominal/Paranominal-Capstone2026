using UnityEngine;
using System.Collections;

public class WeaponFiringLogic : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private float reloadDuration = 2f;

    [Header("Shot Cooldown")]
    [SerializeField] private float shotCooldown = 0.2f;

    [Header("Misfire")]
    [SerializeField] private float misfireCooldown = 1f;

    
    public GameObject msifiretext, reloadtext;
    public GameObject dialoguemanager;
    [SerializeField] private Inventory inventory;

    private bool hasmisfired, hasreloaded;

    private bool isReloading;
    private bool onCooldown;
    private bool onMisfireCooldown;
    private int currentAmmo;
    private float reloadTimeRemaining;

    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
    public bool IsOnCooldown => onCooldown;
    public bool IsOnMisfireCooldown => onMisfireCooldown;
    public float ReloadProgress
    {
        get
        {
            if (!isReloading || reloadDuration <= 0f) return 0f;
            return Mathf.Clamp01(1f - (reloadTimeRemaining / reloadDuration));
        }
    }

    private void Awake()
    {
        currentAmmo = magazineSize;
    }

    public bool HasAmmo() => currentAmmo > 0;

    public bool CanManualReload() => !isReloading && currentAmmo < magazineSize;

    public void StartShotCooldown()
    {
        if (onCooldown)
            return;

        StartCoroutine(ShotCooldownRoutine());
    }

    public void StartMisfireCooldown()
    {
        if (onMisfireCooldown)
            return;

        //StartCoroutine(MisfireCooldownRoutine());
    }

    public bool TryStartReload()
    {
        if (isReloading)
            return false;

        StartCoroutine(ReloadRoutine());
        return true;
    }

    public void ConsumeAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - Mathf.Max(0, amount));
    }

    private IEnumerator ReloadRoutine()
    {
        
        if (isReloading)
            yield break;

        isReloading = true;
        reloadTimeRemaining = reloadDuration;

        while (reloadTimeRemaining > 0f)
        {
            reloadTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        currentAmmo = magazineSize;
        reloadTimeRemaining = 0f;
        isReloading = false;

        /*if (!hasreloaded)
        {

            dialoguemanager.GetComponent<DialogueManager>().StartDialogue(reloadtext);

            //inventory.Add(this.gameObject, true, text.GetComponent<CollectibleObject>().pickupDialogue);
            hasreloaded=true;
        } */
    }

    private IEnumerator ShotCooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(shotCooldown);
        onCooldown = false;
    }

    private IEnumerator MisfireCooldownRoutine()
    {
        
        onMisfireCooldown = true;
        yield return new WaitForSeconds(misfireCooldown);
        onMisfireCooldown = false;

        /*if (!hasmisfired)
        {

            dialoguemanager.GetComponent<DialogueManager>().StartDialogue(msifiretext);

            //inventory.Add(this.gameObject, true, text.GetComponent<CollectibleObject>().pickupDialogue);
            hasmisfired=true;
        } */
    }

    public void CancelActiveState()
    {
        StopAllCoroutines();
        isReloading = false;
        onCooldown = false;
        onMisfireCooldown = false;
        reloadTimeRemaining = 0f;
    }
}
