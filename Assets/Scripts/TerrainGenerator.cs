using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    const float POSITION_SCALE = 0.99f;


    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public HeatMapSettings heatMapSettings;
    public MoistureSettings moistureSettings;
    public BiomeSettings biomeSettings;
    public TextureData textureSettings;

    public Transform viewer;
    public Material mapMaterial;
    public Material heatMaterial;
    public Material biomeMaterial;
    public Material grassMaterial;
    public bool useHeatMaterial;
    public bool useBiomeMaterial;
    public GrassSettings grassSettings;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
        if (grassSettings == null)
        {
            grassSettings = new GrassSettings();
        }

        Material materialToUse = DetermineMaterialToUse();
        InitializeTerrainSettings(materialToUse);

        if (grassMaterial != null)
        {
            // Initialize grass material properties
            // Initialize grass material properties
            grassMaterial.SetFloat("_GrassHeight", grassSettings.maxHeight);
            grassMaterial.SetFloat("_GrassWidth", 0.05f); // Thin blades
            grassMaterial.SetFloat("_GrassBend", 0.3f); // Moderate bend
            grassMaterial.SetFloat("_GrassDensity", grassSettings.maxDensity);
            grassMaterial.SetFloat("_MoistureThreshold", grassSettings.moistureThreshold);
            grassMaterial.SetColor("_GrassColor", grassSettings.healthyColor);
            grassMaterial.SetFloat("_GrassColorVariation", 0.2f);
            grassMaterial.SetFloat("_WindSpeed", grassSettings.windSpeed);
            grassMaterial.SetFloat("_WindStrength", grassSettings.windStrength);
            grassMaterial.SetFloat("_WindFrequency", 0.5f);
            grassMaterial.SetFloat("_WaterLevel", heightMapSettings.waterLevel);
            grassMaterial.SetVector("_TerrainSize", new Vector4(meshSettings.meshWorldSize, meshSettings.meshWorldSize, 0, 0));
        }

        float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

        UpdateVisibleChunks();
    }

    private Material DetermineMaterialToUse()
    {
        if (useHeatMaterial && heatMaterial != null)
            return heatMaterial;
        if (useBiomeMaterial && biomeMaterial != null)
            return biomeMaterial;
        return mapMaterial;
    }

    private void InitializeTerrainSettings(Material material)
    {
        if (useBiomeMaterial && biomeSettings != null)
        {
            biomeSettings.ApplyToMaterial(material);
            Debug.Log("Biome materials initialized");
        }
        else
        {
            textureSettings.ApplyToMaterial(material);
            textureSettings.UpdateMeshHeights(material, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        }
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        Material materialToUse = DetermineMaterialToUse();
                        CreateNewChunk(viewedChunkCoord, materialToUse);
                    }
                }
            }
        }
    }

    private void CreateNewChunk(Vector2 coord, Material material)
    {
        if (grassSettings == null)
        {
            grassSettings = new GrassSettings();
        }

        // Calculate position while keeping the original mesh size
        Vector2 position = coord * meshSettings.meshWorldSize * POSITION_SCALE;

        if (grassMaterial != null)
        {
            grassMaterial.SetVector("_ChunkOffset", new Vector4(position.x, 0, position.y, 0));
        }

        // Create chunk with standard settings but pass the overlap value
        TerrainChunk newChunk = new TerrainChunk(
            coord,
            heightMapSettings,
            heatMapSettings,
            moistureSettings,
            meshSettings,
            detailLevels,
            colliderLODIndex,
            transform,
            viewer,
            material,
            grassMaterial,
            grassSettings,
            position
        );

        terrainChunkDictionary.Add(coord, newChunk);
        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
        newChunk.Load();
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
            visibleTerrainChunks.Add(chunk);
        else
            visibleTerrainChunks.Remove(chunk);
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold => visibleDstThreshold * visibleDstThreshold;
}