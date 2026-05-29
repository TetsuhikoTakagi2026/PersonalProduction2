// 砂時計のガラスフレームを数式で描くフルスクリーン UI シェーダー。
// パラメータを HourglassController と合わせることで
// コライダーと完全に一致したフレームが描かれる。
Shader "Custom/GlassHourglass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Main Tex", 2D) = "white" {}

        // ─── HourglassController と同じ値に設定 ───
        _HalfH  ("Half Height",      Float) = 4.5
        _HalfBW ("Half Bulb Width",  Float) = 2.25
        _HalfNW ("Half Neck Width",  Float) = 0.25
        _HalfNH ("Half Neck Height", Float) = 0.30
        _OrthoSize ("Camera Ortho Size", Float) = 5.0

        // ─── フレームの見た目 ───
        _FrameThick ("Frame Thickness",  Float) = 0.10
        _GlassColor ("Glass Base Color", Color) = (0.65, 0.92, 1.00, 0.30)
        _EdgeColor  ("Edge Highlight",   Color) = (1.00, 1.00, 1.00, 0.90)
        _ShineColor ("Shine Color",      Color) = (1.00, 1.00, 1.00, 0.70)
        _ShineX     ("Shine X Bias",     Range(-1,1)) = -0.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float  _HalfH, _HalfBW, _HalfNW, _HalfNH;
            float  _OrthoSize, _FrameThick;
            float4 _GlassColor, _EdgeColor, _ShineColor;
            float  _ShineX;

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.pos = TransformObjectToHClip(IN.pos.xyz);
                OUT.uv  = IN.uv;
                return OUT;
            }

            // UV (0-1) → ワールド座標
            float2 UVToWorld(float2 uv)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                return float2(
                    (uv.x - 0.5) * _OrthoSize * 2.0 * aspect,
                    (uv.y - 0.5) * _OrthoSize * 2.0
                );
            }

            // ある y での砂時計の半幅
            float hourglassWidth(float ay)
            {
                if (ay >= _HalfNH) return _HalfBW;
                return lerp(_HalfNW, _HalfBW, ay / _HalfNH);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p  = UVToWorld(IN.uv);
                float  ax = abs(p.x);
                float  ay = abs(p.y);

                // ─── サイド壁（左右の斜め壁）───
                float wallX   = hourglassWidth(ay);
                float sideDist = abs(ax - wallX);         // 壁までの水平距離
                bool  inHeight = ay < _HalfH;

                float sideMask = inHeight
                    ? smoothstep(_FrameThick, 0.0, sideDist)
                    : 0.0;

                // ─── 上下キャップ ───
                float capDist  = abs(ay - _HalfH);
                float capMask  = (ax < _HalfBW + _FrameThick)
                    ? smoothstep(_FrameThick, 0.0, capDist)
                    : 0.0;

                float mask = max(sideMask, capMask);
                if (mask < 0.01) discard;

                // ─── ガラスの色 ───
                // 壁の内側(砂時計内部側)ほど明るいハイライト
                float innerRatio = saturate((wallX - ax) / _FrameThick);
                float4 col = lerp(_EdgeColor, _GlassColor, innerRatio);

                // 斜めから当たる光をシミュレート（_ShineX 側に反射）
                float shine = saturate(1.0 - abs(p.x / (_HalfBW + 0.1) - _ShineX));
                shine = pow(shine, 3.0) * mask;
                col   = lerp(col, _ShineColor, shine * 0.6);

                col.a *= mask;
                return col;
            }
            ENDHLSL
        }
    }
}
