Shader "BallGame/Trail Skin"
{
    Properties
    {
        _Shape ("Shape: 0 ribbon, 1 flame, 2 ice, 3 star", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            float _Shape;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float alpha;
                if (_Shape < 0.5)
                    alpha = 1 - smoothstep(0.3, 1, abs(p.y));
                else if (_Shape < 1.5)
                    alpha = 1 - smoothstep(0.4, 1, length(float2(p.x * (1.25 + p.y * 0.5), p.y)));
                else if (_Shape < 2.5)
                    alpha = 1 - smoothstep(0.7, 1, abs(p.x) + abs(p.y));
                else
                    alpha = 1 - smoothstep(0.18, 0.24, sqrt(abs(p.x * p.y)) + length(p) * 0.14);
                fixed4 result = i.color;
                result.a *= alpha;
                return result;
            }
            ENDCG
        }
    }
}