using UnityEngine;

public class PhotoSnapshots : MonoBehaviour
{
    [Header("Camera Setup")]
    public Camera combinedCam;    
    public RenderTexture snapshotRT;

    public Texture2D TakeSnapshot()
    {
        combinedCam.Render();

        RenderTexture.active = snapshotRT; // wakey wakey!!!!
        Texture2D bakedPhoto = new Texture2D(snapshotRT.width, snapshotRT.height, TextureFormat.RGB24, false);
        bakedPhoto.ReadPixels(new Rect(0, 0, snapshotRT.width, snapshotRT.height), 0, 0);
        bakedPhoto.Apply();

        RenderTexture.active = null; // nighty night :)

        return bakedPhoto;
    }
}
