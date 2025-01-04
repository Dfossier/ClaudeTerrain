Shader "Custom/TerrainGrassShader" {
    Properties {
        _MainTex ("Terrain Texture", 2D) = "white" {}
        _MoistureMap ("Moisture Map", 2D) = "white" {}
        _GrassColor ("Grass Color", Color) = (0.4,0.7,0.15,1)
        _GrassColorVariation("Color Variation", Range(0,1)) = 0.1
        _GrassHeight ("Grass Height", Range(0,1)) = 0.5
        _GrassWidth ("Grass Width", Range(0,1)) = 0.1
        _GrassBend ("Grass Bend", Range(0,1)) = 0.3
        _WindSpeed ("Wind Speed", Range(0,10)) = 2.0
        _WindStrength ("Wind Strength", Range(0,1)) = 0.3
        _WindFrequency ("Wind Frequency", Range(0,1)) = 0.5
        _GrassDensity ("Grass Density", Range(0,1)) = 0.5
        _ChunkOffset ("Chunk Offset", Vector) = (0,0,0,0)
        _TerrainSize ("Terrain Size", Vector) = (100,100,0,0)
        _MoistureMin ("Moisture Min", Float) = 0
        _MoistureMax ("Moisture Max", Float) = 1
        _MoistureThreshold ("Moisture Threshold", Range(0,1)) = 0.3
        _WaterLevel ("Water Level", Float) = 0.0
        _TessellationUniform ("Tessellation", Range(1, 64)) = 1
    }

    SubShader {
        Tags {"RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2g {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD1;
                float moisture : TEXCOORD2;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float moisture : TEXCOORD2;
                float3 color : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            sampler2D _MainTex, _MoistureMap;
            float4 _GrassColor;
            float _GrassColorVariation;
            float _GrassHeight;
            float _GrassWidth;
            float _GrassBend;
            float _WindSpeed;
            float _WindStrength;
            float _WindFrequency;
            float _GrassDensity;
            float4 _ChunkOffset;
            float4 _TerrainSize;
            float _MoistureMin;
            float _MoistureMax;
            float _MoistureThreshold;
            float _WaterLevel;

            float2 hash2D(float2 p)
            {
                float2 r = mul(float2x2(127.1, 311.7, 269.5, 183.3), p);
                return frac(sin(r) * 43758.5453);
            }

            float perlinNoise(float2 p)
            {
                float2 pi = floor(p);
                float2 pf = p - pi;
                
                float2 w = pf * pf * (3.0 - 2.0 * pf);
                
                float n00 = dot(hash2D(pi + float2(0.0, 0.0)) * 2.0 - 1.0, pf - float2(0.0, 0.0));
                float n10 = dot(hash2D(pi + float2(1.0, 0.0)) * 2.0 - 1.0, pf - float2(1.0, 0.0));
                float n01 = dot(hash2D(pi + float2(0.0, 1.0)) * 2.0 - 1.0, pf - float2(0.0, 1.0));
                float n11 = dot(hash2D(pi + float2(1.0, 1.0)) * 2.0 - 1.0, pf - float2(1.0, 1.0));
                
                return lerp(lerp(n00, n10, w.x), lerp(n01, n11, w.x), w.y);
            }

            float rand(float3 pos) {
                return frac(sin(dot(pos.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            // Match project's noise settings
            float sampleNoise(float2 worldPos, float scale)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                float maxPossibleHeight = 0;

                const int octaves = 6;
                const float persistence = 0.6;
                const float lacunarity = 2.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    float2 samplePos = worldPos / scale * frequency;
                    float perlinValue = perlinNoise(samplePos) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    
                    maxPossibleHeight += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Global normalization
                float normalizedHeight = (noiseHeight + 1) / (maxPossibleHeight / 0.9);
                return saturate(normalizedHeight);
            }

            float3 calculateWind(float3 worldPos, float height) {
                float time = _Time.y * _WindSpeed;
                float windX = sin(time + worldPos.x * _WindFrequency);
                float windZ = cos(time * 0.7 + worldPos.z * _WindFrequency);
                return float3(windX, 0, windZ) * _WindStrength * height;
            }

            v2g vert(appdata v) {
                v2g o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // Calculate UV based on world position relative to chunk's top-left corner
                float2 topLeft = _ChunkOffset.xz;
                float2 moistureUV = (worldPos.xz - topLeft) / _TerrainSize.xy;
                float moisture = tex2Dlod(_MoistureMap, float4(moistureUV, 0, 0)).r;
                moisture = saturate((moisture - _MoistureMin) / (_MoistureMax - _MoistureMin));

                o.vertex = v.vertex;
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = worldPos;
                o.moisture = moisture;
                return o;
            }

            [maxvertexcount(48)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                float3 pos = IN[0].worldPos;
                float3 normal = normalize(IN[0].normal);
                float moisture = IN[0].moisture;

                    // DEBUG: Always output at least one grass blade with color indicating moisture
                float3 baseColor = float3(moisture, 0, 1-moisture); // Red = dry, Blue = wet

                // Don't generate grass below water level or on low moisture areas
                float heightAboveWater = pos.y - _WaterLevel;
                if (moisture < _MoistureThreshold || heightAboveWater < 0.05) return;

                // Use noise to determine grass placement
                float noiseValue = sampleNoise(pos.xz, 50);
                if (noiseValue < 0.3) return;

                // Generate multiple grass blades per triangle based on density
                int numBlades = (int)(_GrassDensity * 4); // Up to 4 blades per triangle
                for(int blade = 0; blade < numBlades; blade++) {
                    // Interpolate position within triangle
                    float3 weights = float3(
                        rand(IN[0].worldPos + blade),
                        rand(IN[1].worldPos + blade),
                        rand(IN[2].worldPos + blade)
                    );
                    weights = weights / (weights.x + weights.y + weights.z); // Normalize

                    float3 bladePos = 
                        IN[0].worldPos * weights.x + 
                        IN[1].worldPos * weights.y + 
                        IN[2].worldPos * weights.z;
                    
                    float3 bladeNormal = normalize(
                        IN[0].normal * weights.x + 
                        IN[1].normal * weights.y + 
                        IN[2].normal * weights.z);

                    // Simple noise variation for blade
                    float2 bladeNoisePos = (bladePos.xz + _ChunkOffset.xz) / 50;
                    float bladeNoise = sampleNoise(bladeNoisePos, 50);
                    
                    // Use noise for height variation
                    float waterProximity = saturate(heightAboveWater / 1.0);
                    float height = _GrassHeight * (0.7 + noiseValue * 0.3) * moisture * waterProximity;
                    float width = _GrassWidth * (0.8 + rand(bladePos) * 0.4);

                    // Randomize orientation
                    float angle = rand(bladePos) * 3.14159 * 2;
                    float3 tangent = normalize(float3(cos(angle), 0, sin(angle)));
                    float3 bitangent = normalize(cross(bladeNormal, tangent));

                    float3 windOffset = calculateWind(bladePos, height);
                    float3 bendOffset = tangent * (_GrassBend * height);

                    // Generate blade vertices
                    g2f o[6];
                    // Use noise for color variation
                    float heightInfluence = lerp(0.95, 1.0, saturate(heightAboveWater * 0.1));
                    float3 baseColor = _GrassColor.rgb * heightInfluence;
                    baseColor *= (1.0 - bladeNoise * _GrassColorVariation * 0.2);

                    // Base
                    o[0].worldPos = bladePos - tangent * width;
                    o[1].worldPos = bladePos + tangent * width;
                    o[0].uv = o[1].uv = float2(0, 0);
                    o[0].moisture = o[1].moisture = moisture;
                    o[0].color = o[1].color = baseColor;

                    // Middle
                    float3 midPos = bladePos + float3(0, height * 0.5, 0);
                    float3 midOffset = windOffset * 0.5 + bendOffset * 0.7;
                    o[2].worldPos = midPos - tangent * (width * 0.7) + midOffset;
                    o[3].worldPos = midPos + tangent * (width * 0.7) + midOffset;
                    o[2].uv = o[3].uv = float2(0, 0.5);
                    o[2].moisture = o[3].moisture = moisture;
                    o[2].color = o[3].color = baseColor * 1.1;

                    // Top
                    o[4].worldPos = bladePos + float3(0, height, 0) - tangent * (width * 0.3) + windOffset + bendOffset;
                    o[5].worldPos = bladePos + float3(0, height, 0) + tangent * (width * 0.3) + windOffset + bendOffset;
                    o[4].uv = o[5].uv = float2(0, 1);
                    o[4].moisture = o[5].moisture = moisture;
                    o[4].color = o[5].color = baseColor * 1.2;

                    // Convert to clip space
                    for(int i = 0; i < 6; i++) {
                        o[i].pos = UnityWorldToClipPos(o[i].worldPos);
                        UNITY_TRANSFER_FOG(o[i], o[i].pos);
                    }

                    // Output triangles
                    triStream.Append(o[0]);
                    triStream.Append(o[1]);
                    triStream.Append(o[2]);
                    triStream.Append(o[3]);
                    triStream.RestartStrip();

                    triStream.Append(o[2]);
                    triStream.Append(o[3]);
                    triStream.Append(o[4]);
                    triStream.Append(o[5]);
                    triStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target {
                float alpha = saturate((i.moisture - _MoistureThreshold) * 2) * 0.8;
                fixed4 col = fixed4(i.color * i.moisture, alpha);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
