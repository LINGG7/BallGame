Shader "BallGame/Bubble Particle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _EdgeColor ("Bubble Edge", Color) = (0.72, 0.94, 1, 1)
        _FillOpacity ("Fill Opacity", Range(0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
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
            fixed4 _Color;
            fixed4 _EdgeColor;
            float _FillOpacity;

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

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed textureAlpha = tex2D(_MainTex, input.uv).a;
                float2 centered = input.uv * 2.0 - 1.0;
                float radius = length(centered);

                float outerMask = 1.0 - smoothstep(0.88, 1.0, radius);
                float innerMask = smoothstep(0.62, 0.82, radius);
                float ring = outerMask * innerMask;
                float fill = (1.0 - smoothstep(0.0, 0.86, radius)) * _FillOpacity;

                float2 highlightCenter = float2(-0.34, 0.34);
                float highlight = 1.0 - smoothstep(0.08, 0.22, distance(centered, highlightCenter));
                highlight *= outerMask;

                float alpha = saturate(ring + fill + highlight * 0.8);
                fixed3 color = lerp(input.color.rgb, _EdgeColor.rgb, saturate(ring * 0.72 + highlight));
                return fixed4(color, alpha * input.color.a * textureAlpha);
            }
            ENDCG
        }
    }
}
