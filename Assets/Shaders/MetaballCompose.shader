// メタボール合成シェーダー。
// FluidCamera が描画した密度 RenderTexture を受け取り、
// 閾値で切り出して液体の色を付けてシーンに合成する。
// FluidDisplay クワッドの MeshRenderer に設定したマテリアルで使う。
Shader "Custom/MetaballCompose"
{
    Properties
    {
        _FluidTex  ("Fluid Density RT", 2D) = "black" {}
        _Threshold ("Threshold",         Range(0.05, 1.5)) = 0.45
        _EdgeWidth ("Edge Smoothness",   Range(0.001, 0.3)) = 0.06
        _FluidColor("Fluid Color",       Color) = (0.08, 0.45, 0.88, 1.0)
        _EdgeColor ("Edge Highlight",    Color) = (0.55, 0.82, 1.0,  1.0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FluidTex);
            SAMPLER(sampler_FluidTex);

            float  _Threshold;
            float  _EdgeWidth;
            float4 _FluidColor;
            float4 _EdgeColor;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float d = SAMPLE_TEXTURE2D(_FluidTex, sampler_FluidTex, IN.uv).r;

                // 閾値付近で滑らかに立ち上がる alpha
                float alpha = smoothstep(_Threshold - _EdgeWidth,
                                         _Threshold + _EdgeWidth, d);

                // 内部ほど FluidColor、境界は EdgeColor（ハイライト）
                float inner = smoothstep(_Threshold + _EdgeWidth,
                                          _Threshold + _EdgeWidth * 5.0, d);

                float4 col = lerp(_EdgeColor, _FluidColor, inner);
                col.a = alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
