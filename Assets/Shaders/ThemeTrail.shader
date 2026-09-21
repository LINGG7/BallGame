Shader "BallGame/Theme Trail"
{
    Properties
    {
        _Style ("0 Lightning, 1 Rainbow, 2 Sakura, 3 Bubble, 4 Ink, 5 Music", Float) = 0
        _Particle ("Particle silhouette", Float) = 0
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
            float _Style, _Particle;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            float segment(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                return length(p - a - ab * saturate(dot(p-a, ab) / dot(ab, ab)));
            }
            float stroke(float d, float width) { return 1 - smoothstep(width, width + 0.025, d); }
            float3 rainbow(float hue)
            {
                return saturate(abs(frac(hue + float3(0, 0.666667, 0.333333)) * 6 - 3) - 1);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float alpha = 0;
                float3 rgb = i.color.rgb;
                if (_Particle < 0.5)
                {
                    if (_Style < 0.5)
                    {
                        float phase = floor(_Time.y * 9) * 0.173;
                        float zig = (abs(frac(i.uv.x * 12 + phase) * 2 - 1) * 2 - 1) * 0.42;
                        float d = abs(p.y - zig);
                        alpha = saturate((1-smoothstep(0.09,0.2,d)) + 0.2*(1-smoothstep(0.2,0.65,d)));
                        rgb = lerp(rgb, float3(0.9,1,1), 1-smoothstep(0.025,0.12,d));
                    }
                    else if (_Style < 1.5)
                    {
                        alpha = 1-smoothstep(0.6,1,abs(p.y));
                        rgb = rainbow(i.uv.y * 0.85 + _Time.y * 0.12);
                    }
                    else if (_Style > 3.5 && _Style < 4.5)
                    {
                        float grain = sin(i.uv.x*117 + p.y*31)*sin(i.uv.x*63-p.y*17);
                        alpha = (1-smoothstep(0.5+grain*0.12,0.95,abs(p.y)))*(0.74+grain*0.2);
                    }
                    else
                    {
                        float center = _Style > 4.5 ? sin(i.uv.x*45)*0.17 : 0;
                        alpha = 1-smoothstep(0.35,0.95,abs(p.y-center));
                    }
                }
                else if (_Style < 0.5)
                {
                    float d = min(segment(p,float2(0.24,0.85),float2(-0.32,0.1)),
                        min(segment(p,float2(-0.32,0.1),float2(0.28,0.12)),
                            segment(p,float2(0.28,0.12),float2(-0.25,-0.85))));
                    alpha = stroke(d,0.065);
                    rgb = lerp(rgb,float3(0.85,1,1),0.45);
                }
                else if (_Style < 1.5)
                    alpha = 1-smoothstep(0.2,0.26,sqrt(abs(p.x*p.y))+length(p)*0.13);
                else if (_Style < 2.5)
                {
                    float lobes = min(length(p-float2(-0.25,0.25)),length(p-float2(0.25,0.25))) - 0.36;
                    float wedge = max(abs(p.x)-(p.y+0.85)*0.55, max(-0.85-p.y,p.y-0.25));
                    alpha = 1-smoothstep(-0.025,0.025,min(lobes,wedge));
                    rgb = lerp(rgb,float3(1,0.83,0.91),saturate(p.y*0.3+0.35));
                }
                else if (_Style < 3.5)
                {
                    float r = length(p);
                    alpha = max((1-smoothstep(0.035,0.1,abs(r-0.67)))*0.82,
                        (1-smoothstep(0.07,0.15,length(p-float2(-0.25,0.28))))*0.95);
                    alpha = max(alpha,(1-smoothstep(0.5,0.65,r))*0.06);
                    rgb = lerp(rgb,float3(0.9,1,1),saturate(p.y+0.3)*0.5);
                }
                else if (_Style < 4.5)
                {
                    float angle = atan2(p.y,p.x);
                    float edge = 0.55 + 0.1*sin(angle*5) + 0.07*cos(angle*9);
                    alpha = 1-smoothstep(edge-0.04,edge+0.025,length(p));
                    alpha = max(alpha,stroke(length(p-float2(0.65,0.4)),0.11));
                    alpha = max(alpha,stroke(length(p-float2(-0.65,-0.5)),0.07));
                }
                else
                {
                    float head = (1-smoothstep(0.9,1.05,length((p-float2(-0.24,-0.48))/float2(0.28,0.18))));
                    float stem = stroke(segment(p,float2(-0.015,-0.48),float2(-0.015,0.72)),0.065);
                    float flag = min(segment(p,float2(-0.015,0.72),float2(0.43,0.43)),
                        segment(p,float2(0.43,0.43),float2(0.37,0.12)));
                    alpha = max(head,max(stem,stroke(flag,0.08)));
                }
                return fixed4(rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}