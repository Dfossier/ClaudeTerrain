using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    [System.Serializable]
    public struct BiomeLayer
    {
        public string name;
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        public float textureScale;

        [Header("Temperature Range")]
        [Range(0, 1)]
        public float minTemperature;
        [Range(0, 1)]
        public float maxTemperature;

        [Header("Moisture Range")]
        [Range(0, 1)]
        public float minMoisture;
        [Range(0, 1)]
        public float maxMoisture;

        [Header("Blending")]
        [Range(0, 1)]
        public float blendStrength;
    }

    public BiomeLayer[] biomeLayers;
    public float waterLevel = 0.1f;
    public Color waterColor = new Color(0.2f, 0.4f, 0.8f);
    [Range(0, 1)]
    public float waterSmoothness = 0.8f;

    public void ApplyToMaterial(Material material)
    {
        // Basic settings
        material.SetInt("layerCount", biomeLayers.Length);
        material.SetFloat("_WaterLevel", waterLevel);
        material.SetColor("_WaterColor", waterColor);
        material.SetFloat("_WaterSmoothness", waterSmoothness);

        // Layer arrays
        var colors = System.Array.ConvertAll(biomeLayers, x => x.tint);
        var scales = System.Array.ConvertAll(biomeLayers, x => x.textureScale);
        var strengths = System.Array.ConvertAll(biomeLayers, x => x.tintStrength);
        var blends = System.Array.ConvertAll(biomeLayers, x => x.blendStrength);

        material.SetColorArray("baseColours", colors);
        material.SetFloatArray("baseTextureScales", scales);
        material.SetFloatArray("baseColourStrength", strengths);
        material.SetFloatArray("baseBlends", blends);

        // Temperature and moisture ranges
        var minTemps = System.Array.ConvertAll(biomeLayers, x => x.minTemperature);
        var maxTemps = System.Array.ConvertAll(biomeLayers, x => x.maxTemperature);
        var minMoisture = System.Array.ConvertAll(biomeLayers, x => x.minMoisture);
        var maxMoisture = System.Array.ConvertAll(biomeLayers, x => x.maxMoisture);

        material.SetFloatArray("_MinTemps", minTemps);
        material.SetFloatArray("_MaxTemps", maxTemps);
        material.SetFloatArray("_MinMoisture", minMoisture);
        material.SetFloatArray("_MaxMoisture", maxMoisture);

        // Generate and set texture array
        Texture2DArray texturesArray = GenerateTextureArray(System.Array.ConvertAll(biomeLayers, x => x.texture));
        material.SetTexture("baseTextures", texturesArray);
    }

    private Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        const int textureSize = 512;
        const TextureFormat textureFormat = TextureFormat.RGB565;

        if (textures.Any(t => t == null))
        {
            Debug.LogError("One or more textures are null in the biome layers!");
            return null;
        }

        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }
}