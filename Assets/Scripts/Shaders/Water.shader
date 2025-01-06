Shader "Custom/RealisticWater"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 0.5, 0.7, 0.9)
        _MainTex ("Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _Cubemap ("Cubemap", CUBE) = "" {}
        _WaveSpeed ("Wave Speed", Range(0.1, 5.0)) = 0.05
        _WaveHeight ("Wave Height", Range(0, 2)) = 0.5
        _WaveLength ("Wave Length", Range(1, 10)) = 2
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0.1
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5
        _Transparency ("Transparency", Range(0, 1)) = 0.8
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
        float _WaveSpeed;
        float _WaveHeight;
        float _WaveLength;
        float _FresnelPower;
        float _Transparency;

        // Wave function
        float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
        {
            float steepness = wave.z;
            float wavelength = wave.w;
            float k = 2 * UNITY_PI / wavelength;
            float c = sqrt(9.8 / k);
            float2 d = normalize(wave.xy);
            float f = k * (dot(d, p.xz) - c * _Time.y);
            float a = steepness / k;
            
            tangent += float3(
                -d.x * d.x * steepness * sin(f),
                d.x * steepness * cos(f),
                -d.x * d.y * steepness * sin(f)
            );
            
            binormal += float3(
                -d.x * d.y * steepness * sin(f),
                d.y * steepness * cos(f),
                -d.y * d.y * steepness * sin(f)
            );
            
            return float3(
                d.x * (a * cos(f)),
                a * sin(f),
                d.y * (a * cos(f))
            );
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate waves
            float3 tangent = float3(1, 0, 0);
            float3 binormal = float3(0, 0, 1);
            float3 p = float3(IN.uv_MainTex.x, 0, IN.uv_MainTex.y) * _WaveLength;
            
            float3 disp = GerstnerWave(
                float4(1, 1, _WaveHeight, _WaveLength),
                p,
                tangent,
                binormal
            );
            
            disp += GerstnerWave(
                float4(1, 0.6, _WaveHeight * 0.7, _WaveLength * 0.7),
                p,
                tangent,
                binormal
            );

            // Animate normal map
            float2 uv = IN.uv_MainTex;
            uv += _Time.y * _WaveSpeed * 0.05;
            float3 normal1 = UnpackNormal(tex2D(_MainTex, uv));
            uv = IN.uv_MainTex + float2(0.5, 0.5);
            uv += _Time.y * _WaveSpeed * -0.05;
            float3 normal2 = UnpackNormal(tex2D(_MainTex, uv));
            float3 normal = normalize(normal1 + normal2);

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
            o.Emission = reflection.rgb * fresnel * _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a * _Transparency;
        }
        ENDCG
    }
    FallBack "Diffuse"
}