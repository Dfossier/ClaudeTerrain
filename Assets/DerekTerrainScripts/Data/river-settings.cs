using UnityEngine;

[CreateAssetMenu()]
public class RiverSettings : ScriptableObject
{
    [Header("River Generation")]
    public float minRiverElevation = 0.4f;
    public float riverDensity = 1f;
    public float meanderStrength = 0.5f;
    
    [Header("River Properties")]
    public float baseRiverWidth = 1f;
    public float baseRiverDepth = 0.3f;
    public AnimationCurve riverProfile = new AnimationCurve(
        new Keyframe(0, 1),
        new Keyframe(0.5f, 0.5f),
        new Keyframe(1, 0)
    );

    [Header("Flow Settings")]
    public float minSlopeForFlow = 0.01f;
    public float flowAccumulation = 1f;
    public bool createLakes = true;
}