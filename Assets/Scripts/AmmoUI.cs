using System;
using Unity.VisualScripting;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("Ammo UI Game Objects")]
    [SerializeField] private Image[] AmmoUiElement;
    [Header("Sprites")]
    public Sprite baseAmmoSprite;
    public Sprite ironStrikeSprite;
    public Sprite silverStrikeSprite;
    private int strikes = 0;

    void Start()
    {
        ResetStrikes();
    }

    // Update is called once per frame
    public void StrikeAmmo(WeakPointType shotType)
    {
        if (shotType == WeakPointType.Iron) AmmoUiElement[strikes].sprite = ironStrikeSprite;
        if (shotType == WeakPointType.Silver) AmmoUiElement[strikes].sprite = silverStrikeSprite;
        strikes++;
    }
    public void ResetStrikes()
    {
        strikes = 0;
        int i = 0;
        foreach (Image element in AmmoUiElement)
        {
            AmmoUiElement[i].sprite = baseAmmoSprite;
            i++;
        }
    }
}
