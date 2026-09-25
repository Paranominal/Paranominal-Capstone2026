using UnityEngine;

// Summary: Takes Grimoire snapshots from SnapshotCam, which sits on PlayerCam and shares its view.
// TakeSnapshot returns the full view as a Texture2D (items). TakeEnemySnapshot returns a loose crop around an enemy as a RenderTexture (Bestiary).
public class PhotoSnapshots : MonoBehaviour
{
    [Header("Camera Setup")]
    public Camera combinedCam;    
    public RenderTexture snapshotRT;
    // Michael edit (bestiary-snapshots): FOV is copied from this camera before each capture, since the FOV setting changes it at runtime.
    [Tooltip("Camera whose FOV the snapshot camera copies. Defaults to the MainCamera-tagged camera (PlayerCam).")]
    [SerializeField] private Camera sourceCamera;

    // Michael edit (bestiary-snapshots): enemy snapshot settings.
    [Header("Enemy Snapshots")]
    [Tooltip("Pixel size of each Bestiary image. Match the aspect of the Bestiary's ImageScan.")]
    [SerializeField] private Vector2Int enemySnapshotSize = new Vector2Int(512, 512);
    [Tooltip("How much of the crop's height the enemy fills. Lower gives a looser crop with more surroundings.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float enemyFill = 0.5f;
    [Tooltip("Smallest crop, as a fraction of the full view's height. Stops distant enemies being zoomed in too far.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float minCropHeight = 0.3f;
    [SerializeField] private FilterMode enemySnapshotFilter = FilterMode.Point;

    // Michael edit (bestiary-sightline): shared by every BestiaryTracker, so the mask is set once here instead of on each enemy prefab.
    [Header("Enemy Sighting")]
    [Tooltip("Layers that block the player's view of enemies. An enemy behind these doesn't count as sighted.")]
    [SerializeField] private LayerMask sightObstructionLayers = ~0;
    public LayerMask SightObstructionLayers => sightObstructionLayers;

    // Leah's OG snapshot function, returns the full view as a Texture2D for items. ( Now with a null check for missing camera :D )
    public Texture2D TakeSnapshot()
    {
        if (!RenderCamera()) return null;

        RenderTexture.active = snapshotRT; // wakey wakey!!!!
        Texture2D bakedPhoto = new Texture2D(snapshotRT.width, snapshotRT.height, TextureFormat.RGB24, true, true);
        bakedPhoto.ReadPixels(new Rect(0, 0, snapshotRT.width, snapshotRT.height), 0, 0);
        bakedPhoto.Apply();

        RenderTexture.active = null; // nighty night :)

        return bakedPhoto;
    }

    // Michael edit (bestiary-snapshots): Renders the current view and copies a loose crop around the enemy into a new RenderTexture.
    // The copy happens on the GPU, so there's no ReadPixels stall. The caller (Bestiary) owns the returned texture.
    public RenderTexture TakeEnemySnapshot(Bounds enemyBounds)
    {
        if (!RenderCamera()) return null;

        Rect crop = GetCropRect(enemyBounds);

        RenderTexture output = new RenderTexture(enemySnapshotSize.x, enemySnapshotSize.y, 0);
        output.name = "EnemySnapshot";
        output.filterMode = enemySnapshotFilter;
        output.Create();

        // Scale and offset pick out the crop region of the full render.
        Graphics.Blit(snapshotRT, output, crop.size, crop.position);

        return output;
    }

    // Michael edit (bestiary-snapshots): Syncs FOV with PlayerCam and renders into snapshotRT. Returns false if the setup is missing.
    private bool RenderCamera()
    {
        if (combinedCam == null || snapshotRT == null)
        {
            Debug.LogWarning($"[{this}] Snapshot camera or render texture not assigned, skipping snapshot.");
            return false;
        }

        if (sourceCamera == null)
            sourceCamera = Camera.main;

        if (sourceCamera != null)
            combinedCam.fieldOfView = sourceCamera.fieldOfView;

        combinedCam.Render();
        return true;
    }

    // Michael edit (bestiary-snapshots): Crop region around the enemy, in 0-1 viewport space.
    // Sized so the enemy fills enemyFill of it, shaped to the output's aspect, and shifted to stay inside the frame.
    private Rect GetCropRect(Bounds enemyBounds)
    {
        // Project the bounds' corners to find the enemy's rect on screen.
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        bool anyInFront = false;

        Vector3 c = enemyBounds.center;
        Vector3 e = enemyBounds.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z);

            Vector3 viewportPoint = combinedCam.WorldToViewportPoint(corner);
            if (viewportPoint.z <= 0f) continue; // behind the camera

            anyInFront = true;
            min = Vector2.Min(min, viewportPoint);
            max = Vector2.Max(max, viewportPoint);
        }

        // Fallback to the full view if the enemy couldn't be placed.
        if (!anyInFront) return new Rect(0f, 0f, 1f, 1f);

        Vector2 centre = (min + max) * 0.5f;
        Vector2 enemySize = max - min;

        // Viewport units are stretched by the source aspect, so convert between the two shapes.
        float sourceAspect = snapshotRT.width / (float)snapshotRT.height;
        float outputAspect = enemySnapshotSize.x / (float)enemySnapshotSize.y;
        float widthToHeight = sourceAspect / outputAspect;

        // Crop height needed to fit the enemy's height, and its width, at the chosen fill.
        float cropHeight = Mathf.Max(
            enemySize.y / enemyFill,
            (enemySize.x / enemyFill) * widthToHeight,
            minCropHeight);
        float cropWidth = cropHeight / widthToHeight;

        // Can't crop more than the full view, so shrink to fit if needed.
        float overflow = Mathf.Max(cropWidth, cropHeight);
        if (overflow > 1f)
        {
            cropWidth /= overflow;
            cropHeight /= overflow;
        }

        // Centre on the enemy, then shift inwards if that runs off the edge.
        float x = Mathf.Clamp(centre.x - cropWidth * 0.5f, 0f, 1f - cropWidth);
        float y = Mathf.Clamp(centre.y - cropHeight * 0.5f, 0f, 1f - cropHeight);

        return new Rect(x, y, cropWidth, cropHeight);
    }
}