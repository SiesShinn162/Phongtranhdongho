Shader "Courtyard/Roof Triplanar"
{
    Properties
    {
        _BaseMap ("Terracotta Albedo", 2D) = "white" {}
        _BumpMap ("Terracotta Normal", 2D) = "bump" {}
        _Tint ("Earth Red Tint", Color) = (1, 0.83, 0.72, 1)
        _MetersPerTile ("Meters Per Texture Tile", Range(0.2, 6)) = 2.9
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.5
        _BlendSharpness ("Projection Sharpness", Range(1, 12)) = 6
        _Roughness ("Roughness", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _MetersPerTile, _NormalStrength, _BlendSharpness, _Roughness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 weight = pow(abs(n), _BlendSharpness);
                weight /= max(weight.x + weight.y + weight.z, 0.0001);
                // Mirrored coordinates keep the roof tile pattern facing consistently on both slopes.
                float3 p = i.positionWS / max(_MetersPerTile, 0.01);
                float sx = n.x < 0 ? -1 : 1, sy = n.y < 0 ? -1 : 1, sz = n.z < 0 ? -1 : 1;
                float2 uvX = p.zy * float2(-sx, 1);
                float2 uvY = p.xz * float2(1, -sy);
                float2 uvZ = p.xy * float2(sz, 1);
                half3 albedo = (SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvX).rgb * weight.x
                    + SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvY).rgb * weight.y
                    + SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvZ).rgb * weight.z) * _Tint.rgb;
                float3 bx = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvX));
                float3 by = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvY));
                float3 bz = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvZ));
                float3 nx = float3(bx.z * sx, bx.y, -bx.x * sx);
                float3 ny = float3(by.x, by.z * sy, -by.y * sy);
                float3 nz = float3(bz.x * sz, bz.y, bz.z * sz);
                float3 normal = normalize(lerp(n, nx * weight.x + ny * weight.y + nz * weight.z, _NormalStrength));
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float diffuse = saturate(dot(normal, sun.direction)) * sun.shadowAttenuation * sun.distanceAttenuation;
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 halfDir = normalize(viewDir + sun.direction);
                float spec = pow(saturate(dot(normal, halfDir)), lerp(64, 5, _Roughness)) * (1 - _Roughness) * .12;
                half3 ambient = SampleSH(normal);
                return half4(albedo * (ambient + sun.color * diffuse) + sun.color * spec * sun.shadowAttenuation, 1);
            }
            ENDHLSL
        }
        // Opaque URP Lit passes use the same mesh positions; no UVs are needed for
        // shadow/depth because this material has no alpha clipping or displacement.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
