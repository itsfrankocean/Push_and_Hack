Shader "Custom/RibbedMetalCrateSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1, 1, 1, 1)
        _BaseMetalColor ("Base Metal Color", Color) = (0.18, 0.26, 0.29, 1)
        _DarkMetalColor ("Dark Groove Color", Color) = (0.035, 0.045, 0.05, 1)
        _RimColor ("Cold Edge Highlight", Color) = (0.52, 0.68, 0.72, 1)
        _RibCount ("Vertical Rib Count", Range(4, 32)) = 13
        _RibDepth ("Rib Depth", Range(0, 1)) = 0.62
        _BevelStrength ("Bevel Strength", Range(0, 2)) = 0.9
        _PanelDarkness ("Panel Darkness", Range(0, 1)) = 0.35
        _GrimeStrength ("Grime Strength", Range(0, 1)) = 0.22
        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RibbedMetalCrate"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _BaseMetalColor;
                half4 _DarkMetalColor;
                half4 _RimColor;
                half _RibCount;
                half _RibDepth;
                half _BevelStrength;
                half _PanelDarkness;
                half _GrimeStrength;
                half _AlphaClip;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(sprite.a - _AlphaClip);

                float2 uv = saturate(input.uv);
                half spriteLum = dot(sprite.rgb, half3(0.299, 0.587, 0.114));

                half3 metal = lerp(_DarkMetalColor.rgb, _BaseMetalColor.rgb, saturate(spriteLum * 0.85 + 0.25));

                float ribPhase = uv.x * max(_RibCount, 1.0) * 6.2831853;
                half ribWave = (half)(sin(ribPhase) * 0.5 + 0.5);
                half ridge = pow(saturate(ribWave), 4.0);
                half groove = pow(saturate(1.0 - ribWave), 2.0);

                metal = lerp(metal, _DarkMetalColor.rgb, groove * _RibDepth * 0.62);
                metal += _RimColor.rgb * ridge * _RibDepth * 0.16;

                float edgeDist = min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y));
                half outerBevel = (half)(1.0 - smoothstep(0.0, 0.055, edgeDist));
                half innerShadow = (half)(1.0 - smoothstep(0.045, 0.16, edgeDist));
                half topLeftLight = saturate((1.0 - (half)uv.x) * 0.35 + (half)uv.y * 0.35);

                metal += _RimColor.rgb * outerBevel * _BevelStrength * (0.28 + topLeftLight);
                metal = lerp(metal, _DarkMetalColor.rgb, innerShadow * _PanelDarkness * 0.32);

                half sideFalloff = saturate((half)uv.x * 0.72 + (1.0 - (half)uv.y) * 0.32);
                metal *= lerp(1.0, 0.58, sideFalloff * _PanelDarkness);

                half topLip = (half)smoothstep(0.76, 0.9, uv.y) * (half)smoothstep(0.04, 0.18, edgeDist);
                metal += _RimColor.rgb * topLip * 0.12;

                float coarseNoise = Hash21(floor(uv * 42.0));
                float fineNoise = Hash21(floor(uv * 145.0 + 17.0));
                half grime = (half)lerp(coarseNoise, fineNoise, 0.35);
                metal *= lerp(1.0, 0.72 + grime * 0.42, _GrimeStrength);

                return half4(saturate(metal), sprite.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
