Shader "BallGame/Anti-Gravity Flow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BaseColor ("Base Color", Color) = (0.424, 0.302, 1, 0.18)
        _FlowColor ("Flow Color", Color) = (0.749, 0.667, 1, 0.7)
        _ArrowColor ("Arrow Color", Color) = (0.945, 0.925, 1, 0.9)
        _FlowDensity ("Flow Density", Range(0.5, 8)) = 3
        _FlowSpeed ("Upward Speed", Range(0, 6)) = 1.2
        _ArrowStrength ("Arrow Strength", Range(0, 1)) = 0.65
        _EdgePulseStrength ("Edge Pulse", Range(0, 1)) = 0.35
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
            // Flow direction needs each renderer's original object-to-world matrix.
            "DisableBatching" = "True"
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
            fixed4 _FlowColor;
            fixed4 _ArrowColor;
            float _FlowDensity;
            float _FlowSpeed;
            float _ArrowStrength;
            float _EdgePulseStrength;

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
                float2 flowPos : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float2 worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                float2 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xy;
                float2 worldRight = normalize(mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)).xy);
                float2 worldUp = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)).xy);
                float2 relativeWorldPos = worldPos - worldOrigin;
                o.flowPos = float2(dot(relativeWorldPos, worldRight), dot(relativeWorldPos, worldUp));
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float time = _Time.y;

                float column = sin(i.flowPos.x * 5.2) * 0.5 + sin(i.flowPos.x * 9.7 + 1.4) * 0.25;
                float risingPhase = frac((i.flowPos.y + column * 0.12) * _FlowDensity - time * _FlowSpeed);
                float streak = 1.0 - smoothstep(0.04, 0.22, risingPhase);
                streak *= 0.55 + 0.45 * saturate(sin(i.flowPos.x * 7.0 + 2.1) * 0.5 + 0.5);

                float2 arrowCell = frac(float2(i.flowPos.x * 0.72, i.flowPos.y * 0.75 - time * _FlowSpeed * 0.32)) - 0.5;
                float chevron = 1.0 - smoothstep(0.035, 0.105, abs(abs(arrowCell.x) - (0.34 - arrowCell.y) * 0.72));
                chevron *= step(-0.1, arrowCell.y) * (1.0 - step(0.34, arrowCell.y));

                float edgeDistance = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float edge = 1.0 - smoothstep(0.025, 0.085, edgeDistance);
                float edgePulse = edge * (0.55 + 0.45 * sin(time * 2.2 + i.flowPos.y * 1.6));

                float flowEnergy = saturate(streak * 0.5 + edgePulse * _EdgePulseStrength);
                float arrowEnergy = saturate(chevron * _ArrowStrength);
                fixed4 result = _BaseColor;
                result.rgb = lerp(result.rgb, _FlowColor.rgb, flowEnergy);
                result.rgb = lerp(result.rgb, _ArrowColor.rgb, arrowEnergy);
                result.a += flowEnergy * _FlowColor.a * 0.55;
                result.a += arrowEnergy * _ArrowColor.a * 0.55;
                result *= i.color;
                result.a *= tex.a;
                return result;
            }
            ENDCG
        }
    }
}
