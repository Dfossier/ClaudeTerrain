using UnityEngine;

[System.Serializable]
public class GrassSettings
{
    public Texture2D grassTexture;
    public Color healthyColor = new Color(0.2f, 0.8f, 0.2f);
    public Color dryColor = new Color(0.8f, 0.8f, 0.2f);
    [Range(0, 5)]
    public float minWidth = 1f;
    [Range(0, 5)]
    public float maxWidth = 1.5f;
    [Range(0, 5)]
    public float minHeight = 1f;
    [Range(0, 5)]
    public float maxHeight = 1.5f;
    [Range(0, 1)]
    public float noiseSpread = 0.1f;
    [Range(0, 1)]
    public float minDensity = 0.1f;
    [Range(0, 1)]
    public float maxDensity = 0.8f;
    [Range(0, 1)]
    public float moistureThreshold = 0.3f;
    [Range(0, 1)]
    public float steepnessThreshold = 0.7f;

    [Header("Wind Settings")]
    [Range(0, 1)]
    public float windStrength = 0.5f;
    [Range(0, 1)]
    public float windSpeed = 0.5f;
    [Range(0, 1)]
    public float windFrequency = 0.5f;
}

public class TerrainDetailManager : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Material grassMaterial;
    private float[,] heightMap;
    private float[,] moistureMap;
    private Vector2 chunkCoord;
    private bool isInitialized;
    private GrassSettings grassSettings;

    public void Initialize(Vector2 coord, Material grassMat, float[,] heights, float[,] moisture, GrassSettings settings)
    {
        if (isInitialized) return;

        chunkCoord = coord;
        heightMap = heights;
        moistureMap = moisture;
        grassSettings = settings;
        meshFilter = GetComponent<MeshFilter>();

        if (grassMat != null)
        {
            grassMaterial = new Material(grassMat);
            SetupGrassMaterial();
        }

        isInitialized = true;
    }

    private void SetupGrassMaterial()
    {
        if (grassMaterial == null || heightMap == null || moistureMap == null) return;

        // Create moisture texture
        int width = moistureMap.GetLength(0);
        int height = moistureMap.GetLength(1);
        Texture2D moistureTexture = new Texture2D(width, height);

        Color[] colors = new Color[width * height];
        float minMoisture = float.MaxValue;
        float maxMoisture = float.MinValue;

        // Find min/max moisture values
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float m = moistureMap[x, y];
                minMoisture = Mathf.Min(minMoisture, m);
                maxMoisture = Mathf.Max(maxMoisture, m);
            }
        }

        // Add debug logging
        Debug.Log($"Chunk {chunkCoord}: Moisture range {minMoisture:F2} to {maxMoisture:F2}");
        if (maxMoisture < grassSettings.moistureThreshold)
        {
            Debug.LogWarning($"Chunk {chunkCoord}: All moisture values below threshold {grassSettings.moistureThreshold}!");
        }

        // Normalize and set colors
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedMoisture = Mathf.InverseLerp(minMoisture, maxMoisture, moistureMap[x, y]);
                colors[y * width + x] = new Color(normalizedMoisture, normalizedMoisture, normalizedMoisture);
            }
        }

        moistureTexture.SetPixels(colors);
        moistureTexture.Apply();

        // Set material properties
        grassMaterial.SetTexture("_MoistureMap", moistureTexture);
        grassMaterial.SetFloat("_MoistureMin", minMoisture);
        grassMaterial.SetFloat("_MoistureMax", maxMoisture);
        grassMaterial.SetFloat("_MoistureThreshold", grassSettings.moistureThreshold);
        grassMaterial.SetFloat("_WaterLevel", 0.1f); // You might want to make this configurable
        grassMaterial.SetFloat("_GrassHeight", (grassSettings.maxHeight + grassSettings.minHeight) / 2);
        grassMaterial.SetFloat("_GrassWidth", (grassSettings.maxWidth + grassSettings.minWidth) / 2);
        grassMaterial.SetFloat("_WindSpeed", grassSettings.windSpeed * 10); // Scale to shader range
        grassMaterial.SetFloat("_WindStrength", grassSettings.windStrength);
        grassMaterial.SetFloat("_WindFrequency", grassSettings.windFrequency);
        grassMaterial.SetFloat("_GrassDensity", Mathf.Lerp(grassSettings.minDensity, grassSettings.maxDensity, 0.5f));
        grassMaterial.SetColor("_GrassColor", grassSettings.healthyColor);

        // Attach material to renderer
        MeshRenderer grassRenderer = GetComponent<MeshRenderer>();
        if (grassRenderer == null)
        {
            grassRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        grassRenderer.material = grassMaterial;
    }

    public void UpdateWindSettings(GrassSettings settings)
    {
        if (grassMaterial != null)
        {
            grassMaterial.SetFloat("_WindStrength", settings.windStrength);
            grassMaterial.SetFloat("_WindSpeed", settings.windSpeed * 10);
            grassMaterial.SetFloat("_WindFrequency", settings.windFrequency);
        }
    }

    void OnDestroy()
    {
        if (grassMaterial != null)
        {
            Destroy(grassMaterial);
        }
    }
}