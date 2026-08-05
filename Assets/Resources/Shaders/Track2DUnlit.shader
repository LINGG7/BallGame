Shader "BallGame/Track 2D Unlit"
{
    Properties
    {
        _Color ("Track Color", Color) = (0.075, 0.09, 0.115, 1)
        _RailColor ("Rail Color", Color) = (0.72, 0.78, 0.86, 1)
        _CenterLineColor ("Center Line Color", Color) = (0.05, 0.18, 0.95, 1)
        _TieColor ("Tie Color", Color) = (0.025, 0.03, 0.04, 1)
        _RailWidth ("Rail Width", Range(0.01, 0.2)) = 0.055
        _RailInset ("Rail Inset", Range(0.02, 0.35)) = 0.16
        _CenterLineWidth ("Center Line Width", Range(0, 0.15)) = 0.018
        _TieSpacing ("Tie Spacing", Range(0.1, 2)) = 0.42
        _TieWidth ("Tie Width", Range(0.01, 0.25)) = 0.08
        _TieStrength ("Tie Strength", Range(0, 1)) = 0.45
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
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
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _RailColor;
            fixed4 _CenterLineColor;
            fixed4 _TieColor;
            float _RailWidth;
            float _RailInset;
            float _CenterLineWidth;
            float _TieSpacing;
            float _TieWidth;
            float _TieStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * _Color;
                float across = saturate(i.uv.y);
                float along = i.uv.x;

                float leftRail = 1.0 - smoothstep(_RailWidth, _RailWidth + 0.015, abs(across - _RailInset));
                float rightRail = 1.0 - smoothstep(_RailWidth, _RailWidth + 0.015, abs(across - (1.0 - _RailInset)));
                float railMask = saturate(leftRail + rightRail);

                //float centerMask = 1.0 - smoothstep(_CenterLineWidth, _CenterLineWidth + 0.01, abs(across - 0.5));
                float centerMask = 0.0;

                float tiePhase = abs(frac(along / max(_TieSpacing, 0.001)) - 0.5) * 2.0;
                float tieMask = 1.0 - smoothstep(_TieWidth, _TieWidth + 0.02, tiePhase);
                tieMask *= smoothstep(0.05, 0.18, across) * (1.0 - smoothstep(0.82, 0.95, across));

                color.rgb = lerp(color.rgb, _TieColor.rgb, tieMask * _TieStrength);
                color.rgb = lerp(color.rgb, _RailColor.rgb, railMask);
                color.rgb = lerp(color.rgb, _CenterLineColor.rgb, centerMask);
                return color;
            }
            ENDCG
        }
    }
}
