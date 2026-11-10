// Alev kartı: siyah zeminli alev görüntüsü; parlaklık saydamlık olur. Atlas karesi _UV ile seçilir.
Shader "Zu/Flame"
{
    Properties { _MainTex ("Tex", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END
            float4 _UV;     // xy ölçek, zw kayma (nesne başına)
            float _Alpha;
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert (A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv * _UV.xy + _UV.zw; return o; }
            half4 frag (V i) : SV_Target
            {
                float3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                float a = saturate(c.g * 1.6) * _Alpha;
                return half4(c * 1.25, a);
            }
            ENDHLSL
        }
    }
}
