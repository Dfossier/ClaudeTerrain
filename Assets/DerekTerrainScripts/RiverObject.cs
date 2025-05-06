using UnityEngine;
using System.Collections.Generic;

public class RiverObject : MonoBehaviour
{
    private List<Vector3> pathPoints;
    private float flowVolume;
    private Material riverMaterial;

    public void Initialize(List<Vector3> pathPoints, float flowVolume)
    {
        this.pathPoints = new List<Vector3>(pathPoints);
        this.flowVolume = flowVolume;

        GenerateRiverMesh();
    }

    private void GenerateRiverMesh()
    {
        if (pathPoints.Count < 2) return;

        // Create mesh components if they don't exist
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create river mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Generate vertices along river path
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 current = pathPoints[i];
            Vector3 next = pathPoints[i + 1];
            Vector3 direction = (next - current).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

            // Calculate width based on flow volume and position
            float baseWidth = 5f * (1f + flowVolume * 0.2f);
            float startWidth = baseWidth * (1f - (float)i / (pathPoints.Count - 1) * 0.3f);
            float endWidth = baseWidth * (1f - (float)(i + 1) / (pathPoints.Count - 1) * 0.3f);

            // Create quad vertices with varying width
            Vector3 v1 = current + perpendicular * startWidth;
            Vector3 v2 = current - perpendicular * startWidth;
            Vector3 v3 = next + perpendicular * endWidth;
            Vector3 v4 = next - perpendicular * endWidth;

            // Adjust height to be slightly above terrain
            v1.y += 0.1f;
            v2.y += 0.1f;
            v3.y += 0.1f;
            v4.y += 0.1f;

            // Add vertices
            int vertexIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            // Add triangles
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            // Add UVs for flow animation
            float uvY = (float)i / (pathPoints.Count - 1);
            float uvYNext = (float)(i + 1) / (pathPoints.Count - 1);
            uvs.Add(new Vector2(0, uvY * 3f));     // Repeat UV 3 times for faster flow
            uvs.Add(new Vector2(1, uvY * 3f));
            uvs.Add(new Vector2(0, uvYNext * 3f));
            uvs.Add(new Vector2(1, uvYNext * 3f));
        }

        // Apply mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        // Set up material
        if (meshRenderer.sharedMaterial == null)
        {
            Material riverMat = new Material(Shader.Find("Standard"));
            riverMat.color = new Color(0.2f, 0.5f, 1f, 0.8f);
            riverMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            riverMat.renderQueue = 3000; // Transparent queue
            riverMat.SetFloat("_Mode", 3); // Transparent mode
            meshRenderer.sharedMaterial = riverMat;
        }
    }
}
