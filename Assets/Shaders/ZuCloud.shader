// Bulut / ay: yumuşak kenarlı, normal saydam karışım (sis uygulanmaz; ufukta kaybolmaz)
Shader "Zu/Cloud"
{
    Properties { _MainTex ("Tex", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-50" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert (A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o); o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag (V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // alt kısım hafif gölgeli (hacim hissi)
                float shade = lerp(0.78, 1.0, saturate(i.uv.y * 1.6));
                return half4(col.rgb * t.rgb * shade, col.a * t.a);
            }
            ENDHLSL
        }
    }
}
