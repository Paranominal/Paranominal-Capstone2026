using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Summary: Renders the HUD on its own so its post-processing only touches UI pixels. The UI camera (a Base camera, not in
// PlayerCam's stack) renders into a transparent Render Texture at the world's internal resolution, and a RawImage on a
// Screen Space - Overlay canvas draws that texture over the final frame, after all post-processing.
// Recreates the texture whenever the window size or render scale changes. Lives on the UI camera.

[RequireComponent(typeof(Camera))]
public class UIRenderTarget : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The UI camera. Must be a Base camera that only renders the UI layer. Leave its Camera component disabled in the scene. Defaults to this object's Camera.")]
    [SerializeField] private Camera uiCamera;
    [Tooltip("Full-screen RawImage on a Screen Space - Overlay canvas, using the UI/PremultipliedComposite shader. Leave the RawImage component disabled in the scene.")]
    [SerializeField] private RawImage compositeImage;

    private RenderTexture renderTexture;
    private UniversalRenderPipelineAsset pipelineAsset;

    private void Awake()
    {
        if (uiCamera == null)
            uiCamera = GetComponent<Camera>();

        pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        // Transparent background so only the UI ends up with alpha.
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        UniversalAdditionalCameraData cameraData = uiCamera.GetUniversalAdditionalCameraData();
        if (cameraData != null && cameraData.renderType != CameraRenderType.Base)
            Debug.LogWarning($"[{this}] UI camera should be a Base camera, removed from PlayerCam's stack.");

        if (compositeImage != null)
            compositeImage.raycastTarget = false;
        else
            Debug.LogWarning($"[{this}] No Composite Image assigned, the HUD won't be shown.");
    }

    private void OnEnable()
    {
        UpdateRenderTexture();
    }

    private void LateUpdate()
    {
        UpdateRenderTexture();
    }

    private void OnDisable()
    {
        ReleaseRenderTexture();
    }

    // Matches the texture to the world's internal resolution (screen size * URP render scale). Only recreates on change.
    private void UpdateRenderTexture()
    {
        float renderScale = pipelineAsset != null ? pipelineAsset.renderScale : 1f;
        int width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * renderScale));
        int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * renderScale));

        if (renderTexture != null && renderTexture.width == width && renderTexture.height == height)
            return;

        ReleaseRenderTexture();

        // HDR with alpha so post-processing (bloom etc.) keeps its range and the UI keeps its transparency.
        // Depth-stencil is needed for UI Masks.
        renderTexture = new RenderTexture(width, height, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.D24_UNorm_S8_UInt)
        {
            name = "UIRenderTarget",
            filterMode = FilterMode.Point // Matches the world's upscale. Use Bilinear for a softer UI.
        };
        renderTexture.Create();

        // Target first, then switch the camera on, so it never renders a frame to the screen.
        uiCamera.targetTexture = renderTexture;
        uiCamera.enabled = true;

        if (compositeImage != null)
        {
            compositeImage.texture = renderTexture;
            compositeImage.enabled = true;
        }
    }

    // Switches the camera and composite off before releasing, so neither shows anything without a texture.
    private void ReleaseRenderTexture()
    {
        if (uiCamera != null)
        {
            uiCamera.enabled = false;
            uiCamera.targetTexture = null;
        }

        if (compositeImage != null)
        {
            compositeImage.enabled = false;
            compositeImage.texture = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}