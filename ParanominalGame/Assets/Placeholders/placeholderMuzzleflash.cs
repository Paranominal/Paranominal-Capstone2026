using System.Collections;
using UnityEngine;

public class placeholderMuzzleflash : MonoBehaviour

{
public GameObject sprite;

    void Start()
    {
        sprite.GetComponent<SpriteRenderer>().enabled = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(MuzzleFlash());
        }
    }

    IEnumerator MuzzleFlash()
    {
        sprite.GetComponent<SpriteRenderer>().enabled = true;
        yield return new WaitForSeconds(0.2f);
        sprite.GetComponent<SpriteRenderer>().enabled = false;
    }
}
