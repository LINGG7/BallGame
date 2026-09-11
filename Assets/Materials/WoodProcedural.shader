Shader "BallGame/Wood Procedural"
{
    Properties
    {
        _LightColor ("Light Wood Color", Color) = (0.58, 0.32, 0.14, 1)
        _DarkColor ("Dark Grain Color", Color) = (0.19, 0.075, 0.025, 1)
        _Scale ("Grain Density", Range(1, 40)) = 12
        _Stretch ("Grain Length", Range(1, 20)) = 7
        _Rotation ("Grain Rotation (Degrees)", Range(0, 180)) = 0
        _Warp ("Grain Waviness", Range(0, 3)) = 0.8
        _Contrast ("Grain Contrast", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.28
    }
    SubShader
    {
        // Procedural grain requires per-object coordinates, which dynamic batching discards.
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        fixed4 _LightColor, _DarkColor;
        float _Scale, _Stretch, _Rotation, _Warp, _Contrast, _Smoothness;

        struct Input
        {
            float3 worldPos;
        };

        float Hash(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }

        float Noise(float3 p)
        {
            float3 cell = floor(p);
            float3 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(
                lerp(lerp(Hash(cell), Hash(cell + float3(1,0,0)), f.x),
                     lerp(Hash(cell + float3(0,1,0)), Hash(cell + float3(1,1,0)), f.x), f.y),
                lerp(lerp(Hash(cell + float3(0,0,1)), Hash(cell + float3(1,0,1)), f.x),
                     lerp(Hash(cell + float3(0,1,1)), Hash(cell + float3(1,1,1)), f.x), f.y), f.z);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Object coordinates keep the grain attached to moving objects and
            // expose end grain on cut faces without requiring mesh UVs.
            float3 p = mul(unity_WorldToObject, float4(IN.worldPos, 1)).xyz;
            float angle = radians(_Rotation);
            float s = sin(angle), c = cos(angle);
            p.xy = float2(c * p.x - s * p.y, s * p.x + c * p.y);
            p *= _Scale;
            p.y /= max(_Stretch, 1.0);
            float broadNoise = Noise(p * 0.45);
            float fineNoise = Noise(p * 1.7 + 17.0);
            float radius = length(p.xz + float2(3.7, 1.9));
            float phase = (radius + _Warp * (broadNoise + 0.25 * fineNoise)) * 6.283185;
            // Fade fine rings under minification to limit distant shimmer.
            float ringVisibility = 1.0 - smoothstep(0.5, 3.0, fwidth(phase));
            float rings = lerp(0.5, 0.5 + 0.5 * sin(phase), ringVisibility);
            float grain = smoothstep(0.35, 0.92, rings);
            float fibers = Noise(float3(p.x * 6.0, p.y * 0.7, p.z * 6.0));
            float tone = saturate(0.12 + _Contrast * (grain * 0.72 + fibers * 0.18));
            o.Albedo = lerp(_LightColor.rgb, _DarkColor.rgb, tone) * (0.9 + 0.2 * broadNoise);
            o.Metallic = 0;
            o.Smoothness = saturate(_Smoothness * (1.1 - 0.3 * grain));
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
