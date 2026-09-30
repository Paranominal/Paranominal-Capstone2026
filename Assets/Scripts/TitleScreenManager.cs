using UnityEngine;

public class TitleScreenManager : MonoBehaviour
{
    void Start()
    {
        UnlockCursor();
    }

    void UnlockCursor()
    {
        if (Cursor.lockState == CursorLockMode.None) return;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
