using UnityEngine;

public class CameraRecoilController : MonoBehaviour
{
    [Header("Camera Recoil")]
    [SerializeField] private float shotRecoilUpDegrees = 1.5f;
    [SerializeField] private float recoilReturnTime = 0.08f;

    private float recoilOffsetX;
    private float recoilVelocityX;

    public float RecoilOffsetX => recoilOffsetX;

    private void Update()
    {
        recoilOffsetX = Mathf.SmoothDamp(recoilOffsetX, 0f, ref recoilVelocityX, recoilReturnTime);
    }

    public void AddVerticalRecoil(float upDegrees)
    {
        recoilOffsetX -= Mathf.Abs(upDegrees);
    }

    public void PlayShotCameraRecoil()
    {
        AddVerticalRecoil(shotRecoilUpDegrees);
    }

    public void AddPitchOffset(float degrees)
    {
        recoilOffsetX += degrees;
    }
}
