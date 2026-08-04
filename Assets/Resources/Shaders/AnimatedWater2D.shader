Shader "BallGame/Animated Water 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BaseColor ("Base Color", Color) = (0.12, 0.62, 1, 0.32)
        _DeepColor ("Deep Color", Color) = (0.02, 0.25, 0.72, 0.48)
        _WaveColor ("Wave Color", Color) = (0.65, 0.92, 1, 0.9)
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.25)) = 0.055
        _WaveFrequency ("Wave Frequency", Range(1, 30)) = 10
        _WaveSpeed ("Wave Speed", Range(-5, 5)) = 1
        _FlowStrength ("Flow Highlight Strength", Range(0, 1)) = 0.16
        _FlowSpeed ("Flow Speed", Range(-5, 5)) = 0.45
        _BodyDetailStrength ("Body Detail Strength", Range(0, 1)) = 0.28
        _SparkleStrength ("Sparkle Strength", Range(0, 1)) = 0.16
        _SurfaceMode ("Surface Mode", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BaseColor;
            fixed4 _DeepColor;
            fixed4 _WaveColor;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _FlowStrength;
            float _FlowSpeed;
            float _BodyDetailStrength;
            float _SparkleStrength;
            float _SurfaceMode;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float time = _Time.y;

                float waveA = sin(i.uv.x * _WaveFrequency + time * _WaveSpeed) * _WaveAmplitude;
                float waveB = sin(i.uv.x * (_WaveFrequency * 0.57) - time * (_WaveSpeed * 0.72) + 1.7) * (_WaveAmplitude * 0.55);
                float combinedWave = waveA + waveB;

                float diagonalA = sin((i.uv.x * 1.35 + i.uv.y) * 15.0 + time * _FlowSpeed);
                float diagonalB = sin((i.uv.x * 0.8 - i.uv.y * 1.4) * 11.0 - time * (_FlowSpeed * 0.66));
                float flow = saturate(diagonalA * 0.5 + diagonalB * 0.35 - 0.42) * _FlowStrength;
                float breathe = 0.94 + sin(time * 0.75) * 0.06;

                float currentA = pow(saturate(sin((i.uv.x * 4.2 + i.uv.y * 11.0) + time * 0.42) * 0.5 + 0.5), 7.0);
                float currentB = pow(saturate(sin((i.uv.x * -6.5 + i.uv.y * 8.2) - time * 0.31 + 2.4) * 0.5 + 0.5), 9.0);
                float currentBands = saturate(currentA * 0.62 + currentB * 0.48);

                float causticA = pow(saturate(sin((i.uv.x * 15.0 + i.uv.y * 6.5) + time * 0.68) * 0.5 + 0.5), 16.0);
                float causticB = pow(saturate(sin((i.uv.x * -10.0 + i.uv.y * 17.0) - time * 0.53) * 0.5 + 0.5), 18.0);
                float caustics = saturate(causticA + causticB * 0.72);

                float2 sparkleUv = i.uv * float2(26.0, 18.0) + float2(time * 0.16, time * 0.31);
                float2 sparkleCell = floor(sparkleUv);
                float2 sparkleLocal = frac(sparkleUv);
                float2 sparklePosition = float2(
                    Hash21(sparkleCell + 7.3),
                    Hash21(sparkleCell + 19.1));
                float sparklePresence = step(0.83, Hash21(sparkleCell + 31.7));
                float sparkleShape = 1.0 - smoothstep(0.025, 0.105, distance(sparkleLocal, sparklePosition));
                float sparklePulse = 0.58 + sin(time * 2.1 + Hash21(sparkleCell) * 6.2831) * 0.42;
                float sparkles = sparklePresence * sparkleShape * sparklePulse * _SparkleStrength;

                fixed4 body = lerp(_DeepColor, _BaseColor, saturate(i.uv.y));
                float bodyHighlights = saturate(
                    flow
                    + currentBands * (_BodyDetailStrength * 0.42)
                    + caustics * (_BodyDetailStrength * 0.3)
                    + sparkles);
                body.rgb = lerp(body.rgb, _WaveColor.rgb, bodyHighlights);
                body.a *= breathe * (0.96 + currentBands * _BodyDetailStrength * 0.08);

                float surfaceCenter = 0.5 + combinedWave;
                float surfaceBand = 1.0 - smoothstep(0.09, 0.23, abs(i.uv.y - surfaceCenter));
                float secondBand = 1.0 - smoothstep(0.035, 0.11, abs(i.uv.y - (0.5 - combinedWave * 0.58 - 0.19)));
                fixed4 surface = _WaveColor;
                surface.rgb = lerp(_BaseColor.rgb, _WaveColor.rgb, surfaceBand);
                surface.a *= saturate(surfaceBand + secondBand * 0.42);

                fixed4 result = lerp(body, surface, step(0.5, _SurfaceMode));
                result *= i.color;
                result.a *= tex.a;
                return result;
            }
            ENDCG
        }
    }
}
