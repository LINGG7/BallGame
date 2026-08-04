Shader "BallGame/Magnet Range Flow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.18
        _RingAlpha ("Ring Alpha", Range(0, 1)) = 0.32
        _RingCount ("Ring Count", Range(1, 8)) = 4
        _FlowSpeed ("Flow Speed", Range(-4, 4)) = 0.8
        [Enum(Outward,1,Inward,0)] _FlowDirection ("Flow Direction", Float) = 1
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.18
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
            float _BaseAlpha;
            float _RingAlpha;
            float _RingCount;
            float _FlowSpeed;
            float _FlowDirection;
            float _EdgeSoftness;

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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float2 centeredUv = i.uv - 0.5;
                float distanceFromCenter = length(centeredUv) * 2.0;
                float circleMask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, distanceFromCenter);

                clip(circleMask - 0.001);

                float radialFade = saturate(1.0 - distanceFromCenter);
                float flowDirection = _FlowDirection > 0.5 ? 1.0 : -1.0;
                float ringPhase = frac(distanceFromCenter * _RingCount - _Time.y * _FlowSpeed * flowDirection);
                float ring = 1.0 - smoothstep(0.02, 0.18, ringPhase);
                float alpha = (_BaseAlpha * pow(radialFade, 0.55) + ring * _RingAlpha) * circleMask;

                fixed4 color = i.color;
                color.a *= alpha * tex.a;
                return color;
            }
            ENDCG
        }
    }
}
