using UnityEngine;

public static class HeatMapGenerator
{
    public static HeatMap GenerateHeatmap(int width, int depth, HeatMapSettings settings, Vector2 sampleCentre, HeightMap heightMap)
    {
        float[,] values = new float[width, depth];
        
        // First pass - calculate raw temperatures
        float minTemp = float.MaxValue;
        float maxTemp = float.MinValue;
        int heightmapMultiplier = 110;

        for (int y = 0; y < depth; y++)
        {
            // Normalize y position to -1 to 1 range for latitude calculation
            int worldspaceY = (int)sampleCentre.y + (int)((depth - 1) / 2) - y;
            float distanceEquator = Mathf.Abs(worldspaceY - settings.equatorVertex);
            float eqdistanceScale = distanceEquator/(depth* settings.maxTileDepth/(1+settings.equatorScale));

            for (int x = 0; x < width; x++)
            {
                // Base temperature from latitude (equator is hottest)
                float latitudeTemp = 1.0f - Mathf.Abs(eqdistanceScale);
                
                // Add height-based temperature variation (higher = colder)
                float heightPercent = heightMap.values[x, y]/heightmapMultiplier;
                float heightTemp = 1.0f - heightPercent;
                
                // Add noise variation for local climate differences
                float noiseX = (x + sampleCentre.x) * settings.noiseSettings.scale;
                float noiseY = (y + sampleCentre.y) * settings.noiseSettings.scale;
                float noise = Mathf.PerlinNoise(noiseX, noiseY);
                
                // Combine factors:
                // - Latitude: 60% influence (most important)
                // - Height: 30% influence
                // - Noise: 10% influence (subtle variations)
                float rawTemp = (latitudeTemp * 0.5f) + (heightTemp * 0.3f) + (noise * 0.2f);
                
                values[x, y] = rawTemp;
                minTemp = Mathf.Min(minTemp, rawTemp);
                maxTemp = Mathf.Max(maxTemp, rawTemp);
            }
        }
        return new HeatMap(values, 0f, 1f);
    }
}

public struct HeatMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeatMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
