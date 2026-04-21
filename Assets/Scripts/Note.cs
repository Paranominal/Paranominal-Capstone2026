using UnityEngine;

public class Note : MonoBehaviour
{
    public GameObject noteUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        noteUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        noteUI.SetActive(false);
    }

}
