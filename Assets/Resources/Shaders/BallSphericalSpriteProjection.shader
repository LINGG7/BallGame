Shader "BallGame/SphericalSpriteProjection"
{
    Properties
    {
        _MainTex ("Skin Sprite", 2D) = "white" {}
        _SpriteRect ("Sprite Rect", Vector) = (0, 0, 1, 1)
        _ProjectionRadius ("Projection Radius", Range(0.5, 1)) = 1
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.55
        _Smoothness ("Smoothness", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _SpriteRect;
            half _ProjectionRadius;
            fixed4 _Tint;
            half _AmbientStrength;
            half _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 objectNormal : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.objectNormal = input.normal;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 objectNormal = normalize(input.objectNormal);
                float2 projectedUv = objectNormal.xy * 0.5 + 0.5;
                projectedUv = 0.5 + (projectedUv - 0.5) * _ProjectionRadius;
                float2 spriteUv = _SpriteRect.xy + projectedUv * _SpriteRect.zw;
                fixed4 skinSample = tex2D(_MainTex, spriteUv);
                fixed3 albedo = skinSample.rgb * _Tint.rgb;

                float3 worldNormal = normalize(input.worldNormal);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDirection = normalize(_WorldSpaceCameraPos - input.worldPosition);
                float3 halfDirection = normalize(lightDirection + viewDirection);
                half diffuse = saturate(dot(worldNormal, lightDirection));
                half softLighting = _AmbientStrength + (1.0h - _AmbientStrength) * diffuse;
                half specularPower = lerp(8.0h, 48.0h, _Smoothness);
                half specular = pow(saturate(dot(worldNormal, halfDirection)), specularPower)
                    * _Smoothness;

                fixed3 color = albedo * softLighting * _LightColor0.rgb;
                color += _LightColor0.rgb * specular * 0.35h;
                return fixed4(color, 1.0h);
            }
            ENDCG
        }

    }
}
