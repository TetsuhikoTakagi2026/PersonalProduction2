// メタボール合成シェーダー（案A バッフル型 対応版）。
// FluidCamera が描画した密度 RenderTexture を受け取り、
// 閾値で切り出して液体の色を付け、矩形コンテナ形状でクリッピングしてシーンに合成する。
// バッフルフィンは物理コライダーが担当するため、シェーダーは単純な矩形クリップのみ。
Shader "Custom/MetaballCompose"
{
    Properties
    {
        _FluidTex   ("Fluid Density RT", 2D)    = "black" {}
        _Threshold  ("Threshold",        Range(0.05, 1.5)) = 0.45
        _EdgeWidth  ("Edge Smoothness",  Range(0.001, 0.3)) = 0.06
        _FluidColor ("Fluid Color",      Color)  = (0.08, 0.45, 0.88, 1.0)
        _EdgeColor  ("Edge Highlight",   Color)  = (0.55, 0.82, 1.0,  1.0)

        // ─── 矩形コンテナ クリッピング（HourglassController と同じ値） ───
        _ContainerHW ("Container Half Width",  Float) = 2.25
        _ContainerHH ("Container Half Height", Float) = 4.5
        _OrthoSize   ("Camera Ortho Size",     Float) = 5.0
        _ClipEdge    ("Clip Softness",         Range(0.001, 0.2)) = 0.04
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

            float  _Threshold, _EdgeWidth;
            float4 _FluidColor, _EdgeColor;
            float  _ContainerHW, _ContainerHH;
            float  _OrthoSize, _ClipEdge;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // UV (0-1) → カメラ中心を原点としたワールド座標
            float2 UVToWorld(float2 uv)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                return float2(
                    (uv.x - 0.5) * _OrthoSize * 2.0 * aspect,
                    (uv.y - 0.5) * _OrthoSize * 2.0
                );
            }

            // 矩形コンテナの内部余裕 SDF
            // 戻り値: 正値 = 内側, 負値 = 外側
            float containerInside(float2 p)
            {
                float2 d = abs(p) - float2(_ContainerHW, _ContainerHH);
                return -max(d.x, d.y);
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

                // ─── 矩形コンテナでクリッピング ───
                float2 wp     = UVToWorld(IN.uv);
                float  inside = containerInside(wp);
                col.a *= smoothstep(-_ClipEdge, _ClipEdge, inside);

                return col;
            }
            ENDHLSL
        }
    }
}
