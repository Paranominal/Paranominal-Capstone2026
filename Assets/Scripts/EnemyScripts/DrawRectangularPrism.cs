using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class DrawRectangularPrism : MonoBehaviour
{
    public Vector3 size = new Vector3(1, 1, 1);
    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        // 16 points are needed to trace all 12 edges of a box in one continuous line
        lr.positionCount = 16;
        UpdatePrism();
    }

    void Update()
    {
        // Update every frame if you want to change the size dynamically
        UpdatePrism();
    }

    void UpdatePrism()
    {
        Vector3 h = size * 0.5f; // Half-size for centering

        // Define the 8 corners
        Vector3[] v = new Vector3[8];
        v[0] = new Vector3(-h.x, -h.y, -h.z); // Bottom-Back-Left
        v[1] = new Vector3(h.x, -h.y, -h.z);  // Bottom-Back-Right
        v[2] = new Vector3(h.x, h.y, -h.z);   // Top-Back-Right
        v[3] = new Vector3(-h.x, h.y, -h.z);  // Top-Back-Left
        v[4] = new Vector3(-h.x, -h.y, h.z);  // Bottom-Front-Left
        v[5] = new Vector3(h.x, -h.y, h.z);   // Bottom-Front-Right
        v[6] = new Vector3(h.x, h.y, h.z);    // Top-Front-Right
        v[7] = new Vector3(-h.x, h.y, h.z);   // Top-Front-Left

        // Trace path: Bottom square -> Vertical -> Top square -> Remaining verticals
        Vector3[] path = new Vector3[] {
            v[0], v[1], v[2], v[3], v[0], // Back face
            v[4], v[5], v[1],             // Bottom face transition
            v[5], v[6], v[2],             // Right face transition
            v[6], v[7], v[3],             // Top face transition
            v[7], v[4]                    // Front face transition
        };

        lr.SetPositions(path); // Efficiently set all positions at once
    }
}
