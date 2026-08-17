using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScaleButtonOnHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float scaleUpByMulti = 2;
    [Range(0f, 1f)]
    [SerializeField] private float scaleSpeed = 1;
    private Vector3 cachedScale;
    private bool isHovered;

    private void Start()
    {
        cachedScale = transform.localScale;
    }
    private void Update()
    {
        if (isHovered) ScaleUp();
        else ScaleDown();
    }

    private void ScaleUp()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, cachedScale * scaleUpByMulti, scaleSpeed);
    }
    private void ScaleDown()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, cachedScale, scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHovered = true;
    }
    public void OnDeselect(BaseEventData eventData)
    {
        isHovered = false;
    }
}
