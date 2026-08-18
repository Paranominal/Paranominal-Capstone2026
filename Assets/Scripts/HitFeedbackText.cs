using TMPro;
using UnityEngine;

public class HitFeedbackText : MonoBehaviour
{
    private TextMeshPro TMPro;

    private float timer = 2f;
    void Awake()
    {
        TMPro = GetComponent<TextMeshPro>();
        //TMPro.text = "123";
    }
    void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            Destroy(gameObject);
        }
    }


    public void SetText(string text, Color color)
    {
        TMPro.text = text;
        TMPro.color = color;
    }
}
