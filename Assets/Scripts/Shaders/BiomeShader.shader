Shader "Custom/BiomeTerrain" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _WaterLevel ("Water Level", Float) = 10
        _WaterColor ("Water Color", Color) = (0.2, 0.4, 0.8, 1)
        _WaterSmoothness ("Water Smoothness", Range(0, 1)) = 0.8
        _DebugMode ("Debug Mode", Int) = 0
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5
        
        const static int maxLayerCount = 9;
        const static float epsilon = 1E-4;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);
        
        float _WaterLevel;
        float4 _WaterColor;
        float _WaterSmoothness;
        int _DebugMode;

        int layerCount;
        float4 baseColours[maxLayerCount];
        float baseTextureScales[maxLayerCount];
        float baseColourStrength[maxLayerCount];
        float baseBlends[maxLayerCount];
        float _MinTemps[maxLayerCount];
        float _MaxTemps[maxLayerCount];
        float _MinMoisture[maxLayerCount];
        float _MaxMoisture[maxLayerCount];
        
        struct Input {
            float3 worldPos;
            float3 worldNormal;
            float2 uv_MainTex;
        };

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        float getBiomeStrength(float temperature, float moisture, int index) {
            float tempStrength = 1;
            float moistStrength = 1;
            
            // Temperature blend
            if(temperature < _MinTemps[index]) {
                tempStrength = 1 - saturate((_MinTemps[index] - temperature) / (baseBlends[index] + epsilon));
            }
            else if(temperature > _MaxTemps[index]) {
                tempStrength = 1 - saturate((temperature - _MaxTemps[index]) / (baseBlends[index] + epsilon));
            }
            
            // Moisture blend
            if(moisture < _MinMoisture[index]) {
                moistStrength = 1 - saturate((_MinMoisture[index] - moisture) / (baseBlends[index] + epsilon));
            }
            else if(moisture > _MaxMoisture[index]) {
                moistStrength = 1 - saturate((moisture - _MaxMoisture[index]) / (baseBlends[index] + epsilon));
            }
            
            return min(tempStrength, moistStrength);
        }

        float3 VisualizeTemperature(float temp) {
            if (temp < 0.5) {
                return lerp(float3(0,0,1), float3(0,1,0), temp * 2);
            } else {
                return lerp(float3(0,1,0), float3(1,0,0), (temp - 0.5) * 2);
            }
        }

        float3 VisualizeMoisture(float moisture) {
            return lerp(float3(1,1,0), float3(0,0,1), moisture);
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float3 color = float3(1, 0, 0);  // Default red for no match
            float smoothness = 0;
            float totalStrength = 0;
            sampler2D _MainTex;
            
            float temperature = IN.uv_MainTex.x;
            float moisture = IN.uv_MainTex.y;

            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            if (_DebugMode == 1) {
                color = VisualizeTemperature(temperature);
                o.Emission = color;
            }
            else if (_DebugMode == 2) {
                color = VisualizeMoisture(moisture);
                o.Emission = color;
            }
            else if (_DebugMode == 3) {
                float3 tempColor = VisualizeTemperature(temperature);
                float3 moistColor = VisualizeMoisture(moisture);
                color = lerp(tempColor, moistColor, 0.5);
                o.Emission = color;
            }
            else {
                if (IN.worldPos.y < _WaterLevel) {
                    color = _WaterColor.rgb;
                    smoothness = _WaterSmoothness;
                }
                else {
                    color = float3(0, 0, 0);
                    float3 totalColor = float3(0, 0, 0);
                    float totalWeight = 0;
                    
                    for(int i = 0; i < layerCount; i++) {
                        float strength = getBiomeStrength(temperature, moisture, i);
                        if(strength > 0) {
                            float3 texColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i);
                            float3 biomeColor = texColor * lerp(float3(1,1,1), baseColours[i].rgb, baseColourStrength[i]);
                            
                            totalColor += biomeColor * strength;
                            totalWeight += strength;
                        }
                    }
                    
                    if(totalWeight > 0) {
                        color = totalColor / totalWeight;
                    }
                }
                o.Emission = 0;
            }
            
            o.Albedo = color;
            o.Alpha = 1.0;
            o.Metallic = 0;
            o.Smoothness = smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}