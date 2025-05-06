using UnityEngine;

public static class MoistureMapGenerator
{
    public static MoistureMap GenerateMoistureMap(int width, int depth, MoistureSettings settings, Vector2 sampleCentre, HeightMap heightMap, HeatMap heatMap)
    {
        float[,] values = Noise.GenerateNoiseMap(width, depth, settings.noiseSettings, sampleCentre);
        
        // First pass - calculate raw moisture
        float minMoisture = float.MaxValue;
        float maxMoisture = float.MinValue;

        for (int y = 0; y < depth; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Base moisture from height (higher elevation = less moisture)
                float heightInfluence = 1.0f - heightMap.values[x, y]/25;
                
                // Temperature influence (hotter areas tend to be drier)
                float tempInfluence = 1.0f - heatMap.values[x, y];
                
                // Add noise variation for local moisture differences
                float noiseX = (x + sampleCentre.x) * settings.noiseSettings.scale;
                float noiseY = (y + sampleCentre.y) * settings.noiseSettings.scale;
                float noise = Mathf.PerlinNoise(noiseX, noiseY);
                
                // Combine factors:
                // - Height: 40% influence (higher = drier)
                // - Temperature: 40% influence (hotter = drier)
                // - Noise: 20% influence (local variations)
                float rawMoisture = Mathf.Clamp01((heightInfluence * 0.4f) + (tempInfluence * 0.0f) + values[x, y]*0.6f);
                
                values[x, y] = rawMoisture;
                minMoisture = Mathf.Min(minMoisture, rawMoisture);
                maxMoisture = Mathf.Max(maxMoisture, rawMoisture);
            }
        }

        return new MoistureMap(values, minMoisture, maxMoisture);
    }
}

public struct MoistureMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public MoistureMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
