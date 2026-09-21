// Summary: Generates a Voronoi-subdivided mesh from a sprite in Awake, then drives the shatter
// shader's _ShatterAmount from 0 to 1 when Play() is called. Each triangle belongs to a single
// Voronoi cell so pieces move independently without stretching.

using UnityEngine;
using System;
using System.Collections;

public class ShatterEffect : MonoBehaviour
{
    [Tooltip("How long the shatter animation takes in seconds.")]
    [SerializeField] private float shatterDuration = 1.5f;
    [Tooltip("Destroy this GameObject when the shatter finishes.")]
    [SerializeField] private bool destroyOnComplete = true;
    [Tooltip("Pre-configured shatter material (Custom/Sprites/Shatter). Texture is copied from the sprite at runtime.")]
    [SerializeField] private Material shatterMaterial = null;

    [Header("Mesh Generation")]
    [Tooltip("Grid subdivisions per axis. Higher = cleaner cell edges, more triangles.")]
    [SerializeField] private int gridResolution = 25;
    [Tooltip("Number of Voronoi cells (pieces the sprite breaks into).")]
    [SerializeField] private int cellCount = 15;
    [Tooltip("Random seed for Voronoi layout. Different seeds produce different break patterns.")]
    [SerializeField] private int seed = 42;
    [Tooltip("Randomise the seed on Awake so each instance shatters differently.")]
    [SerializeField] private bool randomiseSeed = true;

    public event Action OnShatterComplete;

    private SpriteRenderer spriteRenderer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material material;
    private Mesh shatterMesh;
    private static readonly int ShatterAmountID = Shader.PropertyToID("_ShatterAmount");

    private void Awake()
    {
        if (randomiseSeed) seed = UnityEngine.Random.Range(0, 99999);
    }

    private GameObject shatterObject;

    private void PrepareShatterMesh()
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null) return;

        shatterMesh = GenerateMesh(sprite);

        // separate child object parented to the sprite so it inherits position/rotation
        shatterObject = new GameObject("ShatterMesh");
        shatterObject.transform.SetParent(spriteRenderer.transform, false);

        meshFilter = shatterObject.AddComponent<MeshFilter>();
        meshFilter.mesh = shatterMesh;

        meshRenderer = shatterObject.AddComponent<MeshRenderer>();
        meshRenderer.enabled = false;

        if (shatterMaterial != null)
        {
            material = new Material(shatterMaterial);
            material.SetTexture("_MainTex", sprite.texture);
            material.SetFloat(ShatterAmountID, 0f);
            meshRenderer.material = material;
        }
    }

    // standalone use: shatters the first SpriteRenderer found on this object or children
    public void Play()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        Play(sr);
    }

    // targeted use: shatters a specific SpriteRenderer (e.g. the active weakpoint element)
    public void Play(SpriteRenderer targetRenderer)
    {
        if (targetRenderer == null || targetRenderer.sprite == null) return;
        if (shatterMaterial == null) return;

        spriteRenderer = targetRenderer;
        PrepareShatterMesh();

        if (meshRenderer == null || material == null) return;

        spriteRenderer.enabled = false;
        meshRenderer.enabled = true;

        StartCoroutine(ShatterCoroutine());
    }

    private IEnumerator ShatterCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < shatterDuration)
        {
            elapsed += Time.deltaTime;
            material.SetFloat(ShatterAmountID, Mathf.Clamp01(elapsed / shatterDuration));
            yield return null;
        }

        material.SetFloat(ShatterAmountID, 1f);
        OnShatterComplete?.Invoke();

        if (destroyOnComplete)
            Destroy(gameObject);
        else
            CleanupShatter();
    }

    // stops an in-progress shatter and cleans up immediately (e.g. stagger ended while shatter was playing)
    public void Stop()
    {
        StopAllCoroutines();
        CleanupShatter();
    }

    // tears down the shatter mesh and resets state so the effect can be played again (e.g. boss phase weakpoints)
    private void CleanupShatter()
    {
        if (shatterObject != null)
        {
            Destroy(shatterObject);
            shatterObject = null;
        }

        meshFilter = null;
        meshRenderer = null;

        if (material != null)
        {
            Destroy(material);
            material = null;
        }

        if (shatterMesh != null)
        {
            Destroy(shatterMesh);
            shatterMesh = null;
        }

        spriteRenderer = null;
    }

    // --- mesh generation ---

    private Mesh GenerateMesh(Sprite sprite)
    {
        Bounds bounds = sprite.bounds;
        float xMin = bounds.min.x;
        float xMax = bounds.max.x;
        float yMin = bounds.min.y;
        float yMax = bounds.max.y;

        // sprite UVs (handles atlas packing)
        Rect texRect = sprite.textureRect;
        float uMin = texRect.x / sprite.texture.width;
        float uMax = (texRect.x + texRect.width) / sprite.texture.width;
        float vMin = texRect.y / sprite.texture.height;
        float vMax = (texRect.y + texRect.height) / sprite.texture.height;

        // generate Voronoi seeds in [0,1] space
        System.Random rng = new System.Random(seed);
        Vector2[] seeds = new Vector2[cellCount];
        for (int i = 0; i < cellCount; i++)
            seeds[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());

        // precompute per-cell hash
        float[] cellHashes = new float[cellCount];
        for (int i = 0; i < cellCount; i++)
            cellHashes[i] = Mathf.Abs(Mathf.Sin(i * 127.1f) * 43758.5453f) % 1f;

        // build mesh with non-shared vertices (each triangle is independent)
        int triCount = gridResolution * gridResolution * 2;
        int vertCount = triCount * 3;

        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector2[] uv2s = new Vector2[vertCount]; // cell center in object space
        Color[] colors = new Color[vertCount];
        int[] tris = new int[vertCount];

        int vi = 0;
        for (int gy = 0; gy < gridResolution; gy++)
        {
            for (int gx = 0; gx < gridResolution; gx++)
            {
                // normalized grid coordinates
                float nx0 = (float)gx / gridResolution;
                float nx1 = (float)(gx + 1) / gridResolution;
                float ny0 = (float)gy / gridResolution;
                float ny1 = (float)(gy + 1) / gridResolution;

                // object-space corners
                Vector3 bl = new Vector3(Mathf.Lerp(xMin, xMax, nx0), Mathf.Lerp(yMin, yMax, ny0), 0f);
                Vector3 br = new Vector3(Mathf.Lerp(xMin, xMax, nx1), Mathf.Lerp(yMin, yMax, ny0), 0f);
                Vector3 tl = new Vector3(Mathf.Lerp(xMin, xMax, nx0), Mathf.Lerp(yMin, yMax, ny1), 0f);
                Vector3 tr = new Vector3(Mathf.Lerp(xMin, xMax, nx1), Mathf.Lerp(yMin, yMax, ny1), 0f);

                // texture UVs
                Vector2 uvBL = new Vector2(Mathf.Lerp(uMin, uMax, nx0), Mathf.Lerp(vMin, vMax, ny0));
                Vector2 uvBR = new Vector2(Mathf.Lerp(uMin, uMax, nx1), Mathf.Lerp(vMin, vMax, ny0));
                Vector2 uvTL = new Vector2(Mathf.Lerp(uMin, uMax, nx0), Mathf.Lerp(vMin, vMax, ny1));
                Vector2 uvTR = new Vector2(Mathf.Lerp(uMin, uMax, nx1), Mathf.Lerp(vMin, vMax, ny1));

                // triangle 1 (BL, TL, BR): cell from centroid
                AddTriangle(bl, tl, br, uvBL, uvTL, uvBR,
                    seeds, cellHashes, xMin, xMax, yMin, yMax,
                    verts, uvs, uv2s, colors, tris, ref vi);

                // triangle 2 (BR, TL, TR)
                AddTriangle(br, tl, tr, uvBR, uvTL, uvTR,
                    seeds, cellHashes, xMin, xMax, yMin, yMax,
                    verts, uvs, uv2s, colors, tris, ref vi);
            }
        }

        Mesh mesh = new Mesh();
        if (vertCount > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.SetUVs(1, uv2s);
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddTriangle(
        Vector3 a, Vector3 b, Vector3 c,
        Vector2 uvA, Vector2 uvB, Vector2 uvC,
        Vector2[] seeds, float[] cellHashes,
        float xMin, float xMax, float yMin, float yMax,
        Vector3[] verts, Vector2[] uvs, Vector2[] uv2s, Color[] colors, int[] tris,
        ref int vi)
    {
        // find cell from triangle centroid
        Vector3 centroid = (a + b + c) / 3f;
        Vector2 normCentroid = new Vector2(
            Mathf.InverseLerp(xMin, xMax, centroid.x),
            Mathf.InverseLerp(yMin, yMax, centroid.y));
        int cell = FindNearestCell(normCentroid, seeds);

        // cell center in object space
        Vector2 cc = new Vector2(
            Mathf.Lerp(xMin, xMax, seeds[cell].x),
            Mathf.Lerp(yMin, yMax, seeds[cell].y));
        Color col = new Color(cellHashes[cell], 0f, 0f, 1f);

        verts[vi] = a; uvs[vi] = uvA; uv2s[vi] = cc; colors[vi] = col; tris[vi] = vi; vi++;
        verts[vi] = b; uvs[vi] = uvB; uv2s[vi] = cc; colors[vi] = col; tris[vi] = vi; vi++;
        verts[vi] = c; uvs[vi] = uvC; uv2s[vi] = cc; colors[vi] = col; tris[vi] = vi; vi++;
    }

    private int FindNearestCell(Vector2 point, Vector2[] seeds)
    {
        int nearest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < seeds.Length; i++)
        {
            float dist = (point - seeds[i]).sqrMagnitude;
            if (dist < minDist) { minDist = dist; nearest = i; }
        }
        return nearest;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (shatterMesh != null) Destroy(shatterMesh);
        if (shatterObject != null) Destroy(shatterObject);
    }
}