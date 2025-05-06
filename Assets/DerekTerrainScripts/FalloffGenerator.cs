using UnityEngine;
using System.Collections.Generic;
using System;

public static class FalloffGenerator
{
    private const int NUM_SMALL_SHAPES = 5;
    private const float WORLD_REGION_SIZE = 1250f;
    private const float WORLD_SPACE_TRANSITION = 150f; // Fixed world-space transition width
    private const float SHORELINE_NOISE_SCALE = 0.003f;
    private const float SHORELINE_NOISE_STRENGTH = 0.5f;
    private const float POSITION_EPSILON = 0.001f; // For position snapping

    private class ShapeData
    {
        public Vector2 center;
        public Vector2 radii;
        public float rotation;
        public float distortion;

        public ShapeData(Vector2 center, Vector2 radii, float rotation, float distortion)
        {
            this.center = center;
            this.radii = radii;
            this.rotation = rotation;
            this.distortion = distortion;
        }
    }

    private static int GetStableHashCode(Vector2 position)
    {
        // Convert the floating point coordinates to integers in a way that preserves their structure
        int x = (int)(position.x * 100); // Multiply by 100 to preserve 2 decimal places
        int y = (int)(position.y * 100);

        // Use a simple but effective combining function
        return x * 31 + y;
    }

    public static float[,] ApplyWaterBodies(
        float[,] heightMap,
        float waterLevel,
        Vector2 sampleCenter,
        float maxTileWidth,
        float tileWorldSize)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // Calculate maximum influence distances in world space
        float maxShapeRadius = WORLD_REGION_SIZE * 0.4f;
        float offsetDistance = WORLD_REGION_SIZE * 0.2f;
        float noiseDistortion = maxShapeRadius * SHORELINE_NOISE_STRENGTH;
        float maxInfluenceDistance = maxShapeRadius + offsetDistance + noiseDistortion + WORLD_SPACE_TRANSITION;
        int regionsToCheck = Mathf.CeilToInt((maxInfluenceDistance * 2) / WORLD_REGION_SIZE) + 2;

        Vector2 worldOrigin = new Vector2(
            Mathf.Floor(sampleCenter.x / WORLD_REGION_SIZE) * WORLD_REGION_SIZE,
            Mathf.Floor(sampleCenter.y / WORLD_REGION_SIZE) * WORLD_REGION_SIZE
        );

        Vector2 tileTopLeft = new Vector2(
            sampleCenter.x - (tileWorldSize * 0.5f),
            sampleCenter.y + (tileWorldSize * 0.5f)
        );

        Vector2 regionCenter = new Vector2(
            Mathf.Floor(sampleCenter.x / WORLD_REGION_SIZE) * WORLD_REGION_SIZE,
            Mathf.Floor(sampleCenter.y / WORLD_REGION_SIZE) * WORLD_REGION_SIZE
        );

        float[,] result = new float[width, height];
        List<ShapeData> shapes = new List<ShapeData>();

        // Use constant seed for testing
        int regionSeed = GetStableHashCode(regionCenter);
        System.Random rand = new System.Random(regionSeed);

        // Main shape for current region
        Vector2 mainRadii = new Vector2(
            WORLD_REGION_SIZE * 0.4f,
            WORLD_REGION_SIZE * 0.3f
        );

        Vector2 mainCenter = regionCenter + new Vector2(
            WORLD_REGION_SIZE * 0.5f + WORLD_REGION_SIZE * ((float)rand.NextDouble() - 0.5f) * 0.2f,
            WORLD_REGION_SIZE * 0.5f + WORLD_REGION_SIZE * ((float)rand.NextDouble() - 0.5f) * 0.2f
        );

        shapes.Add(new ShapeData(
            mainCenter,
            mainRadii,
            (float)rand.NextDouble() * Mathf.PI * 2,
            SHORELINE_NOISE_STRENGTH
        ));

        // Small shapes for current region
        for (int i = 0; i < NUM_SMALL_SHAPES; i++)
        {
            Vector2 center = regionCenter + new Vector2(
                WORLD_REGION_SIZE * (float)rand.NextDouble(),
                WORLD_REGION_SIZE * (float)rand.NextDouble()
            );

            float size = WORLD_REGION_SIZE * (0.05f + (float)rand.NextDouble() * 0.15f);
            Vector2 radii = new Vector2(
                size,
                size * (0.5f + (float)rand.NextDouble() * 0.5f)
            );

            shapes.Add(new ShapeData(
                center,
                radii,
                (float)rand.NextDouble() * Mathf.PI * 2,
                SHORELINE_NOISE_STRENGTH * 0.8f
            ));
        }

        // Process neighboring regions
        for (int xOffset = -regionsToCheck; xOffset <= regionsToCheck; xOffset++)
        {
            for (int yOffset = -regionsToCheck; yOffset <= regionsToCheck; yOffset++)
            {
                if (xOffset == 0 && yOffset == 0) continue;

                Vector2 neighborRegionCenter = regionCenter + new Vector2(
                    WORLD_REGION_SIZE * xOffset,
                    WORLD_REGION_SIZE * yOffset
                );

                // Use same constant seed for neighbors
                int neighborSeed = GetStableHashCode(neighborRegionCenter);
                System.Random neighborRand = new System.Random(neighborSeed);

                Vector2 neighborMainCenter = neighborRegionCenter + new Vector2(
                    WORLD_REGION_SIZE * 0.5f + WORLD_REGION_SIZE * ((float)neighborRand.NextDouble() - 0.5f) * 0.2f,
                    WORLD_REGION_SIZE * 0.5f + WORLD_REGION_SIZE * ((float)neighborRand.NextDouble() - 0.5f) * 0.2f
                );

                float distanceToTile = Vector2.Distance(neighborMainCenter, sampleCenter);

                if (distanceToTile < maxInfluenceDistance + WORLD_REGION_SIZE)
                {
                    shapes.Add(new ShapeData(
                        neighborMainCenter,
                        mainRadii,
                        (float)neighborRand.NextDouble() * Mathf.PI * 2,
                        SHORELINE_NOISE_STRENGTH
                    ));

                    if (distanceToTile < maxInfluenceDistance + WORLD_REGION_SIZE * 0.5f)
                    {
                        for (int i = 0; i < NUM_SMALL_SHAPES; i++)
                        {
                            Vector2 center = neighborRegionCenter + new Vector2(
                                WORLD_REGION_SIZE * (float)neighborRand.NextDouble(),
                                WORLD_REGION_SIZE * (float)neighborRand.NextDouble()
                            );

                            float size = WORLD_REGION_SIZE * (0.05f + (float)neighborRand.NextDouble() * 0.15f);
                            Vector2 radii = new Vector2(
                                size,
                                size * (0.5f + (float)neighborRand.NextDouble() * 0.5f)
                            );

                            shapes.Add(new ShapeData(
                                center,
                                radii,
                                (float)neighborRand.NextDouble() * Mathf.PI * 2,
                                SHORELINE_NOISE_STRENGTH * 0.8f
                            ));
                        }
                    }
                }
            }
        }

        // Process neighboring regions
        for (int xOffset = -regionsToCheck; xOffset <= regionsToCheck; xOffset++)
        {
            for (int yOffset = -regionsToCheck; yOffset <= regionsToCheck; yOffset++)
            {
                if (xOffset == 0 && yOffset == 0) continue;

                Vector2 neighborRegionCenter = regionCenter + new Vector2(
                    WORLD_REGION_SIZE * xOffset,
                    WORLD_REGION_SIZE * yOffset
                );

                // Use stable seed for neighbor
                int neighborSeed = GetStableHashCode(neighborRegionCenter);
                System.Random neighborRand = new System.Random(neighborSeed);

                Vector2 neighborMainCenter = neighborRegionCenter + new Vector2(
                    WORLD_REGION_SIZE * 0.5f + WORLD_REGION_SIZE * ((float)neighborRand.NextDouble() - 0.5f) * 0.2f,
                    WORLD_REGION_SIZE * 0.5f + WORLD_REGION_SIZE * ((float)neighborRand.NextDouble() - 0.5f) * 0.2f
                );

                // Snap neighbor main center
                neighborMainCenter = new Vector2(
                    Mathf.Round(neighborMainCenter.x / POSITION_EPSILON) * POSITION_EPSILON,
                    Mathf.Round(neighborMainCenter.y / POSITION_EPSILON) * POSITION_EPSILON
                );

                float distanceToTile = Vector2.Distance(neighborMainCenter, sampleCenter);

                if (distanceToTile < maxInfluenceDistance + WORLD_REGION_SIZE)
                {
                    shapes.Add(new ShapeData(
                        neighborMainCenter,
                        mainRadii,
                        (float)neighborRand.NextDouble() * Mathf.PI * 2,
                        SHORELINE_NOISE_STRENGTH
                    ));

                    if (distanceToTile < maxInfluenceDistance + WORLD_REGION_SIZE * 0.5f)
                    {
                        for (int i = 0; i < NUM_SMALL_SHAPES; i++)
                        {
                            Vector2 center = neighborRegionCenter + new Vector2(
                                WORLD_REGION_SIZE * (float)neighborRand.NextDouble(),
                                WORLD_REGION_SIZE * (float)neighborRand.NextDouble()
                            );

                            // Snap neighbor small shape centers
                            center = new Vector2(
                                Mathf.Round(center.x / POSITION_EPSILON) * POSITION_EPSILON,
                                Mathf.Round(center.y / POSITION_EPSILON) * POSITION_EPSILON
                            );

                            float size = WORLD_REGION_SIZE * (0.05f + (float)neighborRand.NextDouble() * 0.15f);
                            Vector2 radii = new Vector2(
                                size,
                                size * (0.5f + (float)neighborRand.NextDouble() * 0.5f)
                            );

                            shapes.Add(new ShapeData(
                                center,
                                radii,
                                (float)neighborRand.NextDouble() * Mathf.PI * 2,
                                SHORELINE_NOISE_STRENGTH * 0.8f
                            ));
                        }
                    }
                }
            }
        }

        // Process heightmap
        float vertexSpacing = tileWorldSize / (width - 1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate precise world position
                Vector2 worldPos = new Vector2(
                    tileTopLeft.x + (x * vertexSpacing),
                    tileTopLeft.y - (y * vertexSpacing)
                );

                // Snap position to grid to ensure consistency across tiles
                worldPos = new Vector2(
                    Mathf.Round(worldPos.x / POSITION_EPSILON) * POSITION_EPSILON,
                    Mathf.Round(worldPos.y / POSITION_EPSILON) * POSITION_EPSILON
                );

                float currentHeight = heightMap[x, y];
                float minDistance = float.MaxValue;

                foreach (var shape in shapes)
                {
                    float distance = DistanceToShape(worldPos, shape);
                    minDistance = Mathf.Min(minDistance, distance);
                }

                float t = Mathf.Clamp01(minDistance / WORLD_SPACE_TRANSITION);
                result[x, y] = Mathf.Lerp(0, currentHeight, t);
            }
        }

        return result;
    }

    private static float DistanceToShape(Vector2 point, ShapeData shape)
    {
        // Transform point to shape's local space
        Vector2 localPoint = point - shape.center;
        float cos = Mathf.Cos(-shape.rotation);
        float sin = Mathf.Sin(-shape.rotation);
        Vector2 rotatedPoint = new Vector2(
            localPoint.x * cos - localPoint.y * sin,
            localPoint.x * sin + localPoint.y * cos
        );

        // Calculate noise based on world position instead of shape center
        float angle = Mathf.Atan2(rotatedPoint.y, rotatedPoint.x);
        float noise = Mathf.PerlinNoise(
            point.x * SHORELINE_NOISE_SCALE + Mathf.Cos(angle) * 3,
            point.y * SHORELINE_NOISE_SCALE + Mathf.Sin(angle) * 3
        );

        Vector2 distortedRadii = shape.radii * (1 + noise * shape.distortion);

        // Calculate distance to oval edge
        Vector2 normalizedPoint = new Vector2(
            rotatedPoint.x / distortedRadii.x,
            rotatedPoint.y / distortedRadii.y
        );

        float distance = normalizedPoint.magnitude - 1;
        return distance * (Mathf.Min(distortedRadii.x, distortedRadii.y));
    }
}