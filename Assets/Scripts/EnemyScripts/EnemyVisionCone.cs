using UnityEngine;

public class EnemyVisionCone : MonoBehaviour
{
    private EnemyVisionSensor vision;
    private Mesh mesh;
    private MeshFilter coneFilter;
    private Vector3[] vertices;
    private int[] triangles;
    private Quaternion lastRotation = Quaternion.identity;
    private float lastViewDistance = -1f;
    private float lastViewAngle = -1f;
    private float lastEyeHeight = -1f;
    private int lastScanResolution = -1;
    
    public Material coneMaterial;
    public int scanResolution = 30; 

    void Awake()
    {
        vision = GetComponent<EnemyVisionSensor>();
        if (vision == null)
        {
            vision = GetComponentInParent<EnemyVisionSensor>();
        }

        if (vision == null)
        {
            enabled = false;
            return;
        }

        EnsureConeVisual();

        mesh = new Mesh();
        mesh.name = "EnemyVisionConeMesh";
        coneFilter.sharedMesh = mesh;
        
        MeshRenderer coneVisualRenderer = coneFilter.GetComponent<MeshRenderer>();
        if (coneMaterial != null)
        {
            coneVisualRenderer.sharedMaterial = coneMaterial;
        }

        //build cone once so it doesn't have to render again
        lastRotation = vision.transform.rotation;
        DrawCone();
    }

    //rebuilds cone when visual input changes
    void LateUpdate()
    {
        if (vision == null)
        {
            return;
        }

        bool resolutionChanged = scanResolution != lastScanResolution;
        bool settingsChanged = !Mathf.Approximately(vision.viewDistance, lastViewDistance) || !Mathf.Approximately(vision.viewAngle, lastViewAngle) || !Mathf.Approximately(vision.eyeHeight, lastEyeHeight);
        bool rotationChanged = vision.transform.rotation != lastRotation;

        if (resolutionChanged || settingsChanged || rotationChanged)
        {
            lastRotation = vision.transform.rotation;
            DrawCone();
        }
    }

    //memory optimisation, destroy the previously created mesh
    void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }
    }

    //creates mesh once and reuses the material
    private void EnsureConeVisual()
    {
        Transform parentTransform = vision.transform;

        Transform child = parentTransform.Find("VisionConeVisual");
        if (child == null)
        {
            child = transform.Find("VisionConeVisual");
        }

        if (child == null)
        {
            GameObject visual = new GameObject("VisionConeVisual");
            child = visual.transform;
            child.SetParent(parentTransform, false);
        }
        else if (child.parent != parentTransform)
        {
            child.SetParent(parentTransform, false);
        }

        //supposed to be around eye level but adjustments will have to be made
        child.localPosition = new Vector3(0f, vision.eyeHeight, 0f);
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        coneFilter = child.GetComponent<MeshFilter>();
        if (coneFilter == null)
        {
            coneFilter = child.gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer coneVisualRenderer = child.GetComponent<MeshRenderer>();
        if (coneVisualRenderer == null)
        {
            child.gameObject.AddComponent<MeshRenderer>();
        }
    }

    //draws the cone
    void DrawCone()
    {
        int safeResolution = Mathf.Max(1, scanResolution);
        int vertexCount = safeResolution + 2;

        if (vertices == null || vertices.Length != vertexCount)
        {
            vertices = new Vector3[vertexCount];
        }

        if (triangles == null || triangles.Length != safeResolution * 3)
        {
            triangles = new int[safeResolution * 3];
        }

        Transform visual = coneFilter.transform;
        visual.localPosition = new Vector3(0f, vision.eyeHeight, 0f);

        vertices[0] = Vector3.zero;

        float currentAngle = -vision.viewAngle / 2;
        float angleStep = vision.viewAngle / safeResolution;

        //equation from google, super clunky i'll have to fix it soon because the angle and anchor is super off
        for (int i = 0; i <= safeResolution; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;
            vertices[i + 1] = dir * vision.viewDistance;
            currentAngle += angleStep;

            if (i < safeResolution)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        lastViewDistance = vision.viewDistance;
        lastViewAngle = vision.viewAngle;
        lastEyeHeight = vision.eyeHeight;
        lastScanResolution = scanResolution;
    }
}