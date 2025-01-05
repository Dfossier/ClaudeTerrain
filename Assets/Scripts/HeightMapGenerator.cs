using UnityEngine;



public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int depth, HeightMapSettings settings, Vector2 sampleCenter)
    {
        // 1. Generate base terrain
        float[,] heightMap = Noise.GenerateNoiseMap(width, depth, settings.noiseSettings, sampleCenter);

        float[,] finalHeightMap = heightMap;
        AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

        // 2. Apply water bodies if enabled
        if (settings.useFalloff)
        {
            finalHeightMap = FalloffGenerator.ApplyWaterBodies(
                heightMap,
                settings.waterLevel,
                sampleCenter,
                settings.maxTileWidth,
                width
            );
        }

        // 3. Apply height curve and track value range
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int y = 0; y < depth; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float heightValue = finalHeightMap[x, y];

                // Preserve water depths but apply curve to terrain
                //if (heightValue > settings.waterLevel)
                //{
                    heightValue = heightCurve_threadsafe.Evaluate(heightValue);
                //}

                // Apply height multiplier
                heightValue *= settings.heightMultiplier;

                finalHeightMap[x, y] = heightValue;
                minValue = Mathf.Min(minValue, heightValue);
                maxValue = Mathf.Max(maxValue, heightValue);
            }
        }

        return new HeightMap(finalHeightMap, minValue, maxValue);
    }
}
public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}