Shader "Custom/HeatDebug"
{
    Properties
    {
        _HeatMin ("Heat Min", Float) = 0
        _HeatMax ("Heat Max", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float _HeatMin;
        float _HeatMax;

        struct Input
        {
            float2 uv_MainTex; // This will receive both heat (u) and moisture (v) values
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Use the u coordinate for heat value
            float heatValue = IN.uv_MainTex.x;
            
            // Create a color gradient from blue (cold) to red (hot)
            float3 cold = float3(0, 0, 1);    // Blue for cold
            float3 hot = float3(1, 0, 0);     // Red for hot
            
            float t = (heatValue - _HeatMin) / (_HeatMax - _HeatMin);
            o.Albedo = lerp(cold, hot, t);
            o.Metallic = 0;
            o.Smoothness = 0.5;
            o.Alpha = 1;

            // Debug visualization - add some variation based on moisture (v coordinate)
            float moistureValue = IN.uv_MainTex.y;
            o.Albedo *= (1 - moistureValue * 0.2); // Slightly darken based on moisture
        }
        ENDCG
    }
    FallBack "Diffuse"
}
