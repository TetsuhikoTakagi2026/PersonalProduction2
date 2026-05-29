// 液体粒子用シェーダー。
// 円の中心から外縁に向かって滑らかに減衰する密度フィールドを
// 加算ブレンドで RenderTexture に書き込む。
// 複数の円が重なると密度が加算され、メタボールの「溶け合い」が生まれる。
Shader "Custom/FluidCircle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Radius ("Falloff Radius", Range(0.1, 1.0)) = 0.9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One   // Additive: 密度を加算
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Radius;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // UV (0-1) → 中心が原点の距離
                float dist = length(IN.uv - 0.5) * 2.0; // 端で 1.0

                // 2次減衰でメタボールらしいフィールドを生成
                float density = saturate(1.0 - dist / _Radius);
                density = density * density;

                return half4(density, density, density, density);
            }
            ENDHLSL
        }
    }
}
