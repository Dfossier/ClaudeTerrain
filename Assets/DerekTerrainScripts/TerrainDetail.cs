using UnityEngine;

public class TerrainDetailManager : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Material grassMaterial;
    private float[,] heightMap;
    private float[,] moistureMap;
    private Vector2 chunkCoord;
    private bool isInitialized;
    private GrassSettings grassSettings;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f; // Update grass every 100ms
    private Camera mainCamera;
    private float distanceToCamera;
    private const float MAX_GRASS_DISTANCE = 100f;
    private MaterialPropertyBlock propertyBlock;
    private float currentLODBlend;
    private bool isGrassEnabled = true;

    public void Initialize(Vector2 coord, Material grassMat, float[,] heights, float[,] moisture, GrassSettings settings)
    {
        chunkCoord = coord;
        heightMap = heights;
        moistureMap = moisture;
        grassSettings = settings;
        meshFilter = GetComponent<MeshFilter>();
        mainCamera = Camera.main;
        propertyBlock = new MaterialPropertyBlock();

        if (grassMat != null)
        {
            grassMaterial = new Material(grassMat);
            SetupGrassMaterial();
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || !isGrassEnabled || mainCamera == null) return;

        // Update distance to camera
        distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);

        // Only update material properties periodically
        if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
        {
            UpdateGrassProperties();
            lastUpdateTime = Time.time;
        }
    }

    public void UpdateLODSettings(float lodBlend)
    {
        if (!isInitialized || !isGrassEnabled) return;
        currentLODBlend = lodBlend;
        UpdateGrassProperties();
    }

    private void UpdateGrassProperties()
    {
        if (grassMaterial == null || !isGrassEnabled || grassSettings == null) return;

        // Use LOD blend from terrain chunk system
        float distanceLOD = Mathf.Clamp01(distanceToCamera / grassSettings.maxDrawDistance);
        float finalLODBlend = Mathf.Max(currentLODBlend, distanceLOD);

        // Progressive density reduction
        float densityMultiplier = Mathf.Lerp(1f, grassSettings.densityFalloff, finalLODBlend);
        float finalDensity = Mathf.Lerp(grassSettings.maxDensity, grassSettings.minDensity, finalLODBlend);

        // Enhanced wind parameters with micro detail
        float timeOffset = Time.time * grassSettings.windSpeed;
        float windX = Mathf.Sin(timeOffset * 0.5f) + Mathf.Sin(timeOffset * 0.7f) * grassSettings.microDetailStrength;
        float windZ = Mathf.Cos(timeOffset * 0.3f) + Mathf.Cos(timeOffset * 0.5f) * grassSettings.microDetailStrength;
        Vector3 windDirection = new Vector3(windX, 0, windZ).normalized;

        // Enhanced property updates
        propertyBlock.SetFloat("_GrassDensity", finalDensity * densityMultiplier);
        propertyBlock.SetVector("_WindDirection", windDirection);
        propertyBlock.SetFloat("_WindSpeed", grassSettings.windSpeed);
        propertyBlock.SetFloat("_WindStrength", grassSettings.windStrength * (1f - finalLODBlend * 0.5f));
        propertyBlock.SetFloat("_WindFrequency", grassSettings.windFrequency);
        propertyBlock.SetFloat("_MicroDetailStrength", grassSettings.microDetailStrength);
        propertyBlock.SetFloat("_LODBlend", finalLODBlend);
        propertyBlock.SetFloat("_LODTransitionSpeed", grassSettings.lodTransitionSpeed);
        propertyBlock.SetFloat("_ColorVariation", grassSettings.colorVariation);
        propertyBlock.SetFloat("_HeightInfluence", grassSettings.heightInfluence);
        propertyBlock.SetFloat("_AmbientOcclusion", grassSettings.ambientOcclusion);
        propertyBlock.SetFloat("_ShadowSoftness", grassSettings.shadowSoftness);
        propertyBlock.SetFloat("_RimLightIntensity", grassSettings.rimLightIntensity);

        // Apply properties
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void SetupGrassMaterial()
    {
        int width = moistureMap.GetLength(0);
        int height = moistureMap.GetLength(1);

        // Create and setup textures
        SetupMoistureTexture(width, height);
        SetupHeightTexture(width, height);

        // Set base material properties
        SetMaterialProperties();

        // Setup mesh components
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
            
            // Create a simple quad mesh for the grass to generate from
            Mesh quadMesh = new Mesh();
            quadMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f)
            };
            quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            quadMesh.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            quadMesh.RecalculateNormals();
            meshFilter.sharedMesh = quadMesh;
        }

        MeshRenderer grassRenderer = GetComponent<MeshRenderer>();
        if (grassRenderer == null)
        {
            grassRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        if (grassMaterial.shader == null)
        {
            return;
        }
        
        grassRenderer.material = grassMaterial;
    }

    private void SetupMoistureTexture(int width, int height)
    {
        Texture2D moistureTexture = new Texture2D(width, height, TextureFormat.R16, false);
        moistureTexture.wrapMode = TextureWrapMode.Clamp;
        moistureTexture.filterMode = FilterMode.Bilinear;

        float[] moistureData = new float[width * height];
        float minMoisture = float.MaxValue;
        float maxMoisture = float.MinValue;

        // Find min/max and fill data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float m = moistureMap[x, y];
                minMoisture = Mathf.Min(minMoisture, m);
                maxMoisture = Mathf.Max(maxMoisture, m);
                moistureData[y * width + x] = m;
            }
        }

        // Normalize and set colors
        Color[] colors = new Color[width * height];
        for (int i = 0; i < moistureData.Length; i++)
        {
            float normalizedMoisture = Mathf.InverseLerp(minMoisture, maxMoisture, moistureData[i]);
            colors[i] = new Color(normalizedMoisture, normalizedMoisture, normalizedMoisture, 1);
        }

        moistureTexture.SetPixels(colors);
        moistureTexture.Apply(true, false);
        grassMaterial.SetTexture("_MoistureMap", moistureTexture);
        grassMaterial.SetFloat("_MoistureMin", minMoisture);
        grassMaterial.SetFloat("_MoistureMax", maxMoisture);
    }

    private void SetupHeightTexture(int width, int height)
    {
        Texture2D heightTexture = new Texture2D(width, height, TextureFormat.R16, false);
        heightTexture.wrapMode = TextureWrapMode.Clamp;
        heightTexture.filterMode = FilterMode.Bilinear;

        Color[] heights = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float h = heightMap[x, y];
                heights[y * width + x] = new Color(h, h, h, 1);
            }
        }

        heightTexture.SetPixels(heights);
        heightTexture.Apply(true, false);
        grassMaterial.SetTexture("_HeightMap", heightTexture);
    }

    private void SetMaterialProperties()
    {
        if (grassMaterial == null || grassSettings == null) return;

        // Color settings
        grassMaterial.SetColor("_HealthyColor", grassSettings.healthyColor);
        grassMaterial.SetColor("_DryColor", grassSettings.dryColor);
        grassMaterial.SetFloat("_ColorVariation", grassSettings.colorVariation);

        // Blade settings
        grassMaterial.SetFloat("_GrassHeight", Mathf.Lerp(grassSettings.minHeight, grassSettings.maxHeight, 0.5f));
        grassMaterial.SetFloat("_GrassWidth", Mathf.Lerp(grassSettings.minWidth, grassSettings.maxWidth, 0.5f));
        grassMaterial.SetFloat("_GrassDensity", grassSettings.maxDensity);
        grassMaterial.SetFloat("_NoiseSpread", grassSettings.noiseSpread);

        // Placement settings
        grassMaterial.SetFloat("_MoistureThreshold", grassSettings.moistureThreshold);
        grassMaterial.SetFloat("_SteepnessThreshold", grassSettings.steepnessThreshold);
        grassMaterial.SetFloat("_HeightInfluence", grassSettings.heightInfluence);

        // Wind settings
        grassMaterial.SetFloat("_WindSpeed", grassSettings.windSpeed);
        grassMaterial.SetFloat("_WindStrength", grassSettings.windStrength);
        grassMaterial.SetFloat("_WindFrequency", grassSettings.windFrequency);
        grassMaterial.SetFloat("_MicroDetailStrength", grassSettings.microDetailStrength);

        // LOD settings
        grassMaterial.SetFloat("_MaxDrawDistance", grassSettings.maxDrawDistance);
        grassMaterial.SetFloat("_LODTransitionSpeed", grassSettings.lodTransitionSpeed);
        grassMaterial.SetFloat("_DensityFalloff", grassSettings.densityFalloff);

        // Lighting settings
        grassMaterial.SetFloat("_AmbientOcclusion", grassSettings.ambientOcclusion);
        grassMaterial.SetFloat("_ShadowSoftness", grassSettings.shadowSoftness);
        grassMaterial.SetFloat("_RimLightIntensity", grassSettings.rimLightIntensity);

        // Transform data
        grassMaterial.SetVector("_ChunkOffset", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
        grassMaterial.SetVector("_ChunkScale", new Vector4(transform.localScale.x, transform.localScale.y, transform.localScale.z, 0));

        grassMaterial.SetFloat("_ClumpScale", grassSettings.clumpScale);
        grassMaterial.SetFloat("_ClumpSpread", grassSettings.clumpSpread);
    }

    public void UpdateWindSettings(GrassSettings settings)
    {
        if (!isInitialized) return;
        grassSettings = settings;
        if (propertyBlock != null)
        {
            UpdateGrassProperties();
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
