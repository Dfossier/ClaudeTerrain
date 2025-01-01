Shader "Custom/HeatMap" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UseHeatmap("Use Heatmap", Float) = 1
        _HeatmapBlend("Heatmap Blend", Range(0,1)) = 1
        _ColdColor("Cold Color", Color) = (0,0,1,1)
        _MidColor("Mid Color", Color) = (0,1,0,1)
        _HotColor("Hot Color", Color) = (1,0,0,1)
        _DebugMode("Debug Mode", Int) = 0  // 0=normal, 1=show UV.x, 2=show UV.y
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float _UseHeatmap;
        float _HeatmapBlend;
        float4 _ColdColor;
        float4 _MidColor;
        float4 _HotColor;
        int _DebugMode;

        struct Input
        {
            float2 uv_MainTex;  // Changed to uv for terrain
            float3 worldPos;     // Added for additional debugging
        };

        float3 GetHeatmapColor(float heat) 
        {
            if (heat < 0.5) {
                return lerp(_ColdColor.rgb, _MidColor.rgb, heat * 2);
            } else {
                return lerp(_MidColor.rgb, _HotColor.rgb, (heat - 0.5) * 2);
            }
        }

        sampler2D _MainTex;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 color;

    // Debug visualization modes
    if (_DebugMode == 1) 
    {
        color = float3(IN.uv_MainTex.x, IN.uv_MainTex.x, IN.uv_MainTex.x);
        o.Emission = color * 0.2;
    }
    else if (_DebugMode == 2) 
    {
        color = float3(IN.uv_MainTex.y, IN.uv_MainTex.y, IN.uv_MainTex.y);
        o.Emission = color * 0.2;
    }
    else if (_DebugMode == 3) 
    {
        color.r = IN.uv_MainTex.x;
        color.g = IN.uv_MainTex.y;
        color.b = 1.0 - (IN.uv_MainTex.x * IN.uv_MainTex.y);
        o.Emission = color * 0.2;
    }
    else if (_DebugMode == 4) 
    {
        color = frac(IN.worldPos * 0.1);
        o.Emission = color * 0.2;
    }
    else {
        float heat = IN.uv_MainTex.y;
        color = GetHeatmapColor(heat);
        o.Emission = color * 0.2;
    }
            
            o.Albedo = color;
            o.Alpha = 1.0;
            o.Metallic = 0;
            o.Smoothness = 0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
