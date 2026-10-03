Shader "Courtyard/Pond Water"
{
    Properties
    {
        _NormalMap ("Seamless Tangent-space Water Normal", 2D) = "bump" {}
        _ShallowColor ("Shallow Teal", Color) = (0.09, 0.38, 0.35, 1)
        _DeepColor ("Deep Jade", Color) = (0.02, 0.14, 0.16, 1)
        _NormalScale ("Ripples Strength", Range(0, 1.0)) = 0.22
        _Tiling ("Ripple Tiling", Range(0.1, 5)) = 1.2
        _Speed ("Scroll Speed", Range(0, 0.1)) = 0.015
        
        [Header(Real 3D Waves)]
        _WaveHeight ("Wave Amplitude (m)", Range(0, 0.1)) = 0.035
        _WaveFrequency ("Wave Frequency", Range(0.1, 3.0)) = 0.85
        _WaveSpeed ("Wave Speed", Range(0, 3.0)) = 1.0
        
        [Header(Edge Foam and Depth)]
        _FoamColor ("Foam Color", Color) = (0.92, 0.98, 0.98, 0.85)
        _FoamDistance ("Edge Foam Distance (m)", Range(0.02, 0.5)) = 0.18
        _PondBounds ("Pond World Bounds (min XZ, max XZ)", Vector) = (44.53, -15.08, 73.41, 14.94)
        
        [Header(Surface Optics)]
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.45
        _Opacity ("Base Opacity", Range(0, 1)) = 0.86
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor, _FoamColor, _PondBounds;
                float _NormalScale, _Tiling, _Speed, _FresnelStrength, _Opacity;
                float _WaveHeight, _WaveFrequency, _WaveSpeed, _FoamDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  waveY      : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 pWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);

                // Only displace top surface (facing upward) so pond skirt remains anchored
                float isTop = saturate(nWS.y * 1.5 - 0.5);

                float t = _Time.y * _WaveSpeed;

                // Wave 1: primary gentle swell
                float2 d1 = float2(0.92, 0.38);
                float f1 = _WaveFrequency * 1.0;
                float phase1 = dot(d1, pWS.xz) * f1 + t * 1.15;
                float s1 = sin(phase1);
                float c1 = cos(phase1);
                float a1 = _WaveHeight * 0.55;

                // Wave 2: cross breeze swell
                float2 d2 = float2(-0.55, 0.83);
                float f2 = _WaveFrequency * 1.55;
                float phase2 = dot(d2, pWS.xz) * f2 + t * 1.65;
                float s2 = sin(phase2);
                float c2 = cos(phase2);
                float a2 = _WaveHeight * 0.32;

                // Wave 3: surface ripple
                float2 d3 = float2(0.70, -0.71);
                float f3 = _WaveFrequency * 2.4;
                float phase3 = dot(d3, pWS.xz) * f3 + t * 2.2;
                float s3 = sin(phase3);
                float c3 = cos(phase3);
                float a3 = _WaveHeight * 0.18;

                float waveY = (s1 * a1 + s2 * a2 + s3 * a3) * isTop;
                pWS.y += waveY;

                // Slopes for normal calculation
                float dy_dx = (c1 * a1 * f1 * d1.x + c2 * a2 * f2 * d2.x + c3 * a3 * f3 * d3.x) * isTop;
                float dy_dz = (c1 * a1 * f1 * d1.y + c2 * a2 * f2 * d2.y + c3 * a3 * f3 * d3.y) * isTop;
                float3 waveNormal = normalize(float3(-dy_dx, 1.0, -dy_dz));

                o.positionWS = pWS;
                o.normalWS = waveNormal;
                o.waveY = waveY;
                o.positionCS = TransformWorldToHClip(pWS);

                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // World-space rim remains usable without a camera depth texture.
                float2 rimDistance = min(i.positionWS.xz - _PondBounds.xy,
                                         _PondBounds.zw - i.positionWS.xz);
                float shoreDistance = max(0.0, min(rimDistance.x, rimDistance.y));
                float depthDiff = lerp(0.12, 1.35, saturate(shoreDistance / 2.5));
                float shoreFoam = (1.0 - smoothstep(0.02, _FoamDistance, shoreDistance)) * 0.12;

                // LinearEyeDepth is perspective-only. _ProjectionParams.x describes
                // projection flipping; unity_OrthoParams.w identifies orthographic views.
                // Missing depth textures return a near/far sentinel: do not convert it
                // or let it generate foam across the entire pond.
                float2 screenUV = i.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                if (unity_OrthoParams.w < 0.5 && rawDepth > 0.00001 && rawDepth < 0.99999)
                {
                    float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float surfaceEyeDepth = -TransformWorldToView(i.positionWS).z;
                    float sampledThickness = sceneEyeDepth - surfaceEyeDepth;
                    if (sampledThickness > 0.001 && sceneEyeDepth < _ProjectionParams.z - 0.01)
                    {
                        depthDiff = min(sampledThickness, 8.0);
                        float intersectionFoam = 1.0 - smoothstep(0.01, _FoamDistance, depthDiff);
                        shoreFoam = max(shoreFoam, intersectionFoam * 0.22);
                    }
                }

                // Crest foam on peak waves
                float crestFoam = saturate((i.waveY / max(_WaveHeight, 0.001) - 0.72) * 3.6);
                float totalFoam = min(0.25, shoreFoam + crestFoam * 0.04);

                // Scrolling ripple normal map
                float2 uv = i.positionWS.xz * _Tiling;
                float t = _Time.y * _Speed;
                float3 normA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + float2(t, t * 0.38)));
                float3 normB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * 1.33 + float2(-t * 0.55, t * 0.65)));
                float3 rippleOffset = float3(normA.x + normB.x, 0.0, normA.y + normB.y) * _NormalScale;
                float3 n = normalize(i.normalWS + rippleOffset);

                // Depth absorption: teal near shore, jade in deep water
                float waterDepthFactor = saturate(depthDiff / 1.1);
                half3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, waterDepthFactor);

                // Fresnel
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fresnel = pow(1.0 - saturate(dot(n, viewDir)), 4.0);

                // Sun specular glint
                Light sun = GetMainLight();
                float3 h = normalize(viewDir + sun.direction);
                float glint = pow(saturate(dot(n, h)), 128.0) * 0.28;

                half3 col = lerp(baseColor, float3(0.35, 0.52, 0.48), fresnel * _FresnelStrength);
                col += sun.color * glint;

                // Blend foam
                col = lerp(col, _FoamColor.rgb, totalFoam * _FoamColor.a);
                half alpha = saturate(_Opacity + fresnel * 0.12 + totalFoam * 0.5);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
