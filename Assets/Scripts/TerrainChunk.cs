﻿﻿﻿using System.Linq;
using UnityEngine;
using System;

public class TerrainChunk
{

    const float colliderGenerationDistanceThreshold = 100;
    const float POSITION_SCALE = 0.99f;
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;
    public Vector2 coord;

    GameObject meshObject;
    Vector2 sampleCentre;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    HeightMap heightMap;
    bool heightMapReceived;
    HeatMap heatMap;
    bool heatMapReceived;
    MoistureMap moistureMap;
    bool moistureMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDst;

    HeightMapSettings heightMapSettings;
    HeatMapSettings heatMapSettings;
    MoistureSettings moistureSettings;
    MeshSettings meshSettings;
    Transform viewer;

    // Add grass-related fields
    private Material grassMaterial;
    private GrassSettings grassSettings;
    private GameObject grassObject;
    private MeshRenderer grassRenderer;
    private MeshFilter grassFilter;
    private TerrainDetailManager detailManager;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, HeatMapSettings heatMapSettings,
        MoistureSettings moistureSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex,
        Transform parent, Transform viewer, Material material, Material grassMaterial, GrassSettings grassSettings, Vector2 position)
    {

        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.heatMapSettings = heatMapSettings;
        this.moistureSettings = moistureSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;
        this.grassMaterial = grassMaterial;
        this.grassSettings = grassSettings;

        sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        //Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;


        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Create grass object
        if (grassMaterial != null)
        {
            grassObject = new GameObject("Grass");
            grassObject.transform.parent = meshObject.transform;
            grassObject.transform.localPosition = Vector3.zero;
            grassObject.transform.localRotation = Quaternion.identity;

            grassRenderer = grassObject.AddComponent<MeshRenderer>();
            grassFilter = grassObject.AddComponent<MeshFilter>();
            grassRenderer.material = new Material(grassMaterial);

            detailManager = grassObject.AddComponent<TerrainDetailManager>();
        }

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }


        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCentre), OnHeightMapReceived);
    }

    void OnHeightMapReceived(object heightMapObject)
    {
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        ThreadedDataRequester.RequestData(() => HeatMapGenerator.GenerateHeatmap(
            meshSettings.numVertsPerLine,
            meshSettings.numVertsPerLine,
            heatMapSettings,
            sampleCentre,
            heightMap
        ), OnHeatMapReceived);
    }

    void OnHeatMapReceived(object heatMapObject)
    {
        this.heatMap = (HeatMap)heatMapObject;
        heatMapReceived = true;

        ThreadedDataRequester.RequestData(() => MoistureMapGenerator.GenerateMoistureMap(
            meshSettings.numVertsPerLine,
            meshSettings.numVertsPerLine,
            moistureSettings,
            sampleCentre,
            heightMap,
            heatMap
        ), OnMoistureMapReceived);
    }

    void OnMoistureMapReceived(object moistureMapObject)
    {
        this.moistureMap = (MoistureMap)moistureMapObject;
        moistureMapReceived = true;

        if (meshRenderer != null)
        {
            UpdateMaterialProperties();
        }

        // Initialize grass detail manager if available
        if (detailManager != null && heightMapReceived && moistureMapReceived)
        {
            detailManager.Initialize(coord, grassRenderer.material, heightMap.values, moistureMap.values, grassSettings);

            // Update grass mesh
            if (grassFilter != null && meshFilter.mesh != null)
            {
                grassFilter.mesh = meshFilter.mesh;
            }
        }

        UpdateTerrainChunk();
    }
    Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        if (heightMapReceived && heatMapReceived && moistureMapReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

            bool wasVisible = IsVisible();
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                        if (grassFilter != null)
                        {
                            grassFilter.mesh = lodMesh.mesh;
                        }
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(heightMap, heatMap, moistureMap, meshSettings);
                    }
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if (onVisibilityChanged != null)
                {
                    onVisibilityChanged(this, visible);
                }
            }
        }
    }

    void UpdateMaterialProperties()
    {
        if (!moistureMapReceived || meshRenderer.material == null) return;

        // Create moisture texture
        int width = moistureMap.values.GetLength(0);
        int height = moistureMap.values.GetLength(1);
        Texture2D moistureTexture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        // Fill texture data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float moistureValue = moistureMap.values[x, y];
                colors[y * width + x] = new Color(moistureValue, moistureValue, moistureValue);
            }
        }

        moistureTexture.SetPixels(colors);
        moistureTexture.Apply();

        // Update terrain material
        Material terrainMat = meshRenderer.material;
        terrainMat.SetTexture("_MoistureMap", moistureTexture);
        terrainMat.SetFloat("_MoistureMin", moistureMap.minValue);
        terrainMat.SetFloat("_MoistureMax", moistureMap.maxValue);

        // Update grass material
        if (grassRenderer != null && grassRenderer.material != null)
        {
            Material grassMat = grassRenderer.material;
            grassMat.SetTexture("_MoistureMap", moistureTexture);
            grassMat.SetVector("_ChunkOffset", new Vector4(meshObject.transform.position.x, 0, meshObject.transform.position.z, 0));
            grassMat.SetVector("_TerrainSize", new Vector4(meshSettings.meshWorldSize, meshSettings.meshWorldSize, 0, 0));
            grassMat.SetFloat("_MoistureMin", moistureMap.minValue);
            grassMat.SetFloat("_MoistureMax", moistureMap.maxValue);
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
            {
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, heatMap, moistureMap, meshSettings);
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
        if (grassObject != null)
        {
            grassObject.SetActive(visible);
        }
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    public event System.Action updateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;
        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, HeatMap heatMap, MoistureMap moistureMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, heatMap.values, moistureMap.values, meshSettings, lod), OnMeshDataReceived);
    }
}
