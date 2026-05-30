// ガラスフレーム描画シェーダー（SDF 面塗り方式・ワールド座標直接版）。
// 矩形コンテナの枠(帯)と左右2枚の三角バッフルフィンを「中身の詰まった半透明ガラス」として描く。
// クワッドの各ピクセルのワールド座標を頂点シェーダーから渡すことで、
// カメラの OrthoSize やアスペクト比に依存せず、コライダーと正確に一致した位置に描画する。
Shader "Custom/GlassFrame"
{
    Properties
    {
        _ContainerHW ("Container Half Width",  Float) = 2.25
        _ContainerHH ("Container Half Height", Float) = 4.5

        _FrameThickness ("Frame Band Width", Range(0.02, 0.8)) = 0.22

        _FinL0 ("Fin L p0", Vector) = (-2.25, 3.2, 0, 0)
        _FinL1 ("Fin L p1", Vector) = (1.5,   2.0, 0, 0)
        _FinL2 ("Fin L p2", Vector) = (-2.25, 0.8, 0, 0)
        _FinR0 ("Fin R p0", Vector) = (2.25, -3.2, 0, 0)
        _FinR1 ("Fin R p1", Vector) = (-1.5, -2.0, 0, 0)
        _FinR2 ("Fin R p2", Vector) = (2.25, -0.8, 0, 0)

        _GlassColor ("Glass Body (semi-transparent)", Color) = (0.55, 0.80, 0.95, 0.30)
        _EdgeColor  ("Edge Highlight", Color) = (0.95, 0.99, 1.0, 1.0)
        _InnerTint  ("Inner Tint (depth)", Color) = (0.30, 0.55, 0.85, 0.45)

        _EdgeSoft     ("Shape Edge Softness", Range(0.001, 0.08)) = 0.012
        _EdgeWidth    ("Edge Highlight Width", Range(0.01, 0.4)) = 0.10
        _EdgeSharp    ("Edge Highlight Sharpness", Range(0.5, 8.0)) = 2.5
        _EdgeStrength ("Edge Highlight Strength", Range(0.0, 2.0)) = 1.0
        _BodyAlpha    ("Body Alpha (transparency)", Range(0.0, 1.0)) = 0.35
        _EdgeAlpha    ("Edge Alpha", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float  _ContainerHW, _ContainerHH;
            float  _FrameThickness;
            float4 _FinL0, _FinL1, _FinL2, _FinR0, _FinR1, _FinR2;
            float4 _GlassColor, _EdgeColor, _InnerTint;
            float  _EdgeSoft, _EdgeWidth, _EdgeSharp, _EdgeStrength, _BodyAlpha, _EdgeAlpha;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 worldXY    : TEXCOORD0; // このピクセルのワールド座標(XY)
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(wp);
                OUT.worldXY = wp.xy; // ワールド座標をそのまま渡す
                return OUT;
            }

            // 矩形の枠(帯)の SDF。 負=帯の内側, 正=帯の外側
            float sdRectBand(float2 p, float2 half, float bandW)
            {
                float2 d = abs(p) - half;
                float box = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
                return abs(box) - bandW * 0.5;
            }

            // 三角形 SDF（内側が負）
            float sdTriangle(float2 p, float2 p0, float2 p1, float2 p2)
            {
                float2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
                float2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
                float2 pq0 = v0 - e0 * saturate(dot(v0, e0) / dot(e0, e0));
                float2 pq1 = v1 - e1 * saturate(dot(v1, e1) / dot(e1, e1));
                float2 pq2 = v2 - e2 * saturate(dot(v2, e2) / dot(e2, e2));
                float s = sign(e0.x * e2.y - e0.y * e2.x);
                float2 d = min(min(
                    float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                    float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                    float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
                return -sqrt(d.x) * sign(d.y);
            }

            half4 glassFromSDF(float dist)
            {
                float inside = 1.0 - smoothstep(-_EdgeSoft, _EdgeSoft, dist);
                if (inside <= 0.0) return half4(0, 0, 0, 0);

                float depth = saturate(-dist / _EdgeWidth);
                float3 col = lerp(_GlassColor.rgb, _InnerTint.rgb, depth * _InnerTint.a);

                float hl = pow(1.0 - depth, _EdgeSharp) * _EdgeStrength;
                hl = saturate(hl);
                col = lerp(col, _EdgeColor.rgb, hl);

                float a = lerp(_EdgeAlpha, _BodyAlpha, depth);
                a = inside * a;
                return half4(col, a);
            }

            half4 over(half4 top, half4 bottom)
            {
                float a = top.a + bottom.a * (1.0 - top.a);
                float3 rgb = (top.rgb * top.a + bottom.rgb * bottom.a * (1.0 - top.a)) / max(a, 1e-4);
                return half4(rgb, a);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.worldXY; // ワールド座標を直接使用（カメラ非依存）

                float dFrame = sdRectBand(p, float2(_ContainerHW, _ContainerHH), _FrameThickness);
                half4 frame = glassFromSDF(dFrame);

                float dFinL = sdTriangle(p, _FinL0.xy, _FinL1.xy, _FinL2.xy);
                half4 finL = glassFromSDF(dFinL);

                float dFinR = sdTriangle(p, _FinR0.xy, _FinR1.xy, _FinR2.xy);
                half4 finR = glassFromSDF(dFinR);

                half4 col = frame;
                col = over(finL, col);
                col = over(finR, col);
                return col;
            }
            ENDHLSL
        }
    }
}
