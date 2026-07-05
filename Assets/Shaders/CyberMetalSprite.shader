Shader "Custom/CyberMetalSprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _MetalTint("Metal Tint", Color) = (0.70, 0.80, 0.88, 1)
        _DarkReflectionTint("Dark Reflection Tint", Color) = (0.05, 0.07, 0.09, 1)
        _ReflectionTint("Reflection Tint", Color) = (1.00, 0.96, 0.78, 1)
        _MetalBlend("Metal Blend", Range(0, 1)) = 0.9
        _Brightness("Brightness", Range(0, 2)) = 0.88
        _Contrast("Contrast", Range(0, 2)) = 1.35
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.82
        _ReflectionContrast("Reflection Contrast", Range(0.5, 4)) = 1.65
        _ReflectionScale("Reflection Scale", Range(0.1, 5)) = 1.35
        _ReflectionSpeed("Reflection Speed", Range(-2, 2)) = 0
        _ReflectionAngle("Reflection Angle", Range(-180, 180)) = -24
        _HotspotStrength("Hotspot Strength", Range(0, 2)) = 0
        _HotspotWidth("Hotspot Width", Range(0.02, 0.45)) = 0.11
        _HotspotScale("Hotspot Scale", Range(0.1, 4)) = 1.05
        _HotspotSpeed("Hotspot Speed", Range(-4, 4)) = 0
        _HotspotAngle("Hotspot Angle", Range(-180, 180)) = 34
        _EdgeStrength("Edge Strength", Range(0, 2)) = 0.85
        _EdgeWidth("Edge Width", Range(0.1, 4)) = 2.1
        _BevelStrength("Bevel Strength", Range(0, 2)) = 0.55
        _BrushStrength("Brush Strength", Range(0, 0.5)) = 0.11
        _BrushScale("Brush Scale", Range(8, 180)) = 96
        _AlphaClip("Alpha Clip", Range(0, 1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _MetalTint;
                half4 _DarkReflectionTint;
                half4 _ReflectionTint;
                half _MetalBlend;
                half _Brightness;
                half _Contrast;
                half _ReflectionStrength;
                half _ReflectionContrast;
                half _ReflectionScale;
                half _ReflectionSpeed;
                half _ReflectionAngle;
                half _HotspotStrength;
                half _HotspotWidth;
                half _HotspotScale;
                half _HotspotSpeed;
                half _HotspotAngle;
                half _EdgeStrength;
                half _EdgeWidth;
                half _BevelStrength;
                half _BrushStrength;
                half _BrushScale;
                half _AlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float2 worldXY : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.worldXY = positionInputs.positionWS.xy;
                return output;
            }

            float2 DirectionFromDegrees(half degrees)
            {
                float angleRadians = radians(degrees);
                return float2(cos(angleRadians), sin(angleRadians));
            }

            half AlphaEdgeMask(half alpha)
            {
                half alphaWidth = max(fwidth(alpha) * _EdgeWidth, 0.0001h);
                half edge = 1.0h - saturate(alpha / alphaWidth);
                return pow(edge, 0.65h);
            }

            half BrushedNoise(float2 uv)
            {
                float diagonal = (uv.x * 0.62 + uv.y * 1.38) * _BrushScale;
                float brushLineIndex = floor(diagonal);
                return (frac(sin(brushLineIndex * 12.9898h) * 43758.5453h) - 0.5h) * 2.0h;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(sprite.a - _AlphaClip);

                half luminance = dot(sprite.rgb, half3(0.299h, 0.587h, 0.114h));
                half3 grayscale = luminance.xxx;
                half3 coolSilver = _MetalTint.rgb * saturate(luminance * 0.82h + 0.28h);
                half3 metalBase = lerp(sprite.rgb, lerp(grayscale, coolSilver, 0.88h), _MetalBlend);
                metalBase = ((metalBase - 0.5h) * _Contrast) + 0.5h;
                metalBase *= _Brightness;

                float2 centeredUv = input.uv - 0.5f;
                float2 reflectionDir = DirectionFromDegrees(_ReflectionAngle);
                float reflectionCoord = dot(centeredUv, reflectionDir) * _ReflectionScale;
                half broadReflection = saturate(0.54h + reflectionCoord);
                half topLight = smoothstep(0.18h, 0.92h, input.uv.y) * 0.18h;
                half leftLight = (1.0h - smoothstep(0.05h, 0.78h, input.uv.x)) * 0.12h;
                half reflection = saturate(pow(broadReflection, _ReflectionContrast) + topLight + leftLight);

                half3 darkReflection = _DarkReflectionTint.rgb * (1.0h - reflection) * 0.92h;
                half3 brightReflection = _ReflectionTint.rgb * pow(reflection, 3.8h) * 0.78h;
                half3 reflectedMetal = lerp(darkReflection, _MetalTint.rgb, reflection) + brightReflection;

                half surfaceDetail = saturate(luminance * 1.35h + 0.22h);
                half3 finalColor = lerp(metalBase, reflectedMetal * surfaceDetail, _ReflectionStrength);

                half leftEdge = 1.0h - smoothstep(0.02h, 0.16h, input.uv.x);
                half rightEdge = 1.0h - smoothstep(0.02h, 0.16h, 1.0h - input.uv.x);
                half topEdge = 1.0h - smoothstep(0.02h, 0.16h, 1.0h - input.uv.y);
                half bottomEdge = 1.0h - smoothstep(0.02h, 0.16h, input.uv.y);
                half litBevel = saturate(leftEdge + topEdge) * _BevelStrength;
                half darkBevel = saturate(rightEdge + bottomEdge) * _BevelStrength;

                finalColor += _ReflectionTint.rgb * litBevel * 0.22h;
                finalColor *= 1.0h - (darkBevel * 0.18h);

                half alphaEdge = AlphaEdgeMask(sprite.a) * _EdgeStrength;
                finalColor += lerp(_MetalTint.rgb, _ReflectionTint.rgb, 0.65h) * alphaEdge * 0.42h;

                half brush = BrushedNoise(input.uv) * _BrushStrength;
                finalColor *= 1.0h + brush;

                return half4(saturate(finalColor), sprite.a);
            }
            ENDHLSL
        }
    }
}
