Shader "Custom/MoistureDebug"
{
    Properties
    {
        _MoistureMin ("Moisture Min", Float) = 0
        _MoistureMax ("Moisture Max", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float _MoistureMin;
        float _MoistureMax;

        struct Input
        {
            float2 uv_MainTex; // This will receive both heat (u) and moisture (v) values
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Use the v coordinate for moisture value
            float moistureValue = IN.uv_MainTex.y;
            
            // Create a color gradient from brown (dry) to blue (wet)
            float3 dry = float3(0.6, 0.4, 0.2);    // Brown for dry
            float3 wet = float3(0.2, 0.4, 0.8);    // Blue for wet
            
            float t = (moistureValue - _MoistureMin) / (_MoistureMax - _MoistureMin);
            o.Albedo = lerp(dry, wet, t);
            o.Metallic = 0;
            o.Smoothness = 0.5;
            o.Alpha = 1;

            // Debug visualization - add some variation based on heat (u coordinate)
            float heatValue = IN.uv_MainTex.x;
            o.Albedo *= (1 - heatValue * 0.2); // Slightly darken based on heat
        }
        ENDCG
    }
    FallBack "Diffuse"
}
