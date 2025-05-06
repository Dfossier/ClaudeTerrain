using UnityEngine;
using System.Collections.Generic;
using System;

public static class RiverGenerator
{
    private const float WORLD_REGION_SIZE = 2000f;
    private const float WORLD_SPACE_TRANSITION = 150f;
    private const float RIVER_NOISE_SCALE = 0.003f;
    private const float RIVER_NOISE_STRENGTH = 0.5f;
    private const float POSITION_EPSILON = 0.001f;
    private const float MIN_RIVER_ELEVATION = 0.4f;
    private const float RIVER_DEPTH = 35f;
    private const float RIVER_WIDTH = 50f;
    private const int RIVERS_PER_REGION = 3;

    private class RiverPath
    {
        public Vector2 source;
        public List<Vector2> points;
        public float flowVolume;

        public RiverPath(Vector2 source)
        {
            this.source = source;
            this.points = new List<Vector2> { source };
            this.flowVolume = 1f;
        }
    }

    public static float[,] ApplyRivers(
        float[,] heightMap,
        float waterLevel,
        Vector2 sampleCenter,
        float maxTileWidth,
        float tileWorldSize)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float[,] result = (float[,])heightMap.Clone();

        // Calculate world-space positions
        Vector2 worldOrigin = new Vector2(
            Mathf.Floor(sampleCenter.x / WORLD_REGION_SIZE) * WORLD_REGION_SIZE,
            Mathf.Floor(sampleCenter.y / WORLD_REGION_SIZE) * WORLD_REGION_SIZE
        );

        Vector2 tileTopLeft = new Vector2(
            sampleCenter.x - (tileWorldSize * 0.5f),
            sampleCenter.y + (tileWorldSize * 0.5f)
        );

        // Calculate maximum influence distances
        float maxInfluenceDistance = WORLD_REGION_SIZE;
        int regionsToCheck = Mathf.CeilToInt((maxInfluenceDistance * 2) / WORLD_REGION_SIZE) + 2;

        // Generate and process rivers for current and neighboring regions
        List<RiverPath> allRivers = new List<RiverPath>();

        for (int xOffset = -regionsToCheck; xOffset <= regionsToCheck; xOffset++)
        {
            for (int yOffset = -regionsToCheck; yOffset <= regionsToCheck; yOffset++)
            {
                Vector2 regionCenter = worldOrigin + new Vector2(
                    WORLD_REGION_SIZE * xOffset,
                    WORLD_REGION_SIZE * yOffset
                );

                var rivers = GenerateRegionRivers(
                    regionCenter,
                    heightMap,
                    waterLevel,
                    tileTopLeft,
                    tileWorldSize,
                    width,
                    height
                );

                allRivers.AddRange(rivers);
            }
        }

        // Apply river depressions to heightmap
        float vertexSpacing = tileWorldSize / (width - 1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 worldPos = new Vector2(
                    tileTopLeft.x + (x * vertexSpacing),
                    tileTopLeft.y - (y * vertexSpacing)
                );

                // Snap position to grid
                worldPos = new Vector2(
                    Mathf.Round(worldPos.x / POSITION_EPSILON) * POSITION_EPSILON,
                    Mathf.Round(worldPos.y / POSITION_EPSILON) * POSITION_EPSILON
                );

                float riverInfluence = CalculateRiverInfluence(worldPos, allRivers);
                if (riverInfluence > 0)
                {
                    result[x, y] = Mathf.Lerp(result[x, y], waterLevel - RIVER_DEPTH, riverInfluence);
                }
            }
        }

        return result;
    }

    private static List<RiverPath> GenerateRegionRivers(
        Vector2 regionCenter,
        float[,] heightMap,
        float waterLevel,
        Vector2 tileTopLeft,
        float tileWorldSize,
        int mapWidth,
        int mapHeight)
    {
        List<RiverPath> rivers = new List<RiverPath>();

        // Use deterministic random based on region position
        System.Random rand = new System.Random(
            HashCode.Combine((int)regionCenter.x, (int)regionCenter.y)
        );

        // Generate source points for this region
        for (int i = 0; i < RIVERS_PER_REGION; i++)
        {
            float angleNoise = (float)rand.NextDouble() * Mathf.PI * 2;
            float distNoise = (float)rand.NextDouble() * 0.3f + 0.2f; // 20-50% from center

            Vector2 offset = new Vector2(
                Mathf.Cos(angleNoise) * WORLD_REGION_SIZE * distNoise,
                Mathf.Sin(angleNoise) * WORLD_REGION_SIZE * distNoise
            );

            Vector2 source = regionCenter + offset;

            // Snap to grid for consistency
            source = new Vector2(
                Mathf.Round(source.x / POSITION_EPSILON) * POSITION_EPSILON,
                Mathf.Round(source.y / POSITION_EPSILON) * POSITION_EPSILON
            );

            var river = new RiverPath(source);
            TraceRiverPath(river, heightMap, waterLevel, tileTopLeft, tileWorldSize, mapWidth, mapHeight);
            rivers.Add(river);
        }

        return rivers;
    }

    private static void TraceRiverPath(
        RiverPath river,
        float[,] heightMap,
        float waterLevel,
        Vector2 tileTopLeft,
        float tileWorldSize,
        int mapWidth,
        int mapHeight)
    {
        Vector2 current = river.source;
        float flowVolume = 1f;
        int steps = 0;
        const int MAX_STEPS = 100;
        float vertexSpacing = tileWorldSize / (mapWidth - 1);

        while (steps++ < MAX_STEPS)
        {
            // Convert world position to heightmap coordinates
            int x = Mathf.RoundToInt((current.x - tileTopLeft.x) / vertexSpacing);
            int y = Mathf.RoundToInt((tileTopLeft.y - current.y) / vertexSpacing);

            // If outside heightmap, use perlin noise for height approximation
            float currentHeight;
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
            {
                currentHeight = heightMap[x, y];
            }
            else
            {
                // Use perlin noise for height outside heightmap
                float noiseScale = 0.001f;
                currentHeight = Mathf.PerlinNoise(
                    current.x * noiseScale,
                    current.y * noiseScale
                );
            }

            // Stop if we've reached water level
            if (currentHeight <= waterLevel)
                break;

            // Sample heights in 8 directions
            Vector2 lowestPoint = current;
            float lowestHeight = currentHeight;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 testPoint = current + dir * vertexSpacing;

                // Convert to heightmap coordinates
                int testX = Mathf.RoundToInt((testPoint.x - tileTopLeft.x) / vertexSpacing);
                int testY = Mathf.RoundToInt((tileTopLeft.y - testPoint.y) / vertexSpacing);

                float testHeight;
                if (testX >= 0 && testX < mapWidth && testY >= 0 && testY < mapHeight)
                {
                    testHeight = heightMap[testX, testY];
                }
                else
                {
                    float noiseScale = 0.001f;
                    testHeight = Mathf.PerlinNoise(
                        testPoint.x * noiseScale,
                        testPoint.y * noiseScale
                    );
                }

                if (testHeight < lowestHeight)
                {
                    lowestHeight = testHeight;
                    lowestPoint = testPoint;
                }
            }

            if (lowestPoint == current)
                break;

            river.points.Add(lowestPoint);
            current = lowestPoint;
            river.flowVolume += 0.1f;
        }
    }

    private static float CalculateRiverInfluence(Vector2 point, List<RiverPath> rivers)
    {
        float totalInfluence = 0f;

        foreach (var river in rivers)
        {
            for (int i = 1; i < river.points.Count; i++)
            {
                Vector2 start = river.points[i - 1];
                Vector2 end = river.points[i];

                float distToSegment = DistanceToLineSegment(point, start, end);
                float riverWidth = RIVER_WIDTH * (1f + river.flowVolume * 0.2f);

                if (distToSegment < riverWidth + WORLD_SPACE_TRANSITION)
                {
                    float t = 1f - Mathf.Clamp01(distToSegment / (riverWidth + WORLD_SPACE_TRANSITION));
                    totalInfluence = Mathf.Max(totalInfluence, t);
                }
            }
        }

        return totalInfluence;
    }

    private static float DistanceToLineSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 line = end - start;
        float len = line.magnitude;
        if (len == 0f) return Vector2.Distance(point, start);

        float t = Mathf.Clamp01(Vector2.Dot(point - start, line) / (len * len));
        Vector2 projection = start + line * t;
        return Vector2.Distance(point, projection);
    }
}