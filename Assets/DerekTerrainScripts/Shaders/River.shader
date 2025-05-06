Shader "Custom/RiverWater"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 0.5, 0.7, 0.9)
        _MainTex ("Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _Cubemap ("Cubemap", CUBE) = "" {}
        _FlowSpeed ("Flow Speed", Range(0.1, 5.0)) = 1.0
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.2
        _WaveLength ("Wave Length", Range(1, 10)) = 2
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0.1
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5
        _Transparency ("Transparency", Range(0, 1)) = 0.8
        _FlowDirection ("Flow Direction", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
            float3 worldPos;
            INTERNAL_DATA
        };

        sampler2D _MainTex;
        samplerCUBE _Cubemap;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        float _FlowSpeed;
        float _WaveHeight;
        float _WaveLength;
        float _FresnelPower;
        float _Transparency;
        float4 _FlowDirection;

        // Simplified wave function for rivers
        float3 RiverWave(float3 p, float2 direction)
        {
            float k = 2 * UNITY_PI / _WaveLength;
            float speed = _FlowSpeed;
            float2 d = normalize(direction);
            float f = k * (dot(d, p.xz) - speed * _Time.y);
            float a = _WaveHeight / k;
            
            return float3(
                d.x * (a * cos(f)),
                a * sin(f) * 0.5, // Reduced vertical displacement
                d.y * (a * cos(f))
            );
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate flow direction based on UV coordinates
            float2 flowDir = normalize(_FlowDirection.xy);
            float3 p = float3(IN.uv_MainTex.x, 0, IN.uv_MainTex.y) * _WaveLength;
            
            // Add two waves with different frequencies
            float3 disp = RiverWave(p, flowDir);
            disp += RiverWave(p * 1.4, flowDir) * 0.5;

            // Animate normal map along flow direction
            float2 uv = IN.uv_MainTex;
            float2 flowOffset = flowDir * _Time.y * _FlowSpeed;
            
            // Two-layer normal mapping for more detail
            float3 normal1 = UnpackNormal(tex2D(_MainTex, uv + flowOffset));
            float3 normal2 = UnpackNormal(tex2D(_MainTex, uv * 1.4 + flowOffset * 0.7));
            float3 normal = normalize(normal1 + normal2 * 0.5);

            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(normal, IN.viewDir)), _FresnelPower);

            // Cubemap reflection
            float3 worldViewDir = normalize(UnityWorldSpaceViewDir(IN.worldPos));
            float3 worldNormal = WorldNormalVector(IN, normal);
            float3 worldRefl = reflect(-worldViewDir, worldNormal);
            float4 reflection = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl);

            // Final color
            o.Albedo = _Color.rgb;
            o.Normal = normal;
            o.Emission = reflection.rgb * fresnel * _Color.rgb * 0.5; // Reduced reflection intensity
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a * _Transparency;
        }
        ENDCG
    }
    FallBack "Diffuse"
}