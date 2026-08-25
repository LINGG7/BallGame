Shader "BallGame/DoorFlow"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.1, 0.3, 0.4, 0.7)
        _FlowColor ("Flow Color", Color) = (0.1, 0.8, 1, 1)
        _FlowSpeed ("Flow Speed", Float) = 0.45
        _FlowScale ("Flow Scale", Float) = 3
        _FlowStrength ("Flow Strength", Range(0, 2)) = 0.7
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.12
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTest]
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _FlowColor;
            float _FlowSpeed;
            float _FlowScale;
            half _FlowStrength;
            half _PulseStrength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.normal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 flowUv = input.uv;
                flowUv.x = frac(flowUv.x + _Time.y * _FlowSpeed);
                float band = smoothstep(0.42, 0.5, sin((flowUv.x + flowUv.y * 0.35) * 6.28318 * _FlowScale) * 0.5 + 0.5);
                float edge = pow(1.0 - saturate(abs(input.normal.z)), 2.0);
                float pulse = 1.0 + sin(_Time.y * 2.2) * _PulseStrength;
                fixed3 color = _BaseColor.rgb + _FlowColor.rgb * band * _FlowStrength;
                color += _FlowColor.rgb * edge * 0.35;
                return fixed4(color * pulse, saturate(_BaseColor.a));
            }
            ENDCG
        }
    }
}
