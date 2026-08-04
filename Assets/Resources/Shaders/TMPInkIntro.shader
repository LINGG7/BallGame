Shader "BallGame/UI/TMP Ink Intro"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _RevealProgress ("Reveal", Range(0,1)) = 0
        _DissolveProgress ("Dissolve", Range(0,1)) = 0
        _TextMinX ("Text Min X", Float) = -500
        _TextMaxX ("Text Max X", Float) = 500

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _FaceColor;
            float4 _ClipRect;
            float _RevealProgress;
            float _DissolveProgress;
            float _TextMinX;
            float _TextMaxX;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 blend = frac(p);
                blend = blend * blend * (3.0 - 2.0 * blend);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1, 0));
                float c = Hash21(cell + float2(0, 1));
                float d = Hash21(cell + float2(1, 1));
                return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.localPosition = input.vertex.xy;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _FaceColor;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float sdf = tex2D(_MainTex, input.texcoord).a;
                float sdfWidth = max(fwidth(sdf) * 0.8, 0.012);
                float glyphAlpha = smoothstep(0.5 - sdfWidth, 0.5 + sdfWidth, sdf);

                float textWidth = max(_TextMaxX - _TextMinX, 1.0);
                float normalizedX = saturate((input.localPosition.x - _TextMinX) / textWidth);
                float brushNoise = ValueNoise(input.localPosition * float2(0.025, 0.055));
                float brushFront = _RevealProgress * 1.12 - 0.06 + (brushNoise - 0.5) * 0.09;
                float reveal = 1.0 - smoothstep(brushFront - 0.035, brushFront + 0.025, normalizedX);

                float dissolveNoise = ValueNoise(input.localPosition * 0.075);
                float dissolveThreshold = lerp(0.08, 0.92, dissolveNoise);
                float dissolved = smoothstep(dissolveThreshold - 0.055, dissolveThreshold + 0.055, _DissolveProgress);
                float inkVariation = lerp(0.82, 1.0, ValueNoise(input.localPosition * 0.12 + 17.4));

                fixed4 color = input.color;
                color.rgb *= inkVariation;
                color.a *= glyphAlpha * reveal * (1.0 - dissolved);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
