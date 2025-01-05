using UnityEngine;

[CreateAssetMenu()]
public class GrassSettings : ScriptableObject
{
    [Header("Color Settings")]
    public Color healthyColor = new Color(0.2f, 0.8f, 0.2f);
    public Color dryColor = new Color(0.8f, 0.8f, 0.2f);
    [Range(0, 1)]
    public float colorVariation = 0.4f;

    [Header("Blade Settings")]
    [Range(0, 5)]
    public float minWidth = 0.05f;
    [Range(0, 5)]
    public float maxWidth = 0.1f;
    [Range(0, 5)]
    public float minHeight = 0.5f;
    [Range(0, 5)]
    public float maxHeight = 1.0f;
    [Range(0, 1)]
    public float noiseSpread = 0.1f;
    [Range(0, 1)]
    public float minDensity = 0.3f;
    [Range(0, 1)]
    public float maxDensity = 0.8f;

    [Header("Placement Settings")]
    [Range(0, 1)]
    public float moistureThreshold = 0.3f;
    [Range(0, 1)]
    public float steepnessThreshold = 0.7f;
    [Range(0, 1)]
    public float heightInfluence = 0.5f;

    [Header("Wind Settings")]
    [Range(0, 2)]
    public float windStrength = 0.5f;
    [Range(0, 2)]
    public float windSpeed = 0.5f;
    [Range(0, 2)]
    public float windFrequency = 0.5f;
    [Range(0, 1)]
    public float microDetailStrength = 0.2f;

    [Header("LOD Settings")]
    [Range(20, 200)]
    public float maxDrawDistance = 100f;
    [Range(0, 1)]
    public float lodTransitionSpeed = 0.5f;
    [Range(0, 1)]
    public float densityFalloff = 0.8f;

    [Header("Lighting Settings")]
    [Range(0, 1)]
    public float ambientOcclusion = 0.5f;
    [Range(0, 1)]
    public float shadowSoftness = 0.5f;
    [Range(0, 1)]
    public float rimLightIntensity = 0.3f;

    [Header("Clumping Settings")]
    [Range(0.1f, 10f)]
    public float clumpScale = 2.0f;
    [Range(0, 1)]
    public float clumpSpread = 0.7f;
}
