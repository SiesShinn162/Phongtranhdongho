Shader "Courtyard/Pond Water"
{
    Properties
    {
        _NormalMap ("Seamless Tangent-space Water Normal", 2D) = "bump" {}
        _ShallowColor ("Shallow Green", Color) = (0.09, 0.32, 0.29, 1)
        _DeepColor ("Deep Green", Color) = (0.025, 0.15, 0.17, 1)
        _NormalScale ("Ripples", Range(0, 0.6)) = 0.16
        _Tiling ("Tiles Per Meter", Range(0.1, 5)) = 1.2
        _Speed ("Scroll Speed", Range(0, 0.1)) = 0.012
        _FresnelStrength ("Fresnel", Range(0, 1)) = 0.4
        _Opacity ("Opacity", Range(0, 1)) = 0.87
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
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor;
                float _NormalScale, _Tiling, _Speed, _FresnelStrength, _Opacity;
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
                float2 uv = i.positionWS.xz * _Tiling;
                float t = _Time.y * _Speed;
                float3 a = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + float2(t, t * .37)));
                float3 b = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * 1.31 + float2(-t * .53, t * .62)));
                float3 up = normalize(i.normalWS);
                float3 n = normalize(up + float3(a.x+b.x, 0, a.y+b.y) * _NormalScale);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fresnel = pow(1-saturate(dot(n, viewDir)), 4);
                Light sun = GetMainLight();
                float3 h = normalize(viewDir + sun.direction);
                float glint = pow(saturate(dot(n, h)), 96) * .12;
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(.5 + .35*(a.x+b.y)));
                color = lerp(color, float3(.31,.47,.43), fresnel * _FresnelStrength);
                color += sun.color * glint;
                return half4(color, saturate(_Opacity + fresnel * .08));
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
