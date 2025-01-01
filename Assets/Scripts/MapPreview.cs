﻿using UnityEngine;
using System.Collections;

public class MapPreview : MonoBehaviour {

    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode {NoiseMap, Mesh, FalloffMap, HeatMap, MoistureMap, BiomeMap};
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public HeatMapSettings heatMapSettings;
    public MoistureSettings moistureMapSettings;
    public BiomeSettings biomeSettings;
    public TextureData textureData;

    public Material terrainMaterial;
    public Material heatMapMaterial;
    public Material heatDebugMaterial;
    public Material moistureDebugMaterial;
    public Material biomeMaterial;

    [Range(0,MeshSettings.numSupportedLODs-1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    public void DrawMapInEditor() {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        // Create instances and copy the original settings
        HeightMapSettings previewMapSettings = ScriptableObject.CreateInstance<HeightMapSettings>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(heightMapSettings), previewMapSettings);
        HeatMapSettings previewHeatSettings = ScriptableObject.CreateInstance<HeatMapSettings>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(heatMapSettings), previewHeatSettings);
        MoistureSettings previewMoistureSettings = ScriptableObject.CreateInstance<MoistureSettings>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(moistureMapSettings), previewMoistureSettings);

        // Adjust the scale in our copy while leaving original unchanged
        previewMapSettings.noiseSettings.scale = heightMapSettings.noiseSettings.scale / 3;
        
        // Generate maps
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, previewMapSettings, Vector2.zero);
        HeatMap heatMap = HeatMapGenerator.GenerateHeatmap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, previewHeatSettings, Vector2.zero, heightMap);
        MoistureMap moistureMap = MoistureMapGenerator.GenerateMoistureMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, previewMoistureSettings, Vector2.zero, heightMap, heatMap);

        if (drawMode == DrawMode.NoiseMap) {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
            meshRenderer.sharedMaterial = terrainMaterial;
        } 
        else if (drawMode == DrawMode.Mesh) {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, heatMap.values, moistureMap.values, meshSettings, editorPreviewLOD));
            meshRenderer.sharedMaterial = terrainMaterial;
        } 
        //else if (drawMode == DrawMode.FalloffMap) {
        //    DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine),0,1)));
        //    meshRenderer.sharedMaterial = terrainMaterial;
        //} 
        else if (drawMode == DrawMode.HeatMap) {
            DrawTexture(TextureGenerator.TextureFromHeatMap(heatMap));
            meshRenderer.sharedMaterial = heatDebugMaterial;
            
            if (heatDebugMaterial != null) {
                heatDebugMaterial.SetFloat("_HeatMin", heatMap.minValue);
                heatDebugMaterial.SetFloat("_HeatMax", heatMap.maxValue);
            }
        }
        else if (drawMode == DrawMode.BiomeMap) {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, heatMap.values, moistureMap.values, meshSettings, editorPreviewLOD));
            meshRenderer.sharedMaterial = biomeMaterial;

            if (biomeMaterial != null && biomeSettings != null) {
                biomeSettings.ApplyToMaterial(biomeMaterial);
            }
        }
    }

    public void DrawTexture(Texture2D texture) {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        textureRender.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData) {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    void OnValidate() {
        if (meshSettings != null) {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
