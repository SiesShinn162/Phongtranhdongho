Shader "CourtyardV9/GrassWindInstanced" {
Properties {
    _BaseMap("Existing grass atlas", 2D) = "white" {}
    _BaseColor("Tint", Color) = (0.78, 0.9, 0.72, 1)
    _WindStrength("Wind metres", Float) = 0.035
}
SubShader {
    Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
    Pass {
        Name "GrassForwardOnly"
        Tags { "LightMode"="UniversalForward" }
        Cull Off
        ZWrite On

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.0
        #pragma multi_compile_instancing
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Intentionally not UnityPerMaterial: automatic GPU instancing takes priority over SRP batching.
        float4 _BaseColor;
        float _WindStrength;

        struct A {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            float2 root : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct V {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            half3 light : TEXCOORD1;
            half3 tint : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        V vert(A i) {
            V o;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_TRANSFER_INSTANCE_ID(i, o);

            // Compute root position in world coordinates (distributed across chunk)
            float3 worldRoot = TransformObjectToWorld(float3(i.root.x, 0, i.root.y));

            // Blade offset in local meters without horizontal non-uniform scaling distortion
            float3 bladeOffset = i.positionOS.xyz - float3(i.root.x, 0, i.root.y);
            float3 p = worldRoot + bladeOffset;

            // Mask out forbidden zones (sink blocked tufts below ground)
            bool isBlocked = false;
            // Stairs BacTamCap: X in [29.8, 32.5], |Z| in [14.0, 17.0]
            if (worldRoot.x >= 29.8 && worldRoot.x <= 32.5 && abs(worldRoot.z) >= 14.0 && abs(worldRoot.z) <= 17.0) isBlocked = true;
            // Stepping stones path: X near 37.55, |Z| in [4.5, 19.5]
            if (abs(worldRoot.x - 37.55) < 0.78 && abs(worldRoot.z) >= 4.5 && abs(worldRoot.z) <= 19.5) isBlocked = true;
            // Entrance brick plaza (QuangTruong_SanDuoi_NoiHo): X in [31.5, 43.5], |Z| <= 5.2
            if (worldRoot.x >= 31.5 && worldRoot.x <= 43.5 && abs(worldRoot.z) <= 5.2) isBlocked = true;
            // Pond curb border: for X >= 43.0, |Z| <= 16.2
            if (worldRoot.x >= 43.0 && abs(worldRoot.z) <= 16.2) isBlocked = true;

            if (isBlocked) {
                p.y -= 100.0;
            }

            // Wind animation weighted by vertex color alpha
            float gust = sin(_Time.y * 1.5 + p.x * 0.72 + p.z * 0.41);
            p.xz += float2(1.0, 0.37) * (gust * _WindStrength * i.color.a);

            o.positionCS = TransformWorldToHClip(p);
            o.uv = i.uv;

            half3 n = TransformObjectToWorldNormal(i.normalOS);
            Light l = GetMainLight();
            half NdotL = saturate(dot(n, l.direction) * 0.5 + 0.5);
            o.light = SampleSH(half3(0, 1, 0)) * 0.65 + l.color * (0.45 + 0.35 * NdotL);
            o.tint = i.color.rgb * _BaseColor.rgb;
            return o;
        }

        half4 frag(V i) : SV_Target {
            UNITY_SETUP_INSTANCE_ID(i);
            half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
            return half4(texColor.rgb * i.tint * i.light, 1.0);
        }
        ENDHLSL
    }
}
}