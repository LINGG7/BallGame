Shader "BallGame/UI/Ink Scene Transition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Packed Noise", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RevealProgress ("Reveal", Range(0,1)) = 0
        _DissolveProgress ("Dissolve", Range(0,1)) = 0
        _Origin ("Origin", Vector) = (0.5,0.5,0,0)
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
            "CanUseSpriteAtlas"="True"
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
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            fixed4 _Color;
            float4 _ClipRect;
            float _RevealProgress;
            float _DissolveProgress;
            float4 _Origin;

            float2 ToAspectSpace(float2 uv, float aspect)
            {
                return float2((uv.x - 0.5) * aspect, uv.y - 0.5);
            }

            float InkBloom(
                float2 position,
                float2 targetUv,
                float2 originUv,
                float2 origin,
                float aspect,
                float revealProgress,
                float seed,
                float isPrimary,
                fixed4 noiseA,
                fixed4 noiseB)
            {
                const float primaryStageEnd = 0.22;
                float2 target = ToAspectSpace(targetUv, aspect);
                float diagonal = sqrt(aspect * aspect + 1.0);
                float originDistance = distance(target, origin) / max(diagonal, 0.001);

                float branchProgress = saturate((revealProgress - primaryStageEnd) / (1.0 - primaryStageEnd));
                float branchDelay = 0.015 + originDistance * 0.24;
                float localProgress = isPrimary > 0.5
                    ? saturate(revealProgress / 0.72)
                    : saturate((branchProgress - branchDelay) / max(1.0 - branchDelay, 0.001));
                float easedProgress = localProgress * localProgress * (3.0 - 2.0 * localProgress);

                // Branches are born at the click and travel outward slowly enough to remain joined.
                float travelProgress = isPrimary > 0.5 ? 0.0 : localProgress * localProgress;
                float2 centerUv = lerp(originUv, targetUv, travelProgress);
                float2 center = ToAspectSpace(centerUv, aspect);

                float broadNoise = lerp(noiseA.r, noiseA.g, frac(seed * 0.173));
                float fineNoise = lerp(noiseB.b, noiseB.a, frac(seed * 0.317));
                float edgeDistortion = (broadNoise - 0.5) * 0.085 + (fineNoise - 0.5) * 0.027;

                float finalRadius = (isPrimary > 0.5 ? 0.43 : 0.37) + aspect * 0.14;
                float radius = finalRadius * easedProgress;
                float distortedDistance = distance(position, center) + edgeDistortion;
                float edgeWidth = 0.018 + (1.0 - localProgress) * 0.018;
                float coverage = 1.0 - smoothstep(radius - edgeWidth, radius + edgeWidth, distortedDistance);

                // Keep young bloom edges broken and fibrous; the holes close as the ink is absorbed.
                float fiberNoise = lerp(noiseA.a, noiseB.b, frac(seed * 0.431));
                float edgeBand = smoothstep(radius * 0.56, max(radius, 0.001), distortedDistance);
                float porosity = (1.0 - smoothstep(0.46, 0.94, localProgress))
                    * edgeBand * smoothstep(0.42, 0.72, fiberNoise) * 0.82;
                coverage *= 1.0 - porosity;
                coverage *= smoothstep(0.0, 0.045, localProgress);

                // A translucent wet ring makes the initial click bloom readable before branching.
                if (isPrimary > 0.5)
                {
                    float haloOuter = 1.0 - smoothstep(radius + 0.018, radius + 0.085, distortedDistance);
                    float haloInner = smoothstep(max(radius - 0.035, 0.0), radius + 0.012, distortedDistance);
                    float haloLifetime = 1.0 - smoothstep(primaryStageEnd, 0.58, revealProgress);
                    coverage = max(coverage, haloOuter * haloInner * haloLifetime * 0.38);
                }

                return saturate(coverage);
            }

            float MaximumCornerDistance(float2 origin, float aspect)
            {
                float2 bottomLeft = float2(-0.5 * aspect, -0.5);
                float2 bottomRight = float2(0.5 * aspect, -0.5);
                float2 topLeft = float2(-0.5 * aspect, 0.5);
                float2 topRight = float2(0.5 * aspect, 0.5);
                return max(
                    max(distance(origin, bottomLeft), distance(origin, bottomRight)),
                    max(distance(origin, topLeft), distance(origin, topRight)));
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float aspect = max(_ScreenParams.x / max(_ScreenParams.y, 1.0), 0.001);
                float2 position = ToAspectSpace(input.texcoord, aspect);
                float2 originUv = saturate(_Origin.xy);
                float2 origin = ToAspectSpace(originUv, aspect);

                float coverage = 0.0;
                [branch]
                if (_RevealProgress >= 0.999)
                {
                    // Once covered, skip all eight bloom distance fields during loading and dissolve.
                    coverage = 1.0;
                }
                else if (_RevealProgress > 0.001)
                {
                    float2 noiseUv = float2(input.texcoord.x * aspect, input.texcoord.y);
                    fixed4 revealNoiseA = tex2D(_NoiseTex, noiseUv * 0.83 + originUv * 0.37);
                    fixed4 revealNoiseB = tex2D(
                        _NoiseTex,
                        float2(-noiseUv.y, noiseUv.x) * 1.61 + float2(0.27, 0.61));

                    coverage = InkBloom(position, originUv, originUv, origin, aspect, _RevealProgress, 1.7, 1.0, revealNoiseA, revealNoiseB);
                    [branch]
                    if (_RevealProgress > 0.22)
                    {
                        coverage = max(coverage, InkBloom(position, float2(0.08, 0.09), originUv, origin, aspect, _RevealProgress, 3.1, 0.0, revealNoiseA, revealNoiseB));
                        coverage = max(coverage, InkBloom(position, float2(0.92, 0.10), originUv, origin, aspect, _RevealProgress, 5.8, 0.0, revealNoiseA, revealNoiseB));
                        coverage = max(coverage, InkBloom(position, float2(0.10, 0.90), originUv, origin, aspect, _RevealProgress, 8.4, 0.0, revealNoiseA, revealNoiseB));
                        coverage = max(coverage, InkBloom(position, float2(0.91, 0.89), originUv, origin, aspect, _RevealProgress, 11.2, 0.0, revealNoiseA, revealNoiseB));
                        coverage = max(coverage, InkBloom(position, float2(0.50, 0.17), originUv, origin, aspect, _RevealProgress, 14.5, 0.0, revealNoiseA, revealNoiseB));
                        coverage = max(coverage, InkBloom(position, float2(0.48, 0.83), originUv, origin, aspect, _RevealProgress, 17.9, 0.0, revealNoiseA, revealNoiseB));
                        coverage = max(coverage, InkBloom(position, float2(0.51, 0.50), originUv, origin, aspect, _RevealProgress, 21.6, 0.0, revealNoiseA, revealNoiseB));
                    }
                }

                float dissolved = 0.0;
                [branch]
                if (_DissolveProgress > 0.001)
                {
                    // Absorption begins near the click and travels outward with packed paper grain.
                    float maximumDistance = max(MaximumCornerDistance(origin, aspect), 0.001);
                    float radialOrder = distance(position, origin) / maximumDistance;
                    float2 dissolveUv = float2(input.texcoord.x * aspect, input.texcoord.y);
                    fixed4 dissolveNoiseA = tex2D(_NoiseTex, dissolveUv * 1.17 + float2(0.43, 0.19));
                    fixed4 dissolveNoiseB = tex2D(
                        _NoiseTex,
                        float2(dissolveUv.y, -dissolveUv.x) * 2.43 + float2(0.71, 0.38));
                    float dissolveBroad = lerp(dissolveNoiseA.r, dissolveNoiseA.g, 0.42);
                    float dissolveFine = lerp(dissolveNoiseB.b, dissolveNoiseB.a, 0.37);
                    float dissolveOrder = 0.03 + radialOrder * 0.62
                        + dissolveBroad * 0.16 + dissolveFine * 0.04;
                    dissolved = smoothstep(
                        dissolveOrder - 0.055,
                        dissolveOrder + 0.055,
                        _DissolveProgress);
                }

                fixed4 color = input.color;
                color.a *= tex2D(_MainTex, input.texcoord).a * coverage * (1.0 - dissolved);
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
