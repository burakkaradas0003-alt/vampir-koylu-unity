// Çimen: köşe rengi (dipte koyu, uçta açık) x örnek rengi, rüzgârda uçlarından sallanır.
// Graphics.RenderMeshInstanced ile çizilir; ana ışık + gölge + ortam ışığı + sis.
Shader "Zu/Grass"
{
    Properties { _Wind ("Wind", Float) = 1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Wind;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Tint)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 color : COLOR; float fog : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert (Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 root = TransformObjectToWorld(float3(0, 0, 0));
                float h = saturate(i.positionOS.y); h = h * h * 1.4;
                float t = _Time.y;
                float gust = sin(t * 0.6 + root.x * 0.05) * 0.5 + 0.5;
                float3 p = i.positionOS.xyz;
                p.x += (sin(t * 1.9 + root.x * 0.45 + root.z * 0.25) * 0.09 + gust * 0.06) * h * _Wind;
                p.z += cos(t * 1.5 + root.z * 0.4 + root.x * 0.1) * 0.05 * h * _Wind;
                o.positionWS = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _Tint);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float nl = saturate(dot(n, L.direction)) * 0.45 + 0.55;
                float3 amb = SampleSH(n);
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                    amb *= ao.indirectAmbientOcclusion; float dao = ao.directAmbientOcclusion;
                #else
                    float dao = 1;
                #endif
                float3 lit = L.color * nl * L.shadowAttenuation * dao + amb;
                // ek ışıklar (kamp ateşi, fenerler): kum ve çimen ateşin sıcak ışığını alır
                #if defined(_ADDITIONAL_LIGHTS)
                    uint cnt = GetAdditionalLightsCount();
                        for (uint li = 0u; li < cnt; ++li)
                        {
                            Light A = GetAdditionalLight(li, i.positionWS, half4(1, 1, 1, 1));
                            lit += A.color * A.distanceAttenuation * A.shadowAttenuation * (saturate(dot(n, A.direction)) * 0.6 + 0.4);
                        }
                #endif
                float3 c = i.color.rgb * 1.15 * lit;
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Wind;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vert (A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                float3 root = TransformObjectToWorld(float3(0, 0, 0));
                float h = saturate(i.positionOS.y); h = h * h * 1.4; float t = _Time.y;
                float gust = sin(t * 0.6 + root.x * 0.05) * 0.5 + 0.5; float3 p = i.positionOS.xyz;
                p.x += (sin(t * 1.9 + root.x * 0.45 + root.z * 0.25) * 0.09 + gust * 0.06) * h * _Wind;
                p.z += cos(t * 1.5 + root.z * 0.4 + root.x * 0.1) * 0.05 * h * _Wind;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(p)); return o;
            }
            half frag (V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
