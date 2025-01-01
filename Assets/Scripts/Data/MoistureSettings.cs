using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MoistureSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    public int maxTileDepth = 4;
    public float meshWorldSize = 125;
    public float scale = 0.3f;

    public float minMoisture
    {
        get { return 0f; }
    }

    public float maxMoisture
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
