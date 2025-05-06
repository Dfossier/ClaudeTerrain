Shader "Custom/BiomeColor" {
    Properties {
        _WaterLevel ("Water Level", Float) = 0.1
        _WaterColor ("Water Color", Color) = (0.2, 0.4, 0.8, 1)
        _WaterSmoothness ("Water Smoothness", Range(0, 1)) = 0.8
        _MainTex ("Texture", 2D) = "white" {}
        _DebugMode ("Debug Mode", Int) = 0
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;

        sampler2D _MainTex;
        
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

        bool isInBiomeRange(float temperature, float moisture, int index) {
            bool tempMatch = temperature >= _MinTemps[index] && temperature <= _MaxTemps[index];
            bool moistureMatch = moisture >= _MinMoisture[index] && moisture <= _MaxMoisture[index];
            return tempMatch && moistureMatch;
        }

        float3 VisualizeTemperature(float temp) {
            // Convert temperature to a color gradient
            // Cold (0.0) = Blue
            // Medium (0.5) = Green
            // Hot (1.0) = Red
            if (temp < 0.5) {
                return lerp(float3(0,0,1), float3(0,1,0), temp * 2);
            } else {
                return lerp(float3(0,1,0), float3(1,0,0), (temp - 0.5) * 2);
            }
        }

        float3 VisualizeMoisture(float moisture) {
            // Convert moisture to a color gradient
            // Dry (0.0) = Yellow
            // Wet (1.0) = Blue
            return lerp(float3(1,1,0), float3(0,0,1), moisture);
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float3 color;
            float smoothness = 0;
            bool foundMatch = false;
            
            float temperature = IN.uv_MainTex.x;
            float moisture = IN.uv_MainTex.y;

            if (_DebugMode == 1) {
                // Enhanced temperature visualization
                color = VisualizeTemperature(temperature);
                o.Emission = color;
            }
            else if (_DebugMode == 2) {
                // Enhanced moisture visualization
                color = VisualizeMoisture(moisture);
                o.Emission = color;
            }
            else if (_DebugMode == 3) {
                // Combined temperature and moisture
                float3 tempColor = VisualizeTemperature(temperature);
                float3 moistColor = VisualizeMoisture(moisture);
                color = lerp(tempColor, moistColor, 0.5);
                o.Emission = color;
            }
else if (_DebugMode == 4) {
    // Biome matching debug
    if (IN.worldPos.y < _WaterLevel) {
        color = _WaterColor.rgb;
        smoothness = _WaterSmoothness;
    }
    else {
        color = float3(1, 0, 0);  // Red for no match
        
        if (layerCount <= 0) {
            color = float3(1, 0, 1);  // Purple for invalid layer count
        }
        else {
            for(int i = 0; i < layerCount; i++) {
                if(isInBiomeRange(temperature, moisture, i)) {
                    // Get the base texture color
                    float4 texColor = tex2D(_MainTex, IN.uv_MainTex * baseTextureScales[i]);
                    // Blend with the biome tint color using the configured strength
                    color = lerp(texColor.rgb, baseColours[i].rgb, baseColourStrength[i]);
                    foundMatch = true;
                    break;
                }
            }
        }
    }
    o.Emission = 0;  // Turn off emission since we're showing actual textures
}
            else if (_DebugMode == 5) {
                // Raw values debug
                color = float3(
                    temperature,  // Red = raw temperature
                    moisture,     // Green = raw moisture
                    (float)layerCount / maxLayerCount  // Blue = layer count
                );
                o.Emission = color;
            }
            else {
                // Normal rendering path
                if (IN.worldPos.y < _WaterLevel) {
                    color = _WaterColor.rgb;
                    smoothness = _WaterSmoothness;
                }
                else {
                    float4 texColor = tex2D(_MainTex, IN.uv_MainTex);
                    color = texColor.rgb;

                    for (int i = 0; i < layerCount; i++) {
                        if (isInBiomeRange(temperature, moisture, i)) {
                            color = lerp(color, baseColours[i].rgb, baseColourStrength[i]);
                            foundMatch = true;
                            break;
                        }
                    }

                    if (!foundMatch) {
                        // Use temperature visualization as fallback
                        color = VisualizeTemperature(temperature);
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
