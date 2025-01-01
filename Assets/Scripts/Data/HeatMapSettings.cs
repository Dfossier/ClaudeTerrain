using System.Collections;
using System.Collections.Generic;
// HeatmapSettings.cs
using UnityEngine;

[CreateAssetMenu()]
public class HeatMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    public int equatorVertex = 125;
    public float latitudeTemperatureFactor = .05f;
    public bool useHeightInfluence = true;
    public float equatorScale = 0.7f;
    public int maxTileDepth = 4;
    public float meshWorldSize = 125;

    public float minTemperature
    {
        get { return 0f; }
    }

    public float maxTemperature
    {
        get { return 1f; }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif
}