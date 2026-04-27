using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private WeaponFiringLogic weaponFiringLogic;

    [Header("Ammo UI Game Objects")]
    [SerializeField] private Image[] ammoUiElements;

    [Header("Sprites")]
    [SerializeField] private Sprite baseAmmoSprite;
    [SerializeField] private Sprite ironStrikeSprite;
    [SerializeField] private Sprite silverStrikeSprite;

    private Image ammoUiTemplate;
    private int strikes;
    private float shotTemplateSpacing = 30f;

    private void Awake()
    {
        if (weaponEvents == null)
            weaponEvents = GetComponent<WeaponEvents>();

        if (weaponFiringLogic == null)
            weaponFiringLogic = GetComponent<WeaponFiringLogic>();

        CacheTemplate();
    }

    private void OnEnable()
    {
        if (weaponEvents != null)
        {
            weaponEvents.ShotResolved += OnShotResolved;
            weaponEvents.ReloadFinished += ResetStrikes;
            weaponEvents.AmmoChanged += OnAmmoChanged;
        }
    }

    private void OnDisable()
    {
        if (weaponEvents != null)
        {
            weaponEvents.ShotResolved -= OnShotResolved;
            weaponEvents.ReloadFinished -= ResetStrikes;
            weaponEvents.AmmoChanged -= OnAmmoChanged;
        }
    }

    private void Start()
    {
        int magazineSize = weaponFiringLogic != null ? weaponFiringLogic.MagazineSize : ammoUiElements.Length;
        BuildAmmoUiElements(magazineSize);
        ResetStrikes();
    }

    private void OnAmmoChanged(int currentAmmo, int magazineSize)
    {
        if (magazineSize != ammoUiElements.Length)
            BuildAmmoUiElements(magazineSize);
    }

    private void OnShotResolved(WeakPointType shotType, bool rewardedShot)
    {
        if (rewardedShot)
            return;

        StrikeAmmo(shotType);
    }

    private void StrikeAmmo(WeakPointType shotType)
    {
        if (strikes < 0 || strikes >= ammoUiElements.Length)
            return;

        ammoUiElements[strikes].sprite = shotType == WeakPointType.Iron
            ? ironStrikeSprite
            : silverStrikeSprite;

        strikes++;
    }

    private void ResetStrikes()
    {
        strikes = 0;

        for (int i = 0; i < ammoUiElements.Length; i++)
            ammoUiElements[i].sprite = baseAmmoSprite;
    }

    private void CacheTemplate()
    {
        if (ammoUiTemplate != null)
            return;

        for (int i = 0; i < ammoUiElements.Length; i++)
        {
            if (ammoUiElements[i] != null)
            {
                ammoUiTemplate = ammoUiElements[i];
                break;
            }
        }
    }

    private void BuildAmmoUiElements(int magazineSize)
    {
        CacheTemplate();
        if (ammoUiTemplate == null)
            return;

        magazineSize = Mathf.Max(0, magazineSize);

        for (int i = 0; i < ammoUiElements.Length; i++)
        {
            Image element = ammoUiElements[i];
            if (element != null && element != ammoUiTemplate)
                Destroy(element.gameObject);
        }

        ammoUiElements = new Image[magazineSize];
        RectTransform templateRect = ammoUiTemplate.rectTransform;
        Vector2 startAnchoredPosition = templateRect.anchoredPosition;
        float step = templateRect.rect.width + shotTemplateSpacing;
        int templateSiblingIndex = templateRect.GetSiblingIndex();

        for (int i = 0; i < magazineSize; i++)
        {
            Image element;

            if (i == 0)
            {
                element = ammoUiTemplate;
            }
            else
            {
                element = Instantiate(ammoUiTemplate, ammoUiTemplate.transform.parent);
                element.name = $"{ammoUiTemplate.name}_{i + 1}";
            }

            element.gameObject.SetActive(true);
            element.sprite = baseAmmoSprite;
            RectTransform elementRect = element.rectTransform;
            elementRect.anchoredPosition = startAnchoredPosition + (Vector2.right * (step * i));
            elementRect.SetSiblingIndex(templateSiblingIndex + i);
            ammoUiElements[i] = element;
        }

        if (magazineSize == 0)
            ammoUiTemplate.gameObject.SetActive(false);

        strikes = 0;
    }
}