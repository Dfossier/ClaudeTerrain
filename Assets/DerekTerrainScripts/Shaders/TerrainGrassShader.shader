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
        _ClumpScale ("Clump Scale", Range(0.1, 10.0)) = 2.0
        _ClumpSpread ("Clump Spread", Range(0, 1)) = 0.7
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
            float _ClumpScale;
            float _ClumpSpread;
            float _TessellationUniform;


            float rand(float3 pos) {
                return frac(sin(dot(pos.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            float hash(float2 p) {
                float3 p3  = frac(float3(p.xyx) * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 position) {
                float2 i = floor(position);
                float2 f = frac(position);
    
                // Smoothstep interpolation
                f = f * f * (3.0 - 2.0 * f);
    
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
    
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
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

                // Don't generate grass below water level or on low moisture areas
                // More precise water level check using world position
                float heightAboveWater = pos.y - _WaterLevel;
                if (moisture < _MoistureThreshold || heightAboveWater < 0.05) return;

                    // Calculate clumping value
                float2 clumpPos = pos.xz / _ClumpScale;
                float clumpNoise = noise(clumpPos);
                clumpNoise = pow(clumpNoise, 1.0 - _ClumpSpread); // Adjust distribution

                // Modify density based on clumping
                int numBlades = (int)(_GrassDensity * 3 * clumpNoise);

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

                    float random = rand(bladePos + blade);
                    // Adjust grass height based on water proximity
                    float waterProximity = saturate(heightAboveWater / 1.0); // Sharper transition near water
                    float height = _GrassHeight * (0.7 + random * 0.3) * moisture * waterProximity;
                    float width = _GrassWidth * (0.8 + random * 0.4);

                    // Randomize orientation
                    float angle = random * 3.14159 * 2;
                    float3 tangent = normalize(float3(cos(angle), 0, sin(angle)));
                    float3 bitangent = normalize(cross(bladeNormal, tangent));

                    // Adjust position within clump
                    float clumpOffset = rand(bladePos + blade) * 0.2 * (1.0 - clumpNoise);
                    bladePos.xz += float2(cos(angle), sin(angle)) * clumpOffset;

                    // Vary height within clump
                    height *= lerp(0.8, 1.2, rand(bladePos));

                    float3 windOffset = calculateWind(bladePos, height);
                    float3 bendOffset = tangent * (_GrassBend * height);

                    // Generate blade vertices
                    g2f o[6];
                    // Minimal height-based color variation
                    float heightInfluence = lerp(0.95, 1.0, saturate(heightAboveWater * 0.1));
                    float3 baseColor = _GrassColor.rgb * heightInfluence;
                    baseColor *= (1.0 - random * _GrassColorVariation * 0.3); // Reduced variation

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