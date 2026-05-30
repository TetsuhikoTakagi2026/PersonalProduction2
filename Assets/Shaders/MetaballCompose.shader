// メタボール合成シェーダー（ツヤツヤのジェル・粘液バージョン）。
// FluidCamera が描画した密度 RenderTexture を受け取り、
// 閾値で切り出してジェル状の液体に色を付け、矩形コンテナでクリッピングして合成する。
// 不透明寄り＋シャープな輪郭＋内側のツヤ（ハイライト）で、ぷるんとした質感を出す。
Shader "Custom/MetaballCompose"
{
    Properties
    {
        _FluidTex   ("Fluid Density RT", 2D)    = "black" {}
        _Threshold  ("Threshold",        Range(0.05, 1.5)) = 0.45
        _EdgeWidth  ("Edge Smoothness",  Range(0.001, 0.3)) = 0.02

        // ─── ジェルの色 ───
        _BodyColor  ("Body Color",       Color) = (0.10, 0.55, 0.95, 1.0)
        _DeepColor  ("Deep/Core Color",  Color) = (0.04, 0.30, 0.75, 1.0)
        _RimColor   ("Rim (edge) Color", Color) = (0.02, 0.20, 0.55, 1.0)
        _ShineColor ("Shine Highlight",  Color) = (0.85, 0.97, 1.0,  1.0)

        // ─── 質感の調整 ───
        _Alpha       ("Overall Alpha",        Range(0.0, 1.0)) = 0.95
        _RimWidth    ("Rim Width",            Range(0.0, 1.0)) = 0.35
        _RimStrength ("Rim Strength",         Range(0.0, 1.0)) = 0.6
        _ShineWidth  ("Shine Width",          Range(0.0, 1.0)) = 0.25
        _ShineStrength("Shine Strength",      Range(0.0, 2.0)) = 1.0
        _ShinePos    ("Shine Position (0=core,1=edge)", Range(0.0, 1.0)) = 0.7

        // ─── 矩形コンテナ クリッピング（HourglassController と同じ値） ───
        _ContainerHW ("Container Half Width",  Float) = 2.25
        _ContainerHH ("Container Half Height", Float) = 4.5
        _OrthoSize   ("Camera Ortho Size",     Float) = 5.0
        _ClipEdge    ("Clip Softness",         Range(0.001, 0.2)) = 0.02
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
            float4 _BodyColor, _DeepColor, _RimColor, _ShineColor;
            float  _Alpha, _RimWidth, _RimStrength, _ShineWidth, _ShineStrength, _ShinePos;
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

            float containerInside(float2 p)
            {
                float2 d = abs(p) - float2(_ContainerHW, _ContainerHH);
                return -max(d.x, d.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dens = SAMPLE_TEXTURE2D(_FluidTex, sampler_FluidTex, IN.uv).r;

                // シャープな輪郭（ジェルは縁がくっきり）
                float shape = smoothstep(_Threshold - _EdgeWidth,
                                         _Threshold + _EdgeWidth, dens);

                // 表面からの「深さ」 0=縁, 1=中心（密度が高いほど中心）
                float depth = saturate((dens - _Threshold) / (_Threshold * 1.2));

                // ─── ベースの色：中心ほど濃く（立体感）───
                float3 col = lerp(_BodyColor.rgb, _DeepColor.rgb, depth);

                // ─── リム（縁取り）：縁の内側を少し濃くしてぷるん感 ───
                float rim = smoothstep(_RimWidth, 0.0, depth); // 縁付近で1
                col = lerp(col, _RimColor.rgb, rim * _RimStrength);

                // ─── ツヤ（ハイライト）：表面の一定の深さに明るい帯を入れる ───
                float shine = smoothstep(_ShineWidth, 0.0, abs(depth - _ShinePos));
                col = lerp(col, _ShineColor.rgb, saturate(shine * _ShineStrength));

                // ジェルはほぼ不透明
                float alpha = shape * _Alpha;

                float4 outCol = float4(col, alpha);

                // ─── 矩形コンテナでクリッピング ───
                float2 wp     = UVToWorld(IN.uv);
                float  inside = containerInside(wp);
                outCol.a *= smoothstep(-_ClipEdge, _ClipEdge, inside);

                return outCol;
            }
            ENDHLSL
        }
    }
}
